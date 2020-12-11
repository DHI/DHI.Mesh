using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Mesh
{
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
    /// Data for interpolating value in the triangle of two elements and a node
    /// </summary>
    struct InterPData
    {
      /// <summary> Source element </summary>
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

    /// <summary> Source mesh </summary>
    private MeshData _mesh;

    private CircularValueTypes _circularType = CircularValueTypes.Normal;
    private bool _allowExtrapolation;

    /// <summary> Delete/undefined value </summary>
    private double _deleteValue = double.MinValue;
    /// <summary> Delete/undefined value </summary>
    private float _deleteValueFloat = float.MinValue;

    /// <summary> Interpolator for interpolating from element to node values in source mesh </summary>
    private Interpolator _nodeInterpolator;

    /// <summary> Target interpolation values </summary>
    private List<InterPData> _targets;

    /// <summary> Searcher, for finding elements for a given coordinate </summary>
    private MeshSearcher _searcher;

    /// <summary> Node values, interpolated from element values. </summary>
    private double[] _nodeValues;


    /// <summary>
    /// Create interpolator based on <paramref name="sourceMesh"/>
    /// </summary>
    public MeshInterpolator2D(MeshData sourceMesh)
    {
      _mesh = sourceMesh;
      SetupNodeInterpolation();
    }

    /// <summary> Delete/undefined value </summary>
    public double DeleteValue
    {
      get { return _deleteValue; }
      set { _deleteValue = value; }
    }

    /// <summary> Delete/undefined value </summary>
    public float DeleteValueFloat
    {
      get { return _deleteValueFloat; }
      set { _deleteValueFloat = value; }
    }

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

    /// <summary>
    /// Allow extrapolation.
    /// <para>
    /// Default is false.
    /// </para>
    /// </summary>
    public bool AllowExtrapolation
    {
      get { return _allowExtrapolation; }
      set { _allowExtrapolation = value; }
    }

    /// <summary> Node values, interpolated from element values. </summary>
    public double[] NodeValues
    {
      get { return _nodeValues; }
    }

    public void SetupNodeInterpolation()
    {
      if (_nodeInterpolator == null)
      {
        MeshNodeInterpolation interpFactory = new MeshNodeInterpolation();
        interpFactory.AllowExtrapolation = _allowExtrapolation;
        interpFactory.Setup(_mesh);
        _nodeInterpolator              = interpFactory.NodeInterpolator;
        _nodeInterpolator.DeleteValue  = _deleteValue;
        _nodeInterpolator.CircularType = _circularType;
        _nodeValues                    = new double[_mesh.Nodes.Count];
      }
    }

    /// <summary>
    /// Set a target being all elements of the <paramref name="targetMesh"/>
    /// </summary>
    public void SetTarget(MeshData targetMesh)
    {
      SetTargetSize(targetMesh.Elements.Count);
      for (int i = 0; i < targetMesh.Elements.Count; i++)
      {
        MeshElement targetElement = targetMesh.Elements[i];
        AddTarget(targetElement.XCenter, targetElement.YCenter);
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
      SetupNodeInterpolation();
      _targets = new List<InterPData>(targetSize);
    }

    /// <summary>
    /// Add a target, by specifying its (x,y) coordinate.
    /// </summary>
    public void AddTarget(double x, double y)
    {
      if (_targets == null)
        _targets = new List<InterPData>();
      if (_searcher == null)
      {
        _searcher = new MeshSearcher(_mesh);
        _searcher.SetupElementSearch();
      }

      InterPData interpData = new InterPData();
      // Setting "out-of-bounds" index
      interpData.Element1Index = -1;

      // Find element that includes the (x,y) coordinate
      MeshElement element = _searcher.FindElement(x, y);

      // Check if element has been found, i.e. includes the (x,y) point
      if (element != null)
      {
        bool found = false;
        interpData.Element1Index = element.Index;

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

          // Find also the element on the other side of the face
          double otherElementX, otherElementY;
          MeshElement otherElement = elementFace.OtherElement(element);
          if (otherElement != null)
          {
            otherElementX = otherElement.XCenter;
            otherElementY = otherElement.YCenter;
            interpData.Element2Index = otherElement.Index;
          }
          else
          {
            // No other element - boundary face, use center of face.
            otherElementX = 0.5 * (rightNode.X + leftNode.X);
            otherElementY = 0.5 * (rightNode.Y + leftNode.Y);
            // Use "itself" as element-2
            interpData.Element2Index = element.Index;
          }


          // Check if point is on the right side of the line between element and other-element
          if (MeshExtensions.IsPointInsideLines(x, y, element.XCenter, element.YCenter, rightNode.X, rightNode.Y, otherElementX, otherElementY))
          {
            (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, element.XCenter, element.YCenter, rightNode.X, rightNode.Y, otherElementX, otherElementY);
            interpData.NodeIndex = rightNode.Index;
            interpData.Element1Weight = w1;
            interpData.NodeWeight     = w2;
            interpData.Element2Weight = w3;
            found = true;
            break;
          }
          // Check if point is on the left side of the line between element and other-element
          if (MeshExtensions.IsPointInsideLines(x, y, element.XCenter, element.YCenter, otherElementX, otherElementY, leftNode.X, leftNode.Y))
          {
            (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, element.XCenter, element.YCenter, otherElementX, otherElementY, leftNode.X, leftNode.Y);
            interpData.NodeIndex = leftNode.Index;
            interpData.Element1Weight = w1;
            interpData.Element2Weight = w2;
            interpData.NodeWeight     = w3;
            found = true;
            break;
          }
        }

        if (!found) // Should never happen, but just in case
        {
          interpData.Element1Weight = 1;
          interpData.Element2Weight = 0;
          interpData.NodeWeight     = 0;
          interpData.Element2Index  = element.Index;
          interpData.NodeIndex      = element.Nodes[0].Index;
        }
      }
      _targets.Add(interpData);
    }

  }
}
