using System;
using System.Collections.Generic;

namespace DHI.Mesh
{

  /// <summary>
  /// A mesh, consisting of triangles and quadrilaterals elements.
  /// <para>
  /// This MeshData class models the mesh using an objected oriented data model, easing access and navigation in the mesh.
  /// If memory overhead becomes too big, the <see cref="SMeshData"/> uses memory optimized data structures.
  /// </para>
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
    /// Number of nodes in the mesh.
    /// </summary>
    public int NumberOfNodes { get { return (Nodes.Count); } }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    public int NumberOfElements { get { return (Elements.Count); } }


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
    /// <see cref="BuildFaces"/> or 
    /// <see cref="BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<MeshFace> Faces { get; private set; }

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
        };
        meshData.Nodes.Add(node);
      }

      for (int ielmt = 0; ielmt < elementIds.Length; ielmt++)
      {
        int[] nodeInElmt = connectivity[ielmt];
        int numNodesInElmt = nodeInElmt.Length;

        var element = new MeshElement()
        {
          Index = ielmt,
          Id = elementIds[ielmt],
          ElementType = elementTypes[ielmt],
          Nodes = new List<MeshNode>(numNodesInElmt),
        };
        double xc = 0;
        double yc = 0;
        double zc = 0;

        for (int j = 0; j < numNodesInElmt; j++)
        {
          int nodeIndex = nodeInElmt[j]-1;
          MeshNode meshNode = meshData.Nodes[nodeIndex];
          element.Nodes.Add(meshNode);
          xc += meshNode.X;
          yc += meshNode.Y;
          zc += meshNode.Z;
        }

        double inumNodesInElmt = 1.0 / numNodesInElmt;
        element.XCenter = xc * inumNodesInElmt; // / numNodesInElmt;
        element.YCenter = yc * inumNodesInElmt; // / numNodesInElmt;
        element.ZCenter = zc * inumNodesInElmt; // / numNodesInElmt;

        meshData.Elements.Add(element);
      }

      return meshData;

    }

    /// <summary>
    /// Build derived mesh data, the <see cref="MeshNode.Elements"/> and <see cref="MeshFace"/> lists.
    /// </summary>
    public List<string> BuildDerivedData()
    {
      BuildNodeElements();
      List<string> errors = BuildFaces(true, true);
      return errors;
    }

    public void BuildNodeElements()
    {
      // Build up element list in nodes
      for (int i = 0; i < Nodes.Count; i++)
      {
        Nodes[i].Elements = new List<MeshElement>();
      }

      for (int ielmt = 0; ielmt < Elements.Count; ielmt++)
      {
        MeshElement    element        = Elements[ielmt];
        List<MeshNode> nodeInElmt     = element.Nodes;
        int            numNodesInElmt = nodeInElmt.Count;

        for (int j = 0; j < numNodesInElmt; j++)
        {
          MeshNode meshNode = nodeInElmt[j];
          meshNode.Elements.Add(element);
        }
      }
    }

    /// <summary>
    /// Build up the list of <see cref="Faces"/>
    /// </summary>
    /// <param name="nodeFaces">Also build up <see cref="MeshNode.Faces"/></param>
    /// <param name="elmtFaces">Also build up <see cref="MeshElement.Faces"/></param>
    public List<string> BuildFaces(bool nodeFaces = false, bool elmtFaces = false)
    {
      List<string> errors = new List<string>();

      // Build up face lists
      Faces = new List<MeshFace>();

      //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
      //watch.Start();
      // Preallocate list of face on all nodes - used in next loop
      for (int i = 0; i < Nodes.Count; i++)
        Nodes[i].Faces = new List<MeshFace>();
      //watch.Stop();
      //Console.Out.WriteLine("Prealloc nodeface " + watch.Elapsed.TotalSeconds);
      //watch.Reset();

      //watch.Start();
      // Create all faces.
      for (int ielmt = 0; ielmt < Elements.Count; ielmt++)
      {
        MeshElement element = Elements[ielmt];
        if (elmtFaces)
          element.Faces = new List<MeshFace>();
        List<MeshNode> elmtNodes = element.Nodes;
        for (int j = 0; j < elmtNodes.Count; j++)
        {
          MeshNode fromNode = elmtNodes[j];
          MeshNode toNode   = elmtNodes[(j + 1) % elmtNodes.Count];
          AddFace(element, fromNode, toNode);
        }
      }
      //watch.Stop();
      //Console.Out.WriteLine("Build faces       " + watch.Elapsed.TotalSeconds);
      //watch.Reset();

      // Figure out boundary code
      for (int i = 0; i < Faces.Count; i++)
      {
        MeshFace face = Faces[i];
        face.SetBoundaryCode(errors);
      }

      if (!nodeFaces)
      {
        for (int i = 0; i < Nodes.Count; i++)
          Nodes[i].Faces = null;
      }

      if (elmtFaces)
      {
        // Add face to the elements list of faces
        for (int i = 0; i < Faces.Count; i++)
        {
          MeshFace face = Faces[i];
          face.LeftElement.Faces.Add(face);
          face.RightElement?.Faces.Add(face);
        }
      }

      return errors;
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

      // Try find "reverse face" going from from-node to to-node.
      // The FindIndex with delegate is 10+ times slower than the tight loop below.
      //int reverseFaceIndex = toNodeFaces.FindIndex(mf => mf.ToNode == fromNode);
      int reverseToNodeFaceIndex = -1;
      for (int i = 0; i < toNodeFaces.Count; i++)
      {
        if (toNodeFaces[i].ToNode == fromNode)
        {
          reverseToNodeFaceIndex = i;
          break;
        }
      }

      if (reverseToNodeFaceIndex >= 0)
      {
        // Found reverse face, reuse it and add the elment as the RightElement
        MeshFace reverseFace = toNodeFaces[reverseToNodeFaceIndex];
        reverseFace.RightElement = element;
      }
      else
      {
        // Found new face, set element as LeftElement and add it to both from-node and to-node
        MeshFace meshFace = new MeshFace(fromNode, toNode)
        {
          LeftElement = element,
        };

        Faces.Add(meshFace);
        fromNodeFaces.Add(meshFace);
        toNodeFaces.Add(meshFace);
      }
    }

    /// <summary>
    /// Create and add a face - special version for <see cref="MeshBoundaryExtensions.GetBoundaryFaces"/>
    /// <para>
    /// A face is only "added once", i.e. when two elements share the face, it is found twice,
    /// once defined as "toNode"-"fromNode" and once as "fromNode"-"toNode". The second time,
    /// the existing face is being reused, and the element is added as the <see cref="MeshFace.RightElement"/>
    /// </para>
    /// <para>
    /// The <see cref="MeshFace"/> is added to the global list of faces, and also to tne nodes list of faces.
    /// </para>
    /// </summary>
    internal static void AddFace(MeshElement element, MeshNode fromNode, MeshNode toNode, List<MeshFace>[] nodeFaces)
    {
      List<MeshFace> fromNodeFaces = nodeFaces[fromNode.Index];
      List<MeshFace> toNodeFaces = nodeFaces[toNode.Index];

      // Try find "reverse face" going from from-node to to-node.
      // The FindIndex with delegate is 10+ times slower than the tight loop below.
      //int reverseFaceIndex = toNodeFaces.FindIndex(mf => mf.ToNode == fromNode);
      int reverseFaceIndex = -1;
      for (int i = 0; i < toNodeFaces.Count; i++)
      {
        if (toNodeFaces[i].ToNode == fromNode)
        {
          reverseFaceIndex = i;
          break;
        }
      }

      if (reverseFaceIndex >= 0)
      {
        // Found reverse face, reuse it and add the elment as the RightElement
        MeshFace reverseFace = toNodeFaces[reverseFaceIndex];
        reverseFace.RightElement = element;
      }
      else
      {
        // Found new face, set element as LeftElement and add it to both from-node and to-node
        MeshFace meshFace = new MeshFace(fromNode, toNode)
        {
          LeftElement = element,
        };

        fromNodeFaces.Add(meshFace);
        //toNodeFaces.Add(meshFace);
      }
    }

    /// <summary>
    /// Determines whether the specified mesh geometry is equal to the current mesh's geometry.
    /// The order of the nodes and elements in the mesh must equal.
    /// <para>
    /// This only checks the geometry, i.e. node coordinates, node boundary codes and element connectivity.
    /// The Id's in the mesh may differ. 
    /// </para>
    /// </summary>
    /// <param name="other"></param>
    /// <param name="tolerance"></param>
    public bool EqualsGeometry(MeshData other, double tolerance = 1e-3)
    {
      if (this.Nodes.Count != other.Nodes.Count ||
          this.Elements.Count != other.Elements.Count)
        return false;

      for (int i = 0; i < this.Nodes.Count; i++)
      {
        MeshNode nt = this.Nodes[i];
        MeshNode no = other.Nodes[i];
        if (Math.Abs(nt.X - no.X) > tolerance ||
            Math.Abs(nt.Y - no.Y) > tolerance ||
            nt.Code != no.Code)
          return false;
      }

      for (int i = 0; i < this.Elements.Count; i++)
      {
        MeshElement et = this.Elements[i];
        MeshElement eo = other.Elements[i];
        if (et.Nodes.Count != eo.Nodes.Count)
          return false;
        for (int j = 0; j < et.Nodes.Count; j++)
        {
          if (et.Nodes[j].Index != eo.Nodes[j].Index)
            return false;
        }
      }
      return true;
    }
  }
}
