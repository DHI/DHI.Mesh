using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DHI.Mesh
{
  /// <summary>
  /// Boundary of a mesh having a specific boundary code
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


    /// <summary>
    /// Build list of face boundaries of <paramref name="mesh"/>
    /// </summary>
    public static List<MeshBoundary> BuildBoundaryList(MeshData mesh)
    {
      if (mesh.Faces == null)
        mesh.BuildDerivedData();

      // Sort all faces on boundary code
      Dictionary<int, List<MeshFace>> bcs = new Dictionary<int, List<MeshFace>>();

      for (int i = 0; i < mesh.Faces.Count; i++)
      {
        MeshFace meshFace = mesh.Faces[i];
        if (meshFace.IsBoundaryFace())
        {
          List<MeshFace> boundaryFaces;
          if (!bcs.TryGetValue(meshFace.Code, out boundaryFaces))
          {
            boundaryFaces = new List<MeshFace>();
            bcs.Add(meshFace.Code, boundaryFaces);
          }
          boundaryFaces.Add(meshFace);
        }
      }

      List<MeshBoundary> boundaries = new List<MeshBoundary>();

      // For each boundary code, find segments
      foreach (KeyValuePair<int, List<MeshFace>> bfkvp in bcs)
      {
        int code = bfkvp.Key;
        List<MeshFace> faces = bfkvp.Value;

        // Sort faces on FromNode index
        faces.Sort((f1, f2) => f1.FromNode.Index.CompareTo(f2.FromNode.Index));
        
        // Create searchable array of FromNode indices
        int[] fromNodes = faces.Select(f => f.FromNode.Index).ToArray();
        
        // Matching array telling which boundary segment a given face belongs to
        int[] faceSegmentIndex   = new int[faces.Count];
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
            MeshFace currentFace = faces[currentFaceIndex];
            int nextFaceIndex = Array.BinarySearch(fromNodes, currentFace.ToNode.Index);

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
              // Move all from current segment to 
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

            // Make nextFace to currentFace - add it to the list of current segments.
            currentFaceIndex = nextFaceIndex;
            currentSegment.AddLast(currentFaceIndex);
            faceSegmentIndex[currentFaceIndex] = currentSegmentIndex;
          }
        }

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

      boundaries.Sort((mb1, mb2) => mb1.Code.CompareTo(mb2.Code));
      return boundaries;

    }
  }
}
