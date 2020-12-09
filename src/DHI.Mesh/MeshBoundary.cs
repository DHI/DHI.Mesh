using System;
using System.Collections.Generic;
using System.Text;

namespace DHI.Mesh
{
  /// <summary>
  /// Boundary segments / boundary-faces of a mesh having a specific boundary code
  /// </summary>
  public class MeshBoundary
  {
    /// <summary>
    /// Code value of mesh boundary segments
    /// </summary>
    public int Code;

    /// <summary>
    /// Mesh boundary segments
    /// </summary>
    public List<List<MeshFace>> Segments = new List<List<MeshFace>>();
  }

  public static class MeshBoundaryExtensions
  {
    /// <summary>
    /// Return all boundary faces. Unsorted
    /// </summary>
    public static List<MeshFace> GetBoundaryFaces(this MeshData meshData)
    {
      List<MeshFace>[] nodeFaces = new List<MeshFace>[meshData.Nodes.Count];

      // Preallocate list of face on all nodes - used in next loop
      for (int i = 0; i < meshData.Nodes.Count; i++)
        nodeFaces[i] = new List<MeshFace>();

      // Create all potential boundary faces - those having
      // boundary code on both to-node and from-node
      // (not all those need to be boundary faces).
      for (int ielmt = 0; ielmt < meshData.Elements.Count; ielmt++)
      {
        MeshElement    element   = meshData.Elements[ielmt];
        List<MeshNode> elmtNodes = element.Nodes;
        for (int j = 0; j < elmtNodes.Count; j++)
        {
          MeshNode fromNode = elmtNodes[j];
          MeshNode toNode   = elmtNodes[(j + 1) % elmtNodes.Count];
          if (fromNode.Code > 0 && toNode.Code > 0)
            MeshData.AddFace(element, fromNode, toNode, nodeFaces);
        }
      }

      // Figure out boundary code and store all boundary faces
      List<MeshFace> faces = new List<MeshFace>();
      for (int i = 0; i < nodeFaces.Length; i++)
      {
        List<MeshFace> thisNodeFaces = nodeFaces[i];
        for (int j = 0; j < thisNodeFaces.Count; j++)
        {
          MeshFace face = thisNodeFaces[j];
          // Only take those with fromNode matching this node - 
          // otherwise they are taken twice.
          //if (face.FromNode.Index == i)
          {
            face.SetBoundaryCode();
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
    public static List<MeshBoundary> BuildBoundaryList(this MeshData mesh)
    {
      List<MeshFace> meshFaces;
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
    public static List<MeshBoundary> BuildBoundaryList(List<MeshFace> meshFaces)
    {
      // Sort all faces on boundary code, assuming code numbers does not grow very big.
      //Dictionary<int, List<MeshFace>> bcs = new Dictionary<int, List<MeshFace>>();
      List<List<MeshFace>> bcs = new List<List<MeshFace>>();

      for (int i = 0; i < meshFaces.Count; i++)
      {
        MeshFace meshFace = meshFaces[i];
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
            List<MeshFace> boundaryFaces = new List<MeshFace>();
            bcs.Add(boundaryFaces);
          }
          bcs[meshFace.Code].Add(meshFace);
        }
      }

      List<MeshBoundary> boundaries = new List<MeshBoundary>();

      // For each boundary code, find segments

      //foreach (KeyValuePair<int, List<MeshFace>> bfkvp in bcs)
      //{

      //  int code = bfkvp.Key;
      //  List<MeshFace> faces = bfkvp.Value;

      for (int ic = 0; ic < bcs.Count; ic++)
      {
        int code = ic;
        List<MeshFace> faces = bcs[ic];

        if (faces.Count == 0)
          continue;

        // Sort faces on FromNode index
        faces.Sort(FaceSortComparer);

        // Create searchable array of FromNode indices
        int[] fromNodes = new int[faces.Count];
        for (int i = 0; i < faces.Count; i++)
        {
          fromNodes[i] = faces[i].FromNode.Index;
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
          int currentFaceIndex    = i;
          LinkedList<int> currentSegment = new LinkedList<int>();
          // Add current face to segment
          currentSegment.AddLast(currentFaceIndex);
          faceSegmentIndex[currentFaceIndex] = currentSegmentIndex;

          while (true)
          {
            // Try find next face, which is the face with fromNode matching currentFace.ToNode
            MeshFace currentFace   = faces[currentFaceIndex];
            int      nextFaceIndex = Array.BinarySearch(fromNodes, currentFace.ToNode.Index);

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
                thisSegmentListNode               = thisSegmentListNode.Previous;
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
        MeshBoundary meshBoundary = new MeshBoundary() {Code = code};
        foreach (LinkedList<int> segment in segments)
        {
          if (segment == null)
            continue;
          List<MeshFace> segmentFaces = new List<MeshFace>(segment.Count);
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

    private static int FaceSortComparer(MeshFace f1, MeshFace f2)
    {
      return f1.FromNode.Index.CompareTo(f2.FromNode.Index);
    }

  }
}
