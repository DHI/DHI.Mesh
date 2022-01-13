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
