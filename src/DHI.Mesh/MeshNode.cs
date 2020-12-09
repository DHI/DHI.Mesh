using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DHI.Mesh
{
  /// <summary>
  /// A node in the mesh.
  /// </summary>
  [DebuggerDisplay("MeshNode: {" + nameof(Index) + "}")]
  public class MeshNode
  {
    /// <summary>
    /// Index of node (0-based) in list of nodes.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Id of node.
    /// </summary>
    public int Id { get; set; }

    /// <summary> X coordinate </summary>
    public double X { get; set; }
    /// <summary> Y coordinate </summary>
    public double Y { get; set; }
    /// <summary> Z coordinate </summary>
    public double Z { get; set; }
    /// <summary> Boundary code. Zero if ths node is not on the boundary </summary>
    public int Code { get; set; }

    /// <summary>
    /// Elements that this node is part of. 
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="MeshData.BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<MeshElement> Elements { get; set; }

    /// <summary>
    /// Faces that is connected to this node. 
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="MeshData.BuildFaces"/> or 
    /// <see cref="MeshData.BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<MeshFace> Faces { get; set; }

  }
}
