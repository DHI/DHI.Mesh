
using System.Diagnostics;

namespace DHI.Mesh
{
  /// <summary>
  /// A face is a boundary between two elements. The face has a direction, defined from <see cref="FromNode"/> to <see cref="ToNode"/>,
  /// and in that direction it has a <see cref="LeftElement"/> and a <see cref="RightElement"/>
  /// <para>
  /// For boundary faces, the <see cref="RightElement"/> does not exist and is null.
  /// </para>
  /// </summary>
  [DebuggerDisplay("MeshFace: {FromNode.Index}-{ToNode.Index} ({LeftElement.Index},{RightElement?.Index})")]
  public class MeshFace
  {
    /// <summary> From node - start point of face </summary>
    public MeshNode FromNode { get; set; }
    /// <summary> to node - end point of face </summary>
    public MeshNode ToNode { get; set; }
    /// <summary> Left element </summary>
    public MeshElement LeftElement { get; set; }
    /// <summary> Right element. For boundary faces this is null. </summary>
    public MeshElement RightElement { get; set; }

    /// <summary>
    /// Boundary code.
    /// <para>
    /// For internal faces this is zero. For boundary faces this is the boundary code on the face.
    /// </para>
    /// </summary>
    public int Code { get; set; }

  }
}
