using System.Collections.Generic;

namespace DHI.Mesh
{
  /// <summary>
  /// Mesh value type, where do values live.
  /// <para>
  /// Some algorithm supports both types, hence there both can be set.
  /// </para>
  /// </summary>
  [System.Flags]
  public enum MeshValueType
  {
    /// <summary> Mesh values are element center values </summary>
    Elements = 1,
    /// <summary> Mesh values are node values </summary>
    Nodes = 2,
  }

  /// <summary>
  /// Class for interpolating values from a 2D mesh, where data is defined in
  /// the 2D mesh at the center of the mesh elements.
  /// <para>
  /// Interpolation points are specified point by point, in the <see cref="AddTarget"/> method,
  /// so it is possible to interpolate to a point, a line or another mesh, by specifying
  /// target points accordingly.
  /// </para>
  /// <para>
  /// <see cref="InterpolateNodeToXY(double, double, double[])"/> or <see cref="InterpolateElmtToXY(double, double, double[], double[])"/>.
  /// </para>
  /// </summary>
  public partial class MeshInterpolator2D
  {


    /// <summary>
    /// Enumeration indicating how interpolation from element center
    /// values takes place.
    /// </summary>
    public enum ElmtValueInterpolationType
    {
      /// <summary>
      /// Interpolate element values to nodes, and use
      /// element and node values for the final interpolation.
      /// This is the most accurate interpolation routine.
      /// </summary>
      ElmtNodeValues,
      /// <summary>
      /// Interpolate element values to nodes, and use
      /// noe values for the final interpolation.
      /// This will clip max and min values in element centers,
      /// and is less accurate.
      /// It is the original approach in many of DHI tools.
      /// </summary>
      NodeValues,
    }


    /// <summary>
    /// Data for interpolating value in a triangle or quadrangle element using node values.
    /// </summary>
    struct InterpNodeData
    {
      public InterpNodeData(int elmt, double ww1, double ww2, double ww3 = -1)
      {
        ElementIndex = elmt;
        w1 = ww1;
        w2 = ww2;
        w3 = ww3;
      }
      /// <summary> Source element. -1 if not available in source data </summary>
      public int ElementIndex;
      /// <summary> Weight 1. For quads, this is bilinar dx coordiante [0;1] </summary>
      public double w1;
      /// <summary> Weight 2. For quads, this is bilinar dy coordiante [0;1] </summary>
      public double w2;
      /// <summary> Weight 3. For quads, this is not used </summary>
      public double w3;

      public static InterpNodeData Undefined()
      {
        return new InterpNodeData(-1, -1, -1, -1);
      }
    }

    /// <summary>
    /// Flag indicating how interpolation from element center
    /// values takes place.
    /// </summary>
    public ElmtValueInterpolationType ElementValueInterpolationType
    {
      get { return _elmtValueInterpolationType; }
      set { _elmtValueInterpolationType = value; }
    }
    private ElmtValueInterpolationType _elmtValueInterpolationType = ElmtValueInterpolationType.ElmtNodeValues;

    /// <summary>
    /// Returns true if node interpolation is to be set up.
    /// This is the case when source provides node values,
    /// or if source is element values, but <see cref="ElementValueInterpolationType"/>
    /// is set to <see cref="ElmtValueInterpolationType.NodeValues"/>
    /// </summary>
    private bool NodeValueInterpolation
    {
      get
      {
        return
          _sourceType.HasFlag(MeshValueType.Nodes) ||
          _sourceType.HasFlag(MeshValueType.Elements) &&
          _elmtValueInterpolationType == ElmtValueInterpolationType.NodeValues;
      }
    }

    /// <summary>
    /// Returns true if element-node interpolation is to be set up.
    /// </summary>
    private bool ElmtNodeValueInterpolation
    {
      get
      {
        return
          _sourceType.HasFlag(MeshValueType.Elements) &&
          _elmtValueInterpolationType == ElmtValueInterpolationType.ElmtNodeValues;
      }
    }

    /// <summary> Delete/undefined value </summary>
    public double DeleteValue
    {
      get { return _deleteValue; }
      set
      {
        _deleteValue = value;
        if (_nodeInterpolator != null)
          _nodeInterpolator.DeleteValue = value;
        _interpQ.DelVal  = value;
        _interpT.DelVal  = value;
        _interpEN.DelVal = value;
      }
    }
    /// <summary> Delete/undefined value </summary>
    private double _deleteValue = double.MinValue;

    /// <summary> Delete/undefined value </summary>
    public float DeleteValueFloat
    {
      get { return _deleteValueFloat; }
      set
      {
        _deleteValueFloat = value;
        DeleteValue       = value;
      }
    }
    /// <summary> Delete/undefined value </summary>
    private float _deleteValueFloat = float.MinValue;

    /// <summary>
    /// Type of value, for interpolation of radians and degrees
    /// </summary>
    public CircularValueTypes CircularType
    {
      get { return _circularType; }
      set
      {
        _circularType = value;
        if (_nodeInterpolator != null)
          _nodeInterpolator.CircularType = value;
        _interpQ .CircularType = value;
        _interpT .CircularType = value;
        _interpEN.CircularType = value;
      }
    }
    private CircularValueTypes _circularType = CircularValueTypes.Normal;

    /// <summary>
    /// Allow extrapolation when interpolating element values to nodes.
    /// <para>
    /// Default is false.
    /// </para>
    /// </summary>
    public bool AllowExtrapolation
    {
      get { return _allowExtrapolation; }
      set { _allowExtrapolation = value; }
    }
    private bool _allowExtrapolation;

    /// <summary> Node values, interpolated from element values. </summary>
    public double[] NodeValues
    {
      get { return _nodeValues; }
    }
    /// <summary> Node values, interpolated from element values. </summary>
    private double[] _nodeValues;

    private InterpQuadrangle _interpQ;
    private InterpTriangle   _interpT;
    private InterpElmtNode   _interpEN;

    /// <summary> Source mesh </summary>
    private MeshData _mesh;

    private MeshValueType _sourceType;


    /// <summary> Interpolator for interpolating element center values to node in source mesh </summary>
    public Interpolator NodeInterpolator { get { return _nodeInterpolator; } }
    /// <summary> Interpolator for interpolating from element to node values in source mesh </summary>
    private Interpolator _nodeInterpolator;

    /// <summary> Target interpolation values </summary>
    private List<InterpElmtNode.Weights> _targetsElmtNode;

    /// <summary> Target interpolation values </summary>
    private List<InterpNodeData> _targetsNode;

    /// <summary> Searcher, for finding elements for a given coordinate </summary>
    private MeshSearcher _searcher;


    /// <summary>
    /// Create interpolator based on <paramref name="sourceMesh"/>
    /// </summary>
    public MeshInterpolator2D(MeshData sourceMesh, MeshValueType sourceType)
    {
      _mesh       = sourceMesh;
      _sourceType = sourceType;
      _searcher   = new MeshSearcher(_mesh);
      _searcher.SetupElementSearch();
      Init();
    }

    private void Init()
    {
      _interpQ  = new InterpQuadrangle() { DelVal = DeleteValue };
      _interpT  = new InterpTriangle()   { DelVal   = DeleteValue };
      _interpEN = new InterpElmtNode()  { DelVal   = DeleteValue };
    }


    /// <summary>
    /// Setup interpolation from element center values to node values.
    /// </summary>
    public void SetupElmtToNodeInterpolation()
    {
      if (_nodeInterpolator == null)
      {
        MeshNodeInterpolation interpFactory = new MeshNodeInterpolation();
        interpFactory.AllowExtrapolation = _allowExtrapolation;
        if (_mesh != null)
        {
          interpFactory.Setup(_mesh);
          _nodeValues = new double[_mesh.Nodes.Count];
        }
        else
        {
          interpFactory.Setup(_smesh);
          _nodeValues = new double[_smesh.NumberOfNodes];
        }
        _nodeInterpolator              = interpFactory.NodeInterpolator;
        _nodeInterpolator.DeleteValue  = _deleteValue;
        _nodeInterpolator.CircularType = _circularType;
      }
    }

    /// <summary>
    /// Set a target being all elements of the <paramref name="targetMesh"/>
    /// </summary>
    public void SetTarget(MeshData targetMesh, MeshValueType targetType)
    {
      if (targetType == MeshValueType.Elements)
      {
        SetTargetSize(targetMesh.NumberOfElements);
        for (int i = 0; i < targetMesh.NumberOfElements; i++)
        {
          MeshElement targetMeshElement = targetMesh.Elements[i];
          AddTarget(targetMeshElement.XCenter, targetMeshElement.YCenter);
        }
      }
      else
      {
        SetTargetSize(targetMesh.NumberOfNodes);

        for (int i = 0; i < targetMesh.NumberOfNodes; i++)
        {
          MeshNode targetMeshNode = targetMesh.Nodes[i];
          AddTarget(targetMeshNode.X, targetMeshNode.Y);
        }
      }
    }

    /// <summary>
    /// Initialize size of target.
    /// <para>
    /// Add target points by calling <see cref="AddTarget"/>
    /// </para>
    /// </summary>
    public void SetTargetSize(int targetSize)
    {
      if (_sourceType.HasFlag(MeshValueType.Elements))
        SetupElmtToNodeInterpolation();
      if (NodeValueInterpolation)
        _targetsNode = new List<InterpNodeData>(targetSize);
      if (ElmtNodeValueInterpolation)
        _targetsElmtNode = new List<InterpElmtNode.Weights>(targetSize);
    }

    /// <summary>
    /// Add a target, by specifying its (x,y) coordinate.
    /// </summary>
    public void AddTarget(double x, double y)
    {
      if (_mesh == null)
      {
        AddSTarget(x, y);
        return;
      }

      // Find element that includes the (x,y) coordinate
      MeshElement element = _searcher.FindElement(x, y);

      // Setup interpolation from node values
      if (NodeValueInterpolation)
      {
        InterpNodeData interp;
        // Check if element has been found, i.e. includes the (x,y) point
        if (element != null)
        {
          var nodes = element.Nodes;
          if (nodes.Count == 3)
          {
            var weights = InterpTriangle.InterpolationWeights(x, y, nodes);
            interp = new InterpNodeData(element.Index, weights.w1, weights.w2, weights.w3);
          }
          else if (nodes.Count == 4)
          {
            var weights = InterpQuadrangle.InterpolationWeights(x, y, nodes);
            interp = new InterpNodeData(element.Index, weights.dx, weights.dy);
          }
          else
            interp = InterpNodeData.Undefined();
        }
        else
          interp = InterpNodeData.Undefined();

        if (_targetsNode == null)
          _targetsNode = new List<InterpNodeData>();
        _targetsNode.Add(interp);
      }

      // Setup interpolation from element+node values
      if (ElmtNodeValueInterpolation)
      {
        InterpElmtNode.Weights weights;
        // Check if element has been found, i.e. includes the (x,y) point
        if (element != null)
          weights = InterpElmtNode.InterpolationWeights(x, y, element);
        else
          weights = InterpElmtNode.Undefined();

        if (_targetsElmtNode == null)
          _targetsElmtNode = new List<InterpElmtNode.Weights>();
        _targetsElmtNode.Add(weights);
      }
    }

  }
}
