using System;
using System.Collections.Generic;
using System.Text;

namespace DHI.Mesh
{
  public  interface IMeshData : IMeshDataInfo
  {

    /// <summary>
    /// Nodes in the mesh.
    /// </summary>
    IList<MeshNode> Nodes { get; set; }
    /// <summary>
    /// Elements in the mesh.
    /// </summary>
    IList<MeshElement> Elements { get; set; }
  }
}
