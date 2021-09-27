using System.Collections.Generic;

namespace DHI.Mesh
{
  public partial class MeshInterpolator2D
  {
    /// <summary>
    /// Create interpolator based on <paramref name="sourceMesh"/>
    /// </summary>
    public MeshInterpolator2D(SMeshData sourceMesh, MeshValueType sourceType)
    {
      _smesh      = sourceMesh;
      _sourceType = sourceType;
      _ssearcher  = new SMeshSearcher(_smesh);
      _ssearcher.SetupElementSearch();
      Init();
    }

    /// <summary> Source mesh </summary>
    private SMeshData _smesh;
    /// <summary> Searcher, for finding elements for a given coordinate </summary>
    private SMeshSearcher _ssearcher;

    /// <summary>
    /// Set a target being all elements of the <paramref name="targetMesh"/>
    /// </summary>
    public void SetTarget(SMeshData targetMesh, MeshValueType targetType)
    {
      if (targetType == MeshValueType.Elements)
      {
        if (targetMesh.ElementXCenter == null)
          targetMesh.CalcElementCenters();

        SetTargetSize(targetMesh.NumberOfElements);
        for (int i = 0; i < targetMesh.NumberOfElements; i++)
        {
          AddTarget(targetMesh.ElementXCenter[i], targetMesh.ElementYCenter[i]);
        }
      }
      else
      {
        SetTargetSize(targetMesh.NumberOfNodes);
        for (int i = 0; i < targetMesh.NumberOfNodes; i++)
        {
          AddTarget(targetMesh.X[i], targetMesh.Y[i]);
        }
      }
    }

    /// <summary>
    /// Add a target, by specifying its (x,y) coordinate.
    /// </summary>
    private void AddSTarget(double x, double y)
    {
      // Find element that includes the (x,y) coordinate
      int element = _ssearcher.FindElement(x, y);

      // Setup interpolation from node values
      if (NodeValueInterpolation)
      {
        InterpNodeData interp;
        if (element >= 0)
        {
          int[] nodes = _smesh.ElementTable[element];
          if (nodes.Length == 3)
          {
            var weights = InterpTriangle.InterpolationWeights(x, y, _smesh, nodes);
            interp = new InterpNodeData(element, weights.w1, weights.w2, weights.w3);
          }
          else if (nodes.Length == 4)
          {
            var weights = InterpQuadrangle.InterpolationWeights(x, y, _smesh, nodes);
            interp = new InterpNodeData(element, weights.dx, weights.dy);
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
        if (element >= 0)
          weights = InterpElmtNode.InterpolationWeights(x, y, element, _smesh);
        else
          weights = InterpElmtNode.Undefined();

        if (_targetsElmtNode == null)
          _targetsElmtNode = new List<InterpElmtNode.Weights>();
        _targetsElmtNode.Add(weights);
      }
    }

    /// <summary>
    /// Interpolate node values to the (x,y) coordinate.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="nodeValues">Node values</param>
    public double InterpolateNodeToXY(double x, double y, float[] nodeValues)
    {
      // Find element that includes the (x,y) coordinate
      int element = _ssearcher.FindElement(x, y);

      // Check if element has been found, i.e. includes the (x,y) point
      if (element >= 0)
      {
        int[] nodes = _smesh.ElementTable[element];
        if (nodes.Length == 3)
        {
          var weights = InterpTriangle.InterpolationWeights(x, y, _smesh, nodes);
          return _interpT.GetValue(weights, nodes, _smesh, nodeValues);
        }
        if (nodes.Length == 4)
        {
          var weights = InterpQuadrangle.InterpolationWeights(x, y, _smesh, nodes);
          return _interpQ.GetValue(weights, nodes, _smesh, nodeValues);
        }
      }
      return DeleteValue;
    }

    /// <summary>
    /// Interpolate node values to the (x,y) coordinate.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="nodeValues">Node values</param>
    public double InterpolateNodeToXY(double x, double y, double[] nodeValues)
    {
      // Find element that includes the (x,y) coordinate
      int element = _ssearcher.FindElement(x, y);

      // Check if element has been found, i.e. includes the (x,y) point
      if (element >= 0)
      {
        int[] nodes = _smesh.ElementTable[element];
        if (nodes.Length == 3)
        {
          var weights = InterpTriangle.InterpolationWeights(x, y, _smesh, nodes);
          return _interpT.GetValue(weights, nodes, _smesh, nodeValues);
        }
        if (nodes.Length == 4)
        {
          var weights = InterpQuadrangle.InterpolationWeights(x, y, _smesh, nodes);
          return _interpQ.GetValue(weights, nodes, _smesh, nodeValues);
        }
      }
      return DeleteValue;
    }

    /// <summary>
    /// Interpolate element values to the (x,y) coordinate.
    /// <para>
    /// It is required to first calculate node values from
    /// element center values. Check out <see cref="NodeInterpolator"/>.
    /// </para>
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="elmtValues">Element center values</param>
    /// <param name="nodeValues">Node values</param>
    public double InterpolateElmtToXY(double x, double y, float[] elmtValues, float[] nodeValues)
    {
      // Find element that includes the (x,y) coordinate
      int element = _ssearcher.FindElement(x, y);

      // Check if element has been found, i.e. includes the (x,y) point
      if (element >= 0)
      {
        InterpElmtNode.Weights weights = InterpElmtNode.InterpolationWeights(x, y, element, _smesh);
        return _interpEN.GetValue(weights, elmtValues, nodeValues);
      }

      return DeleteValue;
    }

    /// <summary>
    /// Interpolate element values to the (x,y) coordinate.
    /// <para>
    /// It is required to first calculate node values from
    /// element center values. Check out <see cref="NodeInterpolator"/>.
    /// </para>
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="elmtValues">Element center values</param>
    /// <param name="nodeValues">Node values</param>
    public double InterpolateElmtToXY(double x, double y, double[] elmtValues, double[] nodeValues)
    {
      // Find element that includes the (x,y) coordinate
      int element = _ssearcher.FindElement(x, y);

      // Check if element has been found, i.e. includes the (x,y) point
      if (element >= 0)
      {
        InterpElmtNode.Weights weights = InterpElmtNode.InterpolationWeights(x, y, element, _smesh);
        return _interpEN.GetValue(weights, elmtValues, nodeValues);
      }

      return DeleteValue;
    }

  }
}