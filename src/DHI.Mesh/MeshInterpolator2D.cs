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
  /// Interpolation points are specified point by point, in the <see cref="SetTarget"/> method,
  /// so it is possible to interpolate to a point, a line or another mesh, by specifying
  /// target points accordingly. 
  /// </para>
  /// </summary>
  public partial class MeshInterpolator2D
  {


    /// <summary>
    /// Enumeration indicating how interpolation from element center
    /// values takes place
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
      /// </summary>
      NodeValues,
    }

    /// <summary>
    /// Data for interpolating value in the triangle of two elements and a node
    /// </summary>
    struct InterPElmtNodeData
    {
      /// <summary> Source element. -1 if not available in source data </summary>
      public int Element1Index;
      /// <summary> Other element, on the other side of the face. For boundary faces, source element value is used. </summary>
      public int Element2Index;
      /// <summary> Node with interpolated value </summary>
      public int NodeIndex;
      /// <summary> Source element value weight </summary>
      public double Element1Weight;
      /// <summary> other element value weight </summary>
      public double Element2Weight;
      /// <summary> node value weight </summary>
      public double NodeWeight;
    }

    /// <summary>
    /// Data for interpolating value in a triangle or quadrangle element using node values.
    /// </summary>
    struct InterPNodeData
    {
      public InterPNodeData(int elmt, double ww1, double ww2, double ww3 = -1)
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

      public static InterPNodeData Undefined()
      {
        return new InterPNodeData(-1, -1, -1, -1);
      }
    }

    public ElmtValueInterpolationType ElementValueInterpolationType
    {
      get { return _elmtValueInterpolationType; }
      set { _elmtValueInterpolationType = value; }
    }
    private ElmtValueInterpolationType _elmtValueInterpolationType = ElmtValueInterpolationType.ElmtNodeValues;

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
      set { _deleteValue = value; }
    }
    /// <summary> Delete/undefined value </summary>
    private double _deleteValue = double.MinValue;

    /// <summary> Delete/undefined value </summary>
    public float DeleteValueFloat
    {
      get { return _deleteValueFloat; }
      set { _deleteValueFloat = value; }
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


    /// <summary> Source mesh </summary>
    private MeshData _mesh;

    private MeshValueType _sourceType;

    /// <summary> Interpolator for interpolating from element to node values in source mesh </summary>
    private Interpolator _nodeInterpolator;

    /// <summary> Target interpolation values </summary>
    private List<InterPElmtNodeData> _targetsElmtNode;

    /// <summary> Target interpolation values </summary>
    private List<InterPNodeData> _targetsNode;

    /// <summary> Searcher, for finding elements for a given coordinate </summary>
    private MeshSearcher _searcher;


    /// <summary>
    /// Create interpolator based on <paramref name="sourceMesh"/>
    /// </summary>
    public MeshInterpolator2D(MeshData sourceMesh, MeshValueType sourceType)
    {
      _mesh       = sourceMesh;
      _sourceType = sourceType;
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
        _targetsNode = new List<InterPNodeData>(targetSize);
      if (ElmtNodeValueInterpolation)
        _targetsElmtNode = new List<InterPElmtNodeData>(targetSize);
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

      if (_searcher == null)
      {
        _searcher = new MeshSearcher(_mesh);
        _searcher.SetupElementSearch();
      }

      // Find element that includes the (x,y) coordinate
      MeshElement        element            = _searcher.FindElement(x, y);

      // Setup interpolation from node values
      if (NodeValueInterpolation)
      {
        InterPNodeData interp;
        if (element != null)
        {
          var nodes = element.Nodes;
          if (nodes.Count == 3)
          {
            double x1 = nodes[0].X;
            double x2 = nodes[1].X;
            double x3 = nodes[2].X;
            double y1 = nodes[0].Y;
            double y2 = nodes[1].Y;
            double y3 = nodes[2].Y;

            var weights = InterpTriangle.InterpolationWeights(x, y, x1, y1, x2, y2, x3, y3);
            interp = new InterPNodeData(element.Index, weights.w1, weights.w2, weights.w3);
          }
          else if (nodes.Count == 4)
          {
            double x1 = nodes[0].X;
            double x2 = nodes[1].X;
            double x3 = nodes[2].X;
            double x4 = nodes[3].X;
            double y1 = nodes[0].Y;
            double y2 = nodes[1].Y;
            double y3 = nodes[2].Y;
            double y4 = nodes[3].Y;

            var weights = InterpQuadrangle.InterpolationWeights(x, y, x1, y1, x2, y2, x3, y3, x4, y4);
            interp = new InterPNodeData(element.Index, weights.dx, weights.dy);
          }
          else
          {
            interp = InterPNodeData.Undefined();
          }
        }
        else
        {
          interp = InterPNodeData.Undefined();
        }
        if (_targetsNode == null)
          _targetsNode = new List<InterPNodeData>();
        _targetsNode.Add(interp);
      }

      // Setup interpolation from element+node values
      if (ElmtNodeValueInterpolation)
      {
        InterPElmtNodeData interpElmtNodeData = new InterPElmtNodeData();
        // Setting "out-of-bounds" index
        interpElmtNodeData.Element1Index = -1;

        // Check if element has been found, i.e. includes the (x,y) point
        if (element != null)
        {
          bool found = false;
          interpElmtNodeData.Element1Index = element.Index;

          // Check which face the point belongs to, and which "side" of the face
          bool isQuad = element.IsQuadrilateral();
          int numFaces = isQuad ? 4 : 3;
          for (int j = 0; j < numFaces; j++)
          {
            MeshFace elementFace = element.Faces[j];
            // From the element (x,y), looking towards the face, 
            // figure out wich node is right and which is left.
            MeshNode rightNode, leftNode;
            if (elementFace.LeftElement == element)
            {
              rightNode = elementFace.FromNode;
              leftNode  = elementFace.ToNode;
            }
            else
            {
              rightNode = elementFace.ToNode;
              leftNode  = elementFace.FromNode;
            }

            double elementXCenter = element.XCenter;
            double elementYCenter = element.YCenter;
            double rightNodeX     = rightNode.X;
            double rightNodeY     = rightNode.Y;
            double leftNodeX      = leftNode.X;
            double leftNodeY      = leftNode.Y;

            // Find also the element on the other side of the face
            double otherElementX, otherElementY;
            MeshElement otherElement = elementFace.OtherElement(element);
            if (otherElement != null)
            {
              otherElementX = otherElement.XCenter;
              otherElementY = otherElement.YCenter;
              interpElmtNodeData.Element2Index = otherElement.Index;
            }
            else
            {
              // No other element - boundary face, use center of face.
              otherElementX = 0.5 * (rightNodeX + leftNodeX);
              otherElementY = 0.5 * (rightNodeY + leftNodeY);
              // Use "itself" as element-2
              interpElmtNodeData.Element2Index = element.Index;
            }


            // Check if point is on the right side of the line between element and other-element
            if (MeshExtensions.IsPointInsideLines(x, y, elementXCenter, elementYCenter, rightNodeX, rightNodeY, otherElementX, otherElementY))
            {
              (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, elementXCenter, elementYCenter, rightNodeX, rightNodeY, otherElementX, otherElementY);
              interpElmtNodeData.NodeIndex = rightNode.Index;
              interpElmtNodeData.Element1Weight = w1;
              interpElmtNodeData.NodeWeight     = w2;
              interpElmtNodeData.Element2Weight = w3;
              found = true;
              break;
            }
            // Check if point is on the left side of the line between element and other-element
            if (MeshExtensions.IsPointInsideLines(x, y, elementXCenter, elementYCenter, otherElementX, otherElementY, leftNodeX, leftNodeY))
            {
              (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, elementXCenter, elementYCenter, otherElementX, otherElementY, leftNodeX, leftNodeY);
              interpElmtNodeData.NodeIndex = leftNode.Index;
              interpElmtNodeData.Element1Weight = w1;
              interpElmtNodeData.Element2Weight = w2;
              interpElmtNodeData.NodeWeight = w3;
              found = true;
              break;
            }
          }

          if (!found) // Should never happen, but just in case
          {
            interpElmtNodeData.Element1Weight = 1;
            interpElmtNodeData.Element2Weight = 0;
            interpElmtNodeData.NodeWeight     = 0;
            interpElmtNodeData.Element2Index  = element.Index;
            interpElmtNodeData.NodeIndex      = element.Nodes[0].Index;
          }
        }
        if (_targetsElmtNode == null)
          _targetsElmtNode = new List<InterPElmtNodeData>();
        _targetsElmtNode.Add(interpElmtNodeData);
      }
    }

  }
}
