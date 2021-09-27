using System;

namespace DHI.Mesh
{
  // These methods are put in seperate file to ease comparision of float and double version
  public partial class MeshInterpolator2D
  {
    /// <summary>
    /// Interpolate values from source (element values) to target points.
    /// </summary>
    public void InterpolateElmtToTarget(double[] sourceElementValues, double[] target)
    {
      // Firstly, interpolate to node values
      _nodeInterpolator.Interpolate(sourceElementValues, _nodeValues);

      if (_elmtValueInterpolationType == ElmtValueInterpolationType.NodeValues)
      {
        InterpolateNodeToTarget(_nodeValues, target);
        return;
      }

      for (int i = 0; i < _targetsElmtNode.Count; i++)
      {
        InterpElmtNode.Weights weights = _targetsElmtNode[i];
        if (weights.Element1Index < 0)
        {
          // target not included in source
          target[i] = _deleteValue;
        }
        else
          target[i] =        _interpEN.GetValue(weights,
                                                sourceElementValues[weights.Element1Index],
                                                sourceElementValues[weights.Element2Index],
                                                _nodeValues[weights.NodeIndex]);
      }
    }


    /// <summary>
    /// Interpolate values from source node values to target points.
    /// </summary>
    public void InterpolateNodeToTarget(double[] sourceNodeValues, double[] target)
    {
      for (int i = 0; i < _targetsNode.Count; i++)
      {
        target[i] = (float)InterpolateNodeToTarget(sourceNodeValues, i);
      }
    }

    /// <summary>
    /// Interpolate values from source node values to target points.
    /// </summary>
    private double InterpolateNodeToTarget(double[] nodeValues, int i)
    {
      double delVal  = DeleteValue;

      InterpNodeData w = _targetsNode[i];
      int elmtIndex = w.ElementIndex;

      if (elmtIndex < 0)
      {
        // target not included in source
        return delVal;
      }

      if (_smesh == null)
        throw new NotSupportedException("Node interpolation is only supported by SMeshData objects");

      int[] elmtNodes = _smesh.ElementTable[elmtIndex];

      double res;
      if (elmtNodes.Length == 3)
      {
        double v1 = nodeValues[elmtNodes[0]];
        double v2 = nodeValues[elmtNodes[1]];
        double v3 = nodeValues[elmtNodes[2]];

        res = _interpT.GetValue(w.w1, w.w2, w.w3, v1, v2, v3);
      }
      else
      {
        double v1 = nodeValues[elmtNodes[0]];
        double v2 = nodeValues[elmtNodes[1]];
        double v3 = nodeValues[elmtNodes[2]];
        double v4 = nodeValues[elmtNodes[3]];

        res = _interpQ.GetValue(w.w1, w.w2, v1, v2, v3, v4);
      }

      return res;
    }
  }
}
