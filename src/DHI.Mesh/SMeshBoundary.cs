﻿using System;
using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
// For easing syncing MeshBoundary.cs and SMeshBoundary.cs
using CMeshData=DHI.Mesh.SMeshData;
using CMeshFace=DHI.Mesh.SMeshFace;
using CMeshBoundary = DHI.Mesh.SMeshBoundary;

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
    public List<List<CMeshFace>> Segments = new List<List<CMeshFace>>();
  }

  public static partial class MeshBoundaryExtensions
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
      List<CMeshFace>[] nodeFaces = new List<CMeshFace>[meshData.NumberOfNodes];

      // Preallocate list of face on all nodes - used in next loop
      for (int i = 0; i < meshData.NumberOfNodes; i++)
        nodeFaces[i] = new List<CMeshFace>();

      // Create all potential boundary faces - those having
      // boundary code on both to-node and from-node
      // (not all those need to be boundary faces).
      for (int ielmt = 0; ielmt < meshData.NumberOfElements; ielmt++)
      {
        CreateAddElementFaces(meshData, nodeFaces, ielmt, checkAllFaces);
      }

      // Figure out boundary code and store all boundary faces
      List<CMeshFace> faces = new List<CMeshFace>();
      for (int i = 0; i < nodeFaces.Length; i++)
      {
        List<CMeshFace> thisNodeFaces = nodeFaces[i];
        for (int j = 0; j < thisNodeFaces.Count; j++)
        {
          CMeshFace face = thisNodeFaces[j];
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
    /// Build list of <see cref="CMeshBoundary"/>, one for each boundary code, based on the mesh <paramref name="mesh"/>
    /// </summary>
    public static List<CMeshBoundary> BuildBoundaryList(this CMeshData mesh)
    {
      List<CMeshFace> meshFaces;
      if (mesh.Faces != null)
        meshFaces = ExtractBoundaryFaces(mesh.Faces);
      else
        meshFaces = GetBoundaryFaces(mesh);

      return BuildBoundaryList(meshFaces);
    }

    /// <summary>
    /// Build list of <see cref="CMeshBoundary"/>, one for each boundary code, based on the <paramref name="meshFaces"/>
    /// <para>
    /// The <paramref name="meshFaces"/> need only contain boundary faces. Internal faces are ignored.
    /// </para>
    /// </summary>
    public static List<CMeshBoundary> BuildBoundaryList(List<CMeshFace> meshFaces)
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

      // For each boundary code, find segments
      for (int ic = 0; ic < bcs.Count; ic++)
      {
        int code = ic;
        List<CMeshFace> faces = bcs[ic];

        List<LinkedList<int>> segments = BuildBoundarySegments(faces);
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
    public static IGeometry BuildBoundaryGeometry(this CMeshData mesh, bool alwaysMultiPolygon = false)
    {
      List<CMeshFace> meshFaces;
      if (mesh.Faces != null)
        meshFaces = ExtractBoundaryFaces(mesh.Faces);
      else
        meshFaces = GetBoundaryFaces(mesh);

      return BuildBoundaryGeometry(mesh, meshFaces, alwaysMultiPolygon);
    }

    /// <summary>
    /// Build boundary polygon. Returns either a <see cref="Polygon"/> or <see cref="MultiPolygon"/>
    /// depending on whether the mesh is fully connected or consist of independent parts.
    /// <para>
    /// To always return a <see cref="MultiPolygon"/>, set the <paramref name="alwaysMultiPolygon"/> to true.
    /// </para>
    /// </summary>
    private static IGeometry BuildBoundaryGeometry(CMeshData mesh, List<CMeshFace> boundaryFaces, bool alwaysMultiPolygon)
    {
      // Find connected segments
      List<LinkedList<int>> segments = BuildBoundarySegments(boundaryFaces);
      if (segments == null)
        return null;

      List<LinearRing>  shells = new List<LinearRing>();
      List<ILinearRing> holes  = new List<ILinearRing>();

      // Create mesh boundary with segments
      foreach (LinkedList<int> segment in segments)
      {
        CMeshFace first = boundaryFaces[segment.First.Value];
        CMeshFace last  = boundaryFaces[segment.Last.Value];

        if (!NodeEquals(first.FromNode,last.ToNode))
          continue;

        Coordinate[] coords = new Coordinate[segment.Count+1];

        int i = 0;
        foreach (int iFace in segment)
        {
          CMeshFace face = boundaryFaces[iFace];
          coords[i++] = new Coordinate(mesh.X[face.FromNode], mesh.Y[face.FromNode]);
        }
        coords[segment.Count] = new Coordinate(mesh.X[last.ToNode], mesh.Y[last.ToNode]);
        LinearRing ring = new LinearRing(coords);
        if (ring.IsCCW)
        {
          shells.Add(ring);
        }
        else
        {
          holes.Add(ring);
        }
      }

      if (!alwaysMultiPolygon && shells.Count == 1)
      {
        Polygon p = new Polygon(shells[0], holes.ToArray());
        return p;
      }

      List<IPolygon> polygons = new List<IPolygon>(shells.Count);
      for (int i = 0; i < shells.Count; i++)
      {
        LinearRing shell = shells[i];
        Polygon shellPoly = new Polygon(shell);
        List<ILinearRing> myHoles = new List<ILinearRing>();
        List<ILinearRing> otherHoles = new List<ILinearRing>();
        for (int j = 0; j < holes.Count; j++)
        {
          ILinearRing hole = holes[i];
          if (shellPoly.Contains(hole))
            myHoles.Add(hole);
          else
            otherHoles.Add(hole);
        }

        Polygon p = new Polygon(shell, myHoles.ToArray());
        polygons.Add(p);
        holes = otherHoles;
      }
      MultiPolygon multiPoly = new MultiPolygon(polygons.ToArray());
      return multiPoly;
    }

    /// <summary>
    /// Build boundary segments, i.e. connect the faces to lines when possible.
    /// If provided all mesh faces, this will provide the outer boundary as
    /// one segment and all holes in the mesh etc. as individual section.
    /// There is no guarantee that the first one is the outer boundary.
    /// </summary>
    private static List<LinkedList<int>> BuildBoundarySegments(List<CMeshFace> faces)
    {
      if (faces.Count == 0)
        return null;

      // Sort faces on FromNode index
      faces.Sort(FaceSortComparer);

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
        int currentFaceIndex    = i;
        LinkedList<int> currentSegment = new LinkedList<int>();
        // Add current face to segment
        currentSegment.AddLast(currentFaceIndex);
        faceSegmentIndex[currentFaceIndex] = currentSegmentIndex;

        while (true)
        {
          // Try find next face, which is the face with fromNode matching currentFace.ToNode
          CMeshFace currentFace  = faces[currentFaceIndex];
          int      nextFaceIndex = Array.BinarySearch(fromNodes, currentFace.ToNode);

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

      return segments;
    }

    private static bool NodeEquals(int node1, int node2)
    {
      return node1 == node2;
    }

    private static int FaceSortComparer(CMeshFace f1, CMeshFace f2)
    {
      return f1.FromNode.CompareTo(f2.FromNode);
    }

    private static void CreateAddElementFaces(CMeshData meshData, List<CMeshFace>[] nodeFaces, int ielmt, bool checkAllFaces)
    {
      int[] elmtNodes = meshData.ElementTable[ielmt];
      for (int j = 0; j < elmtNodes.Length; j++)
      {
        int fromNode = elmtNodes[j];
        int toNode   = elmtNodes[(j + 1) % elmtNodes.Length];
        if (meshData.Code[fromNode] > 0 && meshData.Code[toNode] > 0 || checkAllFaces)
          SMeshData.AddFace(ielmt, fromNode, toNode, nodeFaces);
      }
    }

  }

}