using System;
using System.Runtime.Serialization;

namespace DHI.Mesh
{

  /// <summary>
  /// A mesh, consisting of triangles and quadrilaterals elements.
  /// <para>
  /// This SMeshData class uses low-level structures to save on memory, compared to
  /// the <see cref="MeshData"/> class that uses a more object oriented approach
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
  [Serializable]
  public class SMeshDataBase : ISMeshData
  {

    public SMeshDataBase(string projection, int[] nodeIds, double[] x, double[] y, double[] z, int[] code, int[] elementIds, MeshUnit zUnit = MeshUnit.Meter)
    {
      Projection = projection;
      ZUnit = zUnit;
      NodeIds = nodeIds;
      X = x;
      Y = y;
      Z = z;
      Code = code;
      ElementIds = elementIds;
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
    /// Number of nodes in the mesh.
    /// </summary>
    [IgnoreDataMemberAttribute]
    public virtual int NumberOfNodes { get { return (NodeIds.Length); } }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    [IgnoreDataMemberAttribute]
    public virtual int NumberOfElements { get { return (ElementIds.Length); } }


    /// <summary>
    /// Node Id's. Can be null, then default value is assumed.
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// <para>
    /// Be aware that changing this to anything but the default values (1,2,3,...)
    /// can make some tools stop working.
    /// </para>
    /// </summary>
    public virtual int[] NodeIds { get; set; }

    /// <summary>
    /// Node X coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public virtual double[] X { get; set; }

    /// <summary>
    /// Node Y coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public virtual double[] Y { get; set; }

    /// <summary>
    /// Node Z coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public virtual double[] Z { get; set; }

    /// <summary>
    /// Node boundary code.
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public virtual int[] Code { get; set; }

    /// <summary>
    /// Element Id's. Can be null, then default value is assumed.
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// <para>
    /// Be aware that changing this to anything but the default values (1,2,3,...)
    /// can make some tools stop working.
    /// </para>
    /// </summary>
    public virtual int[] ElementIds { get; set; }
  }
}
