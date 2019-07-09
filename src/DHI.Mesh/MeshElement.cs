using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DHI.Mesh
{

  /// <summary>
  /// An element in the mesh.
  /// </summary>
  [DebuggerDisplay("MeshElement: {" + nameof(Index) + "}")]
  public class MeshElement
  {
    /// <summary>
    /// Index of mesh element in list of elements
    /// </summary>
    public int Index { get; set; }
    /// <summary>
    /// Id of mesh element
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of element
    /// </summary>
    public int ElementType { get; set; }

    /// <summary>
    /// Nodes that this element is defined by.
    /// <para>
    /// Nodes are defined counter-clockwise in 2D.
    /// </para>
    /// </summary>
    public List<MeshNode> Nodes { get; set; }

    /// <summary>
    /// Faces (boundary of element) that this element is defined by.
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="MeshData.BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<MeshFace> Faces { get; set; }

    /// <summary>
    /// X center coordiante of this element
    /// </summary>
    public double XCenter { get; set; }
    /// <summary>
    /// Y center coordiante of this element
    /// </summary>
    public double YCenter { get; set; }
    /// <summary>
    /// Z center coordiante of this element
    /// </summary>
    public double ZCenter { get; set; }
  }
}
