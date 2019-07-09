using System;
using System.Collections.Generic;

namespace DHI.Mesh
{

  /// <summary>
  /// A mesh, consisting of triangles and quadrilaterals elements.
  /// <para>
  /// A mesh consist of a number of nodes an elements. The node defines the coordinates.
  /// Each element is defined by a number of nodes. The number of nodes depends on the
  /// type of the element, triangular or quadrilateral, 2D or 3D, horizontal or vertical.
  /// </para>
  /// <para>
  /// For details, check the "DHI Flexible File Format" document, in the
  /// "MIKE SDK documentation index".
  /// </para>
  /// </summary>
  public class MeshData
  {
    #region Geometry region

    /// <summary>
    /// Projection string, in WKT format
    /// </summary>
    public string Projection { get; set; }

    /// <summary>
    /// Unit of the z variables in the nodes and elements.
    /// </summary>
    public MeshUnit ZUnit { get; set; }

    /// <summary>
    /// Nodes in the mesh.
    /// </summary>
    public List<MeshNode> Nodes { get; set; }
    /// <summary>
    /// Elements in the mesh.
    /// </summary>
    public List<MeshElement> Elements { get; set; }

    /// <summary>
    /// Element faces.
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<MeshFace> Faces { get; set; }

    #endregion

    /// <summary>
    /// Create mesh from arrays. 
    /// </summary>
    public static MeshData CreateMesh(string projection, int[] nodeIds, double[] x, double[] y, double[] z, int[] code, int[] elementIds, int[] elementTypes, int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
    {
      MeshData meshData = new MeshData();
      meshData.Projection = projection;
      meshData.ZUnit = zUnit;
      meshData.Nodes = new List<MeshNode>(nodeIds.Length);
      meshData.Elements = new List<MeshElement>(elementIds.Length);

      for (int i = 0; i < nodeIds.Length; i++)
      {
        var node = new MeshNode()
        {
          Index = i,
          Id = nodeIds[i],
          X = x[i],
          Y = y[i],
          Z = z[i],
          Code = code[i],
          Elements = new List<MeshElement>(),
        };
        meshData.Nodes.Add(node);
      }

      for (int ielmt = 0; ielmt < elementIds.Length; ielmt++)
      {
        var element = new MeshElement()
        {
          Index = ielmt,
          Id = elementIds[ielmt],
          ElementType = elementTypes[ielmt],
          Nodes = new List<MeshNode>(connectivity[ielmt].Length),
        };
        double xc = 0;
        double yc = 0;
        double zc = 0;

        for (int j = 0; j < connectivity[ielmt].Length; j++)
        {
          MeshNode meshNode = meshData.Nodes[connectivity[ielmt][j]-1];
          element.Nodes.Add(meshNode);
          meshNode.Elements.Add(element);
          xc += meshNode.X;
          yc += meshNode.Y;
          zc += meshNode.Z;
        }

        element.XCenter = xc / connectivity[ielmt].Length;
        element.YCenter = yc / connectivity[ielmt].Length;
        element.ZCenter = zc / connectivity[ielmt].Length;

        meshData.Elements.Add(element);
      }

      return meshData;

    }

    /// <summary>
    /// Build derived mesh data, especially the <see cref="MeshFace"/> lists.
    /// </summary>
    public void BuildDerivedData()
    {
      Faces = new List<MeshFace>();

      // Preallocate list of face on all nodes - used in next loop
      for (int i = 0; i < Nodes.Count; i++)
        Nodes[i].Faces = new List<MeshFace>();

      // Create all faces.
      for (int ielmt = 0; ielmt < Elements.Count; ielmt++)
      {
        MeshElement element = Elements[ielmt];
        element.Faces = new List<MeshFace>();
        List<MeshNode> elmtNodes = element.Nodes;
        for (int j = 0; j < elmtNodes.Count; j++)
        {
          MeshNode fromNode = elmtNodes[j];
          MeshNode toNode   = elmtNodes[(j + 1) % elmtNodes.Count];
          AddFace(element, fromNode, toNode);
        }
      }

      // Figure out boundary code
      for (int i = 0; i < Faces.Count; i++)
      {
        MeshFace face = Faces[i];

        // If the RightElement exists, this face is an internal face
        if (face.RightElement != null)
        {
          continue;
        }

        // RightElement does not exist, so it is a boundary face.
        int fromCode = face.FromNode.Code;
        int toCode   = face.ToNode.Code;

        // True if "invalid" boundary face, then set it as internal face.
        bool internalFace = false;

        if (fromCode == 0)
        {
          internalFace = true;
          throw new Exception (string.Format("Invalid mesh: Boundary face, from node {0} to node {1} is missing a boundary code on node {0}. " +
                                   "Hint: Modify boundary code for node {0}",
                                   face.FromNode.Index + 1, face.ToNode.Index + 1));
        }
        if (toCode == 0)
        {
          internalFace = true;
          throw new Exception(string.Format("Invalid mesh: Boundary face, from node {0} to node {1} is missing a boundary code on node {1}. " +
                                   "Hint: Modify boundary code for node {1}",
                                   face.FromNode.Index + 1, face.ToNode.Index + 1));
        }

        int faceCode;

        // Find face code:
        // 1) In case any of the nodes is a land node (code value 1) then the
        //    boundary face is a land face, given boundary code value 1.
        // 2) For boundary faces (if both fromNode and toNode have code values larger than 1), 
        //    the face code is the boundary code value of toNode.
        if (fromCode == 1 || toCode == 1)
          faceCode = 1;
        else
          faceCode = toCode;

        if (!internalFace)
          face.Code = faceCode;
      }

      // Add face to the elements list of faces
      for (int i = 0; i < Faces.Count; i++)
      {
        MeshFace face = Faces[i];
        face.LeftElement.Faces.Add(face);
        face.RightElement?.Faces.Add(face);
      }
    }

    /// <summary>
    /// Create and add a face.
    /// <para>
    /// A face is only "added once", i.e. when two elements share the face, it is found twice,
    /// once defined as "toNode"-"fromNode" and once as "fromNode"-"toNode". The second time,
    /// the existing face is being reused, and the element is added as the <see cref="MeshFace.RightElement"/>
    /// </para>
    /// <para>
    /// The <see cref="MeshFace"/> is added to the global list of faces, and also to tne nodes list of faces.
    /// </para>
    /// </summary>
    private void AddFace(MeshElement element, MeshNode fromNode, MeshNode toNode)
    {
      List<MeshFace> fromNodeFaces = fromNode.Faces;
      List<MeshFace> toNodeFaces = toNode.Faces;

      if (fromNodeFaces.FindIndex(mf => mf.ToNode == toNode) >= 0)
      {
        throw new Exception (string.Format("Invalid mesh: Double face, from node {0} to node {1}. " +
                                 "Hint: Probably too many nodes was merged into one of the two face nodes." +
                                 "Try decrease node merge tolerance value",
                                 fromNode.Index + 1, toNode.Index + 1));
      }
      
      // Try find "reverse face" going from from-node to to-node.
      int reverseFaceIndex = toNodeFaces.FindIndex(mf => mf.ToNode == fromNode);
      if (reverseFaceIndex >= 0)
      {
        // Found reverse face, reuse it and add the elment as the RightElement
        MeshFace reverseFace = toNodeFaces[reverseFaceIndex];
        reverseFace.RightElement = element;
      }
      else
      {
        // Found new face, set element as LeftElement and add it to both from-node and to-node
        MeshFace meshFace = new MeshFace()
        {
          FromNode = fromNode,
          ToNode = toNode,
          LeftElement = element,
        };

        Faces.Add(meshFace);
        fromNodeFaces.Add(meshFace);
        toNodeFaces.Add(meshFace);
      }
    }


  }
}
