using System;
using System.Collections.Generic;

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
  public class SMeshData
  {
    // Node variables
    private int[]    _nodeIds; // this can be null, then set default id's, starting from 1
    private double[] _x;
    private double[] _y;
    private double[] _z;
    private int[]    _code;

    // Element variables
    private int[]   _elementIds; // this can be null, then set default id's, starting from 1
    private int[]   _elementType;
    private int[][] _connectivity;

    private bool _hasQuads;


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
    public int NumberOfNodes { get { return (_nodeIds.Length); } }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    public int NumberOfElements { get { return (_elementIds.Length); } }


    /// <summary>
    /// Node Id's
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
    /// Element Id's
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
    /// Array of element types. See documentation for each type.
    /// </summary>
    // TODO: Make into a enum
    public int[] ElementType
    {
      get { return _elementType; }
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
    /// The numbers in the <see cref="ElementTable"/> are node numbers, not indices!
    /// Each value in the table must be between 1 and number-of-nodes.
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

    private double[] _elmtXCenter;
    /// <summary> For each element, a list of element center coordinates</summary>
    public double[] ElementXCenter
    {
      get { return _elmtXCenter; }
    }

    private double[] _elmtYCenter;
    /// <summary> For each element, a list of element center coordinates</summary>
    public double[] ElementYCenter
    {
      get { return _elmtYCenter; }
    }

    private double[] _elmtZCenter;
    /// <summary> For each element, a list of element center coordinates</summary>
    public double[] ElementZCenter
    {
      get { return _elmtZCenter; }
    }

    private List<int>[] _nodesElmts;
    /// <summary> For each node, a list of elements that is connected to this node </summary>
    public List<int>[] NodesElmts
    {
      get { return _nodesElmts; }
    }

    /// <summary>
    /// Create mesh from arrays. 
    /// </summary>
    public static SMeshData CreateMesh(string projection, int[] nodeIds, double[] x, double[] y, double[] z, int[] code, int[] elementIds, int[] elementTypes, int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
    {
      SMeshData meshData     = new SMeshData();
      meshData.Projection    = projection;
      meshData.ZUnit         = zUnit;
      meshData._nodeIds      = nodeIds;
      meshData._x            = x;
      meshData._y            = y;
      meshData._z            = z;
      meshData._code         = code;
      meshData._elementIds   = elementIds;
      meshData._elementType  = elementTypes;
      meshData._connectivity = connectivity;

      return meshData;

    }

    public List<string> BuildDerivedData()
    {
      CalcElementCenters();
      BuildNodeElements();
      List<string> errors = BuildFaces();
      return errors;
    }

    public void CalcElementCenters()
    {
      _elmtXCenter = new double[NumberOfElements];
      _elmtYCenter = new double[NumberOfElements];
      _elmtZCenter = new double[NumberOfElements];
      for (int ielmt = 0; ielmt < _elementIds.Length; ielmt++)
      {
        int[] nodeInElmt     = _connectivity[ielmt];
        int   numNodesInElmt = nodeInElmt.Length;

        double xc = 0;
        double yc = 0;
        double zc = 0;

        for (int j = 0; j < numNodesInElmt; j++)
        {
          int      nodeIndex = nodeInElmt[j];
          xc += _x[nodeIndex];
          yc += _y[nodeIndex];
          zc += _z[nodeIndex];
        }

        double inumNodesInElmt = 1.0 / numNodesInElmt;
        _elmtXCenter[ielmt] = xc * inumNodesInElmt; // / numNodesInElmt;
        _elmtYCenter[ielmt] = yc * inumNodesInElmt; // / numNodesInElmt;
        _elmtZCenter[ielmt] = zc * inumNodesInElmt; // / numNodesInElmt;
      }
    }

    public void BuildNodeElements()
    {
      _nodesElmts = new List<int>[_nodeIds.Length];

      // Build up element list in nodes
      for (int i = 0; i < _nodeIds.Length; i++)
      {
        _nodesElmts[i] = new List<int>();
      }

      for (int ielmt = 0; ielmt < _elementIds.Length; ielmt++)
      {
        int[]     elmtNodes     = _connectivity[ielmt];
        int       numNodesInElmt = elmtNodes.Length;
        for (int j = 0; j < numNodesInElmt; j++)
        {
          int       meshNode  = elmtNodes[j];
          List<int> nodeElmts = _nodesElmts[meshNode];
          nodeElmts.Add(ielmt);
        }
      }
    }
    public List<string> BuildFaces(bool nodeFaces = false, bool elmtFaces = false, bool newImp = false)
    {
      throw new NotImplementedException();
    }
  }
}