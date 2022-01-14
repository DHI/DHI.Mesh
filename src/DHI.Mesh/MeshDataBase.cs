using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DHI.Mesh
{

  /// <summary>
  /// A mesh, consisting of triangles and quadrilaterals elements.
  /// A mesh consist of a number of nodes an elements. The node defines the coordinates.
  /// Each element is defined by a number of nodes. The number of nodes depends on the
  /// type of the element, triangular or quadrilateral, 2D or 3D, horizontal or vertical.
  /// </summary>
  [Serializable]
  public class MeshDataBase : IMeshData
  {
    public MeshDataBase(IList<MeshNode> nodes, IList<MeshElement> elements, string projection, MeshUnit zUnit)
    {
      Nodes = nodes;
      Elements = elements;
      Projection = projection;
      ZUnit = zUnit;
    }

    /// <summary>
    /// Create mesh from arrays.
    /// <para>
    /// Note that the <paramref name="connectivity"/> array is using zero-based indices
    /// (as compared to the <see cref="MeshFile.ElementTable"/>, which is using one-based indices)
    /// </para>
    /// </summary>
    public MeshDataBase(string projection, int[] nodeIds, double[] x, double[] y, double[] z, int[] code, int[] elementIds, int[] elementTypes, int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
    {
      Projection = projection;
      ZUnit = zUnit;
      Nodes = new List<MeshNode>(nodeIds.Length);
      Elements = new List<MeshElement>(elementIds.Length);

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
        Nodes.Add(node);
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
          int nodeIndex = nodeInElmt[j];
          MeshNode meshNode = Nodes[nodeIndex];
          element.Nodes.Add(meshNode);
          xc += meshNode.X;
          yc += meshNode.Y;
          zc += meshNode.Z;
        }

        double inumNodesInElmt = 1.0 / numNodesInElmt;
        element.XCenter = xc * inumNodesInElmt; // / numNodesInElmt;
        element.YCenter = yc * inumNodesInElmt; // / numNodesInElmt;
        element.ZCenter = zc * inumNodesInElmt; // / numNodesInElmt;

        Elements.Add(element);
      }
    }

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
    public IList<MeshNode> Nodes { get; set; }
    /// <summary>
    /// Elements in the mesh.
    /// </summary>
    public IList<MeshElement> Elements { get; set; }

    /// <summary>
    /// Number of nodes in the mesh.
    /// </summary>
    [IgnoreDataMemberAttribute]
    public int NumberOfNodes { get { return (Nodes.Count); } }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    [IgnoreDataMemberAttribute]
    public int NumberOfElements { get { return (Elements.Count); } }

  }
}
