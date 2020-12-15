using System;
using System.Collections.Generic;

namespace DHI.Mesh
{
  /// <summary>
  /// Boundary segments / boundary-faces of a mesh having a specific boundary code
  /// </summary>
  public class SMeshBoundary
  {
    /// <summary>
    /// Code value of mesh boundary segments
    /// </summary>
    public int Code;

    /// <summary>
    /// Mesh boundary segments
    /// </summary>
    public List<List<SMeshFace>> Segments = new List<List<SMeshFace>>();
  }

  public static partial class MeshBoundaryExtensions
  {
    /// <summary>
    /// Return all boundary faces. Unsorted
    /// </summary>
    public static List<SMeshFace> GetBoundaryFaces(this SMeshData meshData)
    {
      int numberOfNodes    = meshData.NumberOfNodes;
      int numberOfElements = meshData.NumberOfElements;

      List<SMeshFace>[] nodeFaces             = new List<SMeshFace>[numberOfNodes];

      // Preallocate list of face on all nodes - used in next loop
      for (int i = 0; i < numberOfNodes; i++)
        nodeFaces[i] = new List<SMeshFace>();

      // Create all potential boundary faces - those having
      // boundary code on both to-node and from-node
      // (not all those need to be boundary faces).
      for (int ielmt = 0; ielmt < numberOfElements; ielmt++)
      {
        int element = ielmt;
        int[] elmtNodes = meshData.ElementTable[element];
        for (int j = 0; j < elmtNodes.Length; j++)
        {
          int fromNode = elmtNodes[j];
          int toNode = elmtNodes[(j + 1) % elmtNodes.Length];
          if (meshData.Code[fromNode] > 0 && meshData.Code[toNode] > 0)
            SMeshData.AddFace(element, fromNode, toNode, nodeFaces);
        }
      }

      // Figure out boundary code and store all boundary faces
      List<SMeshFace> faces = new List<SMeshFace>();
      for (int i = 0; i < nodeFaces.Length; i++)
      {
        List<SMeshFace> thisNodeFaces = nodeFaces[i];
        for (int j = 0; j < thisNodeFaces.Count; j++)
        {
          SMeshFace face = thisNodeFaces[j];
          // Only take those with fromNode matching this node - 
          // otherwise they are taken twice.
          //if (face.FromNode.Index == i)
          {
            face.SetBoundaryCode(meshData);
            if (face.IsBoundaryFace())
              faces.Add(face);
          }
        }
      }

      return faces;
    }

    /// <summary>
    /// Build list of <see cref="MeshBoundary"/>, one for each boundary code, based on the mesh <paramref name="mesh"/>
    /// </summary>
    public static List<SMeshBoundary> BuildBoundaryList(this SMeshData mesh)
    {
      List<SMeshFace> meshFaces;
      if (mesh.Faces != null)
        meshFaces = mesh.Faces;
      else
        meshFaces = GetBoundaryFaces(mesh);

      return BuildBoundaryList(meshFaces);
    }

    /// <summary>
    /// Build list of <see cref="MeshBoundary"/>, one for each boundary code, based on the <paramref name="meshFaces"/>
    /// <para>
    /// The <paramref name="meshFaces"/> need only contain boundary faces. Internal faces are ignored.
    /// </para>
    /// </summary>
    public static List<SMeshBoundary> BuildBoundaryList(List<SMeshFace> meshFaces)
    {
      // Sort all faces on boundary code, assuming code numbers does not grow very big.
      //Dictionary<int, List<MeshFace>> bcs = new Dictionary<int, List<MeshFace>>();
      List<List<SMeshFace>> bcs = new List<List<SMeshFace>>();

      for (int i = 0; i < meshFaces.Count; i++)
      {
        SMeshFace meshFace = meshFaces[i];
        if (meshFace.IsBoundaryFace())
        {
          //List<MeshFace> boundaryFaces;
          //if (!bcs.TryGetValue(meshFace.Code, out boundaryFaces))
          //{
          //  boundaryFaces = new List<MeshFace>();
          //  bcs.Add(meshFace.Code, boundaryFaces);
          //}
          //boundaryFaces.Add(meshFace);

          while (meshFace.Code + 1 > bcs.Count)
          {
            List<SMeshFace> boundaryFaces = new List<SMeshFace>();
            bcs.Add(boundaryFaces);
          }
          bcs[meshFace.Code].Add(meshFace);
        }
      }

      List<SMeshBoundary> boundaries = new List<SMeshBoundary>();

      // For each boundary code, find segments

      //foreach (KeyValuePair<int, List<MeshFace>> bfkvp in bcs)
      //{

      //  int code = bfkvp.Key;
      //  List<MeshFace> faces = bfkvp.Value;

      for (int ic = 0; ic < bcs.Count; ic++)
      {
        int code = ic;
        List<SMeshFace> faces = bcs[ic];

        if (faces.Count == 0)
          continue;

        // Sort faces on FromNode index
        faces.Sort(SMeshFaceSortComparer);

        // Create searchable array of FromNode indices
        int[] fromNodes = new int[faces.Count];
        for (int i = 0; i < faces.Count; i++)
        {
          fromNodes[i] = faces[i].FromNode;
        }

        // Matching array telling which boundary segment a given face belongs to
        int[] faceSegmentIndex = new int[faces.Count];
        for (int ii = 0; ii < faceSegmentIndex.Length; ii++) faceSegmentIndex[ii] = -1;

        // All segments with this boundary code
        List<LinkedList<int>> segments = new List<LinkedList<int>>();

        // Make sure to visit all faces
        for (int i = 0; i < faces.Count; i++)
        {
          // Check if this face has already been visited.
          if (faceSegmentIndex[i] >= 0)
            continue;

          // Start new boundary segment with face i
          int currentSegmentIndex = segments.Count;
          int currentFaceIndex = i;
          LinkedList<int> currentSegment = new LinkedList<int>();
          // Add current face to segment
          currentSegment.AddLast(currentFaceIndex);
          faceSegmentIndex[currentFaceIndex] = currentSegmentIndex;

          while (true)
          {
            // Try find next face, which is the face with fromNode matching currentFace.ToNode
            SMeshFace currentFace = faces[currentFaceIndex];
            int nextFaceIndex = Array.BinarySearch(fromNodes, currentFace.ToNode);

            if (nextFaceIndex < 0)
            {
              // No to-node, we are done with this segment
              segments.Add(currentSegment);
              break;
            }

            // Check if the next face is already part of a segment
            if (faceSegmentIndex[nextFaceIndex] >= 0)
            {
              if (faceSegmentIndex[nextFaceIndex] == currentSegmentIndex)
              {
                // Circular boundary - we are done with this segment
                segments.Add(currentSegment);
                break;
              }

              // Now: nextSegment is not the same as the currentSection,
              // but they should be - move entire current segment to the
              // start of the nextFace segment

              int nextFaceSegment = faceSegmentIndex[nextFaceIndex];
              // Move all faces from currentSegment to nextFaceSegment
              LinkedListNode<int> thisSegmentListNode = currentSegment.Last;
              while (thisSegmentListNode != null)
              {
                int faceToMoveIndex = thisSegmentListNode.Value;
                segments[nextFaceSegment].AddFirst(faceToMoveIndex);
                faceSegmentIndex[faceToMoveIndex] = nextFaceSegment;
                thisSegmentListNode = thisSegmentListNode.Previous;
              }

              break; // Break out of while (true) loop
            }

            // Next face is not already part of a segment, add it to the end of this segment
            // Make nextFace to currentFace - add it to the list of current segments.
            currentFaceIndex = nextFaceIndex;
            currentSegment.AddLast(currentFaceIndex);
            faceSegmentIndex[currentFaceIndex] = currentSegmentIndex;
          }
        }

        // Create mesh boundary with segments
        SMeshBoundary meshBoundary = new SMeshBoundary() { Code = code };
        foreach (LinkedList<int> segment in segments)
        {
          if (segment == null)
            continue;
          List<SMeshFace> segmentFaces = new List<SMeshFace>(segment.Count);
          foreach (int currentFace in segment)
          {
            segmentFaces.Add(faces[currentFace]);
          }

          meshBoundary.Segments.Add(segmentFaces);
        }

        boundaries.Add(meshBoundary);
      }

      // Sort on boundary codes
      boundaries.Sort((mb1, mb2) => mb1.Code.CompareTo(mb2.Code));
      return boundaries;
    }

    private static int SMeshFaceSortComparer(SMeshFace f1, SMeshFace f2)
    {
      return f1.FromNode.CompareTo(f2.FromNode);
    }

  }

}