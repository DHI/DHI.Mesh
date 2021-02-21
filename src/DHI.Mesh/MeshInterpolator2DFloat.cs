using System;

namespace DHI.Mesh
{
  // These methods are put in seperate file to ease comparision of float and double version
  public partial class MeshInterpolator2D
  {
    /// <summary>
    /// Interpolate values from source (element values) to target points.
    /// </summary>
    public void InterpolateElmtToTarget(float[] sourceElementValues, float[] target)
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
        InterpElmtNode.Weights interpElmtNode = _targetsElmtNode[i];
        if (interpElmtNode.Element1Index < 0)
        {
          // target not included in source
          target[i] = _deleteValueFloat;
          continue;
        }

        // Do interpolation inside (element-element-node) triangle, 
        // disregarding any delete values.
        double sourceElementValue = sourceElementValues[interpElmtNode.Element1Index];
        if (sourceElementValue != _deleteValue)
        {
          double value  = sourceElementValue * interpElmtNode.Element1Weight;
          double weight = interpElmtNode.Element1Weight;

          {
            double otherElmentValue = sourceElementValues[interpElmtNode.Element2Index];
            if (otherElmentValue != _deleteValue)
            {
              CircularValueHandler.ToReference(_circularType, ref otherElmentValue, sourceElementValue);
              value  += otherElmentValue * interpElmtNode.Element2Weight;
              weight += interpElmtNode.Element2Weight;
            }
          }

          {
            double nodeValue = _nodeValues[interpElmtNode.NodeIndex];
            if (nodeValue != _deleteValue)
            {
              CircularValueHandler.ToReference(_circularType, ref nodeValue, sourceElementValue);
              value  += nodeValue * interpElmtNode.NodeWeight;
              weight += interpElmtNode.NodeWeight;
            }
          }

          value    /= weight;
          CircularValueHandler.ToCircular(_circularType, ref value);
          target[i] =  (float)value;
        }
        else
        {
          target[i] = _deleteValueFloat;
        }
      }
    }


    /// <summary>
    /// Interpolate values from source node values to target points.
    /// </summary>
    public void InterpolateNodeToTarget(double[] sourceNodeValues, float[] target)
    {
      for (int i = 0; i < _targetsNode.Count; i++)
      {
        target[i] = (float)InterpolateNodeToTarget(sourceNodeValues, i);
      }
    }

    /// <summary>
    /// Interpolate values from source node values to target points.
    /// </summary>
    public void InterpolateNodeToTarget(float[] sourceNodeValues, float[] target)
    {
      for (int i = 0; i < _targetsNode.Count; i++)
      {
        target[i] = (float)InterpolateNodeToTarget(sourceNodeValues, i);
      }
    }

    /// <summary>
    /// Interpolate values from source node values to target points.
    /// </summary>
    private double InterpolateNodeToTarget(float[] nodeValues, int i)
    {
      double delVal  = DeleteValueFloat;

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
