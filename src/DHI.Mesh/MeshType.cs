namespace DHI.Mesh
{
  /// <summary>
  /// Type of mesh
  /// </summary>
  public enum MeshType
  {
    /// <summary>
    /// 2D area series
    /// </summary>
    Mesh2D,
    /// <summary>
    /// 1D vertical column
    /// </summary>
    MeshVerticalColumn,
    /// <summary>
    /// 2D vertical slice through a <see cref="Mesh3DSigma"/>
    /// </summary>
    MeshVerticalProfileSigma,
    /// <summary>
    /// 2D vertical slice through a <see cref="Mesh3DSigmaZ"/>
    /// </summary>
    MeshVerticalProfileSigmaZ,
    /// <summary>
    /// 3D file with sigma coordinates, i.e., a constant number of layers.
    /// </summary>
    Mesh3DSigma,
    /// <summary>
    /// 3D file with sigma and Z coordinates, i.e. a varying number of layers.
    /// </summary>
    Mesh3DSigmaZ,

  }
}