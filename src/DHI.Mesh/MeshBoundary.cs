using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
// For easing syncing MeshBoundary.cs and SMeshBoundary.cs
using CMeshData =DHI.Mesh.MeshData;
using CMeshFace=DHI.Mesh.MeshFace;
using CMeshBoundary = DHI.Mesh.MeshBoundary;

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
    public List<List<CMeshFace>> Segments = new List<List<CMeshFace>>();
  }

  public static class MeshBoundaryExtensions
  {

    /// <summary>
    /// Provided a list of all faces in the mesh, extract faces on the boundary - unsorted
    /// </summary>
    public static List<CMeshFace> ExtractBoundaryFaces(List<CMeshFace> meshFaces)
    {
      List<CMeshFace> bcs = new List<CMeshFace>();
      for (int i = 0; i < meshFaces.Count; i++)
      {
        CMeshFace meshFace = meshFaces[i];
        if (meshFace.IsBoundaryFace())
        {
          bcs.Add(meshFace);
        }
      }
      return bcs;
    }

    /// <summary>
    /// Return all boundary faces. Unsorted
    /// </summary>
    /// <param name="meshData">MeshData object to get boundary faces for</param>
    public static List<CMeshFace> GetBoundaryFaces(this CMeshData meshData)
    {
      return meshData.GetBoundaryFaces(false);
    }

    /// <summary>
    /// Return all boundary faces. Unsorted
    /// </summary>
    /// <param name="meshData">MeshData object to get boundary faces for</param>
    /// <param name="checkAllFaces">In case boundary codes are set incorrectly, this will check all faces</param>
    public static List<CMeshFace> GetBoundaryFaces(this CMeshData meshData, bool checkAllFaces)
    {
      //System.Diagnostics.Stopwatch timer = MeshExtensions.StartTimer();

      List<CMeshFace>[] facesFromNode = new List<CMeshFace>[meshData.NumberOfNodes];

      // Preallocate list of face on all nodes - used in next loop
      for (int i = 0; i < meshData.NumberOfNodes; i++)
        facesFromNode[i] = new List<CMeshFace>();

      // Create all potential boundary faces - those having
      // boundary code on both to-node and from-node
      // (not all those need to be boundary faces).
      for (int ielmt = 0; ielmt < meshData.NumberOfElements; ielmt++)
      {
        CreateAddElementFaces(meshData, facesFromNode, ielmt, checkAllFaces);
      }

      // Figure out boundary code and store all boundary faces
      List<CMeshFace> boundaryFaces = new List<CMeshFace>();
      for (int i = 0; i < facesFromNode.Length; i++)
      {
        List<CMeshFace> facesFromThisNode = facesFromNode[i];
        for (int j = 0; j < facesFromThisNode.Count; j++)
        {
          CMeshFace face = facesFromThisNode[j];
          // Only take those with fromNode matching this node - 
          // otherwise they are taken twice.
          //if (face.FromNode.Index == i)
          {
            face.SetBoundaryCode();
            if (face.IsBoundaryFace())
              boundaryFaces.Add(face);
          }
        }
      }

      //timer.Report("GetBoundaryFaces");
      return boundaryFaces;
    }


    /// <summary>
    /// Build list of <see cref="CMeshBoundary"/>, one for each boundary code, based on the mesh <paramref name="mesh"/>
    /// </summary>
    public static List<CMeshBoundary> BuildBoundaryList(this CMeshData mesh)
    {
      List<CMeshFace> meshFaces;
      if (mesh.Faces != null)
        meshFaces = ExtractBoundaryFaces(mesh.Faces);
      else
        meshFaces = GetBoundaryFaces(mesh, true);

      return BuildBoundaryList(mesh, meshFaces);
    }

    /// <summary>
    /// Build list of <see cref="CMeshBoundary"/>, one for each boundary code, based on the <paramref name="meshFaces"/>
    /// <para>
    /// The <paramref name="meshFaces"/> need only contain boundary faces. Internal faces are ignored.
    /// </para>
    /// </summary>
    private static List<CMeshBoundary> BuildBoundaryList(CMeshData mesh, List<CMeshFace> meshFaces)
    {
      // Sort all faces on boundary code, assuming code numbers does not grow very big.
      List<List<CMeshFace>> bcs = new List<List<CMeshFace>>();

      for (int i = 0; i < meshFaces.Count; i++)
      {
        CMeshFace meshFace = meshFaces[i];
        if (meshFace.IsBoundaryFace())
        {

          while (meshFace.Code + 1 > bcs.Count)
          {
            List<CMeshFace> boundaryFaces = new List<CMeshFace>();
            bcs.Add(boundaryFaces);
          }
          bcs[meshFace.Code].Add(meshFace);
        }
      }

      List<CMeshBoundary> boundaries = new List<CMeshBoundary>();

      BoundarySegmentsBuilder bsb = new BoundarySegmentsBuilder(mesh);

      // For each boundary code, find segments
      for (int ic = 0; ic < bcs.Count; ic++)
      {
        int code = ic;
        List<CMeshFace> faces = bcs[ic];

        List<LinkedList<int>> segments = bsb.BuildBoundarySegments(faces);
        if (segments == null)
          continue;

        // Create mesh boundary with segments
        CMeshBoundary meshBoundary = new CMeshBoundary() {Code = code};
        foreach (LinkedList<int> segment in segments)
        {
          if (segment == null)
            continue;
          List<CMeshFace> segmentFaces = new List<CMeshFace>(segment.Count);
          foreach (int currentFace in segment)
          {
            segmentFaces.Add(faces[currentFace]);
          }

          meshBoundary.Segments.Add(segmentFaces);
        }

        boundaries.Add(meshBoundary);
      }

      //// Sort on boundary codes - well, they are created in code-order
      //boundaries.Sort((mb1, mb2) => mb1.Code.CompareTo(mb2.Code));
      return boundaries;
    }

    /// <summary>
    /// Build boundary polygon. Returns either a <see cref="Polygon"/> or <see cref="MultiPolygon"/>
    /// depending on whether the mesh is fully connected or consist of independent parts.
    /// <para>
    /// To always return a <see cref="MultiPolygon"/>, set the <paramref name="alwaysMultiPolygon"/> to true.
    /// </para>
    /// </summary>
    public static Geometry BuildBoundaryGeometry(this CMeshData mesh, bool alwaysMultiPolygon = false, bool checkAllFaces = true)
    {
      //System.Diagnostics.Stopwatch timer = MeshExtensions.StartTimer();
      mesh.BuildFaces(true);
      //timer.ReportAndRestart("BuildFaces");

      List<CMeshFace> meshFaces;
      if (mesh.Faces != null)
        meshFaces = ExtractBoundaryFaces(mesh.Faces);
      else
        meshFaces = GetBoundaryFaces(mesh, checkAllFaces);

      //timer.Report("GetBoundaryFaces " + meshFaces.Count);

      return BuildBoundaryGeometry(mesh, meshFaces, alwaysMultiPolygon);
    }

    /// <summary>
    /// Build boundary polygon. Returns either a <see cref="Polygon"/> or <see cref="MultiPolygon"/>
    /// depending on whether the mesh is fully connected or consist of independent parts.
    /// <para>
    /// To always return a <see cref="MultiPolygon"/>, set the <paramref name="alwaysMultiPolygon"/> to true.
    /// </para>
    /// </summary>
    private static Geometry BuildBoundaryGeometry(CMeshData mesh, List<CMeshFace> boundaryFaces, bool alwaysMultiPolygon)
    {
      // There will be one polygon for each connected sub mesh, in case there is more than one.

      //System.Diagnostics.Stopwatch timer = MeshExtensions.StartTimer();
      // Find all connected sub meshes.
      SubMeshes subMeshes = mesh.FindConnectedSubMeshes();
      //timer.ReportAndRestart("FindConnectedSubMeshes " + subMeshes.NumberOfSubMeshes);

      BoundarySegmentsBuilder bsb = new BoundarySegmentsBuilder(mesh);

      if (subMeshes.NumberOfSubMeshes == 1)
      {
        Polygon boundaryPoly = BuildSubMeshBoundaryGeometry(mesh, bsb, boundaryFaces);
        if (!alwaysMultiPolygon)
          return boundaryPoly;
        MultiPolygon multiPolygon = new MultiPolygon(new Polygon[] { boundaryPoly });
        return multiPolygon;
      }

      else // More than one sub-mesh - make a Polygon for each sub mesh
      {
        // Find boundary faces for each sub mesh
        List<List<CMeshFace>> subMeshesBoundaryFaces = new List<List<CMeshFace>>(subMeshes.NumberOfSubMeshes);
        for (int i = 0; i < subMeshes.NumberOfSubMeshes; i++)
        {
          subMeshesBoundaryFaces.Add(new List<CMeshFace>());
        }
        for (int i = 0; i < boundaryFaces.Count; i++)
        {
          CMeshFace bcFace    = boundaryFaces[i];
          int       subMeshId = subMeshes.ElmtSubMesh[bcFace.LeftElement.Index];
          // subMeshId's starts from 1
          subMeshesBoundaryFaces[subMeshId - 1].Add(bcFace);
        }

        // Make boundary polygon for each sub mesh - ordered as in SubMeshInfos to get largest parts first
        List<Polygon> polygons = new List<Polygon>(subMeshes.NumberOfSubMeshes);
        foreach (SubMeshInfo subMeshInfo in subMeshes.SubMeshInfos)
        {
          List<CMeshFace> subMeshBoundaryFaces = subMeshesBoundaryFaces[subMeshInfo.SubMeshId-1];
          Polygon         subMeshBoundaryPoly  = BuildSubMeshBoundaryGeometry(mesh, bsb, subMeshBoundaryFaces);
          polygons.Add(subMeshBoundaryPoly);

        }

        MultiPolygon multiPoly = new MultiPolygon(polygons.ToArray());
        return multiPoly;
      }
    }

    private static Polygon BuildSubMeshBoundaryGeometry(CMeshData mesh, BoundarySegmentsBuilder bsb, List<CMeshFace> boundaryFaces)
    {
      // Find connected segments
      List<LinkedList<int>> segments = bsb.BuildBoundarySegments(boundaryFaces);
      if (segments == null)
        return null;

      LinearRing shell = null;
      List<LinearRing> holes  = new List<LinearRing>();

      // Create mesh boundary with segments
      for (int isegment = 0; isegment < segments.Count; isegment++)
      {
        LinkedList<int> segment = segments[isegment];
        CMeshFace       first   = boundaryFaces[segment.First.Value];
        CMeshFace       last    = boundaryFaces[segment.Last.Value];

        if (!NodeEquals(first.FromNode, last.ToNode))
        {
          Console.Out.WriteLine("Skipping: {0,4} {1,8} {2,8} {3,8}", isegment, segment.Count, first.FromNode, last.ToNode);
          continue;
        }

        Coordinate[] coords = new Coordinate[segment.Count + 1];

        int i = 0;
        foreach (int iFace in segment)
        {
          CMeshFace face = boundaryFaces[iFace];
          coords[i++] = new Coordinate(face.FromNode.X, face.FromNode.Y);
        }

        coords[segment.Count] = new Coordinate(last.ToNode.X, last.ToNode.Y);
        LinearRing ring = new LinearRing(coords);
        if (ring.IsCCW)
        {
          if (shell != null)
            throw new Exception("Finding two shells of a connected sub-mesh");
          shell = ring;
        }
        else
        {
          holes.Add(ring);
        }
      }
      Polygon p;
      if (holes.Count > 0)
        p = new Polygon(shell, holes.ToArray());
      else
        p = new Polygon(shell);
      return p;
    }

    /// <summary>
    /// Helper class for building boundary segments
    /// </summary>
    class BoundarySegmentsBuilder
    {
      private readonly CMeshData _mesh;

      int[]                      _fromNodeFace;
      Dictionary<int, List<int>> _fromNodeFaceDict;

      public BoundarySegmentsBuilder(CMeshData mesh)
      {
        _mesh = mesh;

        // Create searchable array of FromNode faces. Its value for each node is:
        // >=  0: Exactly one boundary face having this node as fromNode 
        // == -1: no boundary face having this node as fromNode 
        // == -2: More than one boundary face having this node as fromNode 
        // The last case happens rarely, though for grid meshes (dfs2 bathymetry) it is not uncommon.
        _fromNodeFace     = new int[_mesh.NumberOfNodes];
        for (int i = 0; i < _fromNodeFace.Length; i++) _fromNodeFace[i] = -1;
        _fromNodeFaceDict = new Dictionary<int, List<int>>();
      }


      /// <summary>
      /// Build boundary segments, i.e. connect the faces to lines when possible.
      /// If provided all mesh faces, this will provide the outer boundary as
      /// one segment and all holes in the mesh etc. as individual section.
      /// There is no guarantee that the first one is the outer boundary.
      /// </summary>
      public List<LinkedList<int>> BuildBoundarySegments(List<CMeshFace> faces)
      {
        if (faces.Count == 0)
          return null;

        for (int iface = 0; iface < faces.Count; iface++)
        {
          int fromNode = faces[iface].FromNode.Index;
          if (_fromNodeFace[fromNode] == -1)
          {
            // The first time the fromNode is a from-node
            _fromNodeFace[fromNode] = iface;
          }
          else if (_fromNodeFace[fromNode] == -2)
          {
            // The third or even more time the fromNode is a from-node
            _fromNodeFaceDict[fromNode].Add(iface);
          }
          else
          {
            // The second time the fromNode is a from-node
            List<int> list = new List<int>();
            list.Add(_fromNodeFace[fromNode]);
            list.Add(iface);
            _fromNodeFaceDict.Add(fromNode, list);
            _fromNodeFace[fromNode] = -2;
          }
        }

        // Array telling which boundary segment a given face belongs to
        int[] faceSegmentIndex = new int[faces.Count];
        for (int ii = 0; ii < faceSegmentIndex.Length; ii++) faceSegmentIndex[ii] = -1;

        // All segments build by the list of faces
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
            CMeshFace currentFace = faces[currentFaceIndex];
            int nextFaceIndex = NextFaceFromNode(currentFace.ToNode.Index, _fromNodeFace, _fromNodeFaceDict);

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

              int nextFaceSegmentIndex = faceSegmentIndex[nextFaceIndex];
              LinkedList<int> nextSegment = segments[nextFaceSegmentIndex];
              // Move all faces from currentSegment to nextFaceSegment
              LinkedListNode<int> thisSegmentListNode = currentSegment.Last;
              while (thisSegmentListNode != null)
              {
                int faceToMoveIndex = thisSegmentListNode.Value;
                nextSegment.AddFirst(faceToMoveIndex);
                faceSegmentIndex[faceToMoveIndex] = nextFaceSegmentIndex;
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


        // Reset fromNodeFace array to initial value - in case of more than one sub mesh
        for (int iface = 0; iface < faces.Count; iface++)
        {
          int fromNode = faces[iface].FromNode.Index;
          _fromNodeFace[fromNode] = -1;
        }
        _fromNodeFaceDict.Clear();

        return segments;
      }

    }

    private static int NextFaceFromNode(int nextFromNode, int[] fromNodeFace, Dictionary<int, List<int>> fromNodeFaceDict)
    {
      if (fromNodeFace[nextFromNode] >= -1)
        return fromNodeFace[nextFromNode];
      List<int> faces = fromNodeFaceDict[nextFromNode];
      if (faces.Count > 0)
      {
        int nextFace = faces[0];
        faces.RemoveAt(0);
        return nextFace;
      }
      return -1;
    }

    #region MeshData class dependent code

    private static bool NodeEquals(MeshNode node1, MeshNode node2)
    {
      return node1.Index == node2.Index;
    }

    private static void CreateAddElementFaces(CMeshData meshData, List<CMeshFace>[] facesFromNode, int ielmt, bool checkAllFaces)
    {
      MeshElement    elmt      = meshData.Elements[ielmt];
      List<MeshNode> elmtNodes = elmt.Nodes;
      for (int j = 0; j < elmtNodes.Count; j++)
      {
        MeshNode fromNode = elmtNodes[j];
        MeshNode toNode   = elmtNodes[(j + 1) % elmtNodes.Count];
        if (checkAllFaces || fromNode.Code > 0 && toNode.Code > 0)
          MeshData.AddFace(elmt, fromNode, toNode, facesFromNode);
      }
    }

    #endregion

  }

}
