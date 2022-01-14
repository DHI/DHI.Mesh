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
    private int[] _nodeIds;
    private int[] _elementIds;
    private double[] _x;
    private double[] _y;
    private double[] _z;
    private int[] _code;

    internal protected int[] _elementType;
    internal protected int[][] _connectivity;

    public SMeshDataBase(string projection, int[] nodeIds, double[] x, double[] y, double[] z, int[] code, int[] elementIds, int[] elementType, int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
    {
      Projection = projection;
      ZUnit = zUnit;
      _nodeIds = nodeIds;
      _x = x;
      _y = y;
      _z = z;
      _code = code;
      _elementIds = elementIds;
      _elementType = elementType;
      _connectivity = connectivity;
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
    public int NumberOfNodes { get { return (_nodeIds.Length); } }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    [IgnoreDataMemberAttribute]
    public int NumberOfElements { get { return (_elementIds.Length); } }


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
    public int[] NodeIds
    {
      get { return _nodeIds; }
      set
      {
        if (_nodeIds.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _nodeIds = value;
      }
    }

    /// <summary>
    /// Node X coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public double[] X
    {
      get { return _x; }
      set
      {
        if (_x.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _x = value;
      }
    }

    /// <summary>
    /// Node Y coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public double[] Y
    {
      get { return _y; }
      set
      {
        if (_y.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _y = value;
      }
    }

    /// <summary>
    /// Node Z coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public double[] Z
    {
      get { return _z; }
      set
      {
        if (_z.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _z = value;
      }
    }

    /// <summary>
    /// Node boundary code.
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public int[] Code
    {
      get { return _code; }
      set
      {
        if (_code.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _code = value;
      }
    }

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
    public int[] ElementIds
    {
      get { return _elementIds; }
      set
      {
        if (_elementIds.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of elements");
        _elementIds = value;
      }
    }

    /// <summary>
    /// Array of element types. See documentation for each type. Can be null, then automatically derived.
    /// </summary>
    // TODO: Make into a enum
    public int[] ElementType
    {
      get { return _elementType; ; }
      set
      {
        if (_elementType.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of elements");
        _elementType = value;
      }
    }

    /// <summary>
    /// The <see cref="ElementTable"/> defines for each element which 
    /// nodes that defines the element. 
    /// <para>
    /// The numbers in the <see cref="ElementTable"/> are node indeces, not numbers!
    /// Each value in the table must be between 0 and <code>number-of-nodes - 1</code>.
    /// </para>
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public int[][] ElementTable
    {
      get { return _connectivity; }
      set
      {
        if (_connectivity.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of elements");
        _connectivity = value;
      }
    }
  }
}
