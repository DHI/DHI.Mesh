using System;
using System.Collections.Generic;
using System.Text;

namespace DHI.Mesh
{
  /// <summary>
  /// Common interface for the two MeshData classes.
  /// <para>
  /// Included for easing unit tests.
  /// </para>
  /// </summary>
  public interface IMeshDataInfo
  {
    /// <summary>
    /// Projection string, in WKT format
    /// </summary>
    string Projection { get; set; }

    /// <summary>
    /// Unit of the z variables in the nodes and elements.
    /// </summary>
    MeshUnit ZUnit { get; set; }

    /// <summary>
    /// Number of nodes in the mesh.
    /// </summary>
    int NumberOfNodes { get; }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    int NumberOfElements { get; }
  }
}
