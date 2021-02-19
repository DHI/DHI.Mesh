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
        InterPElmtNodeData interPElmtNodeData = _targetsElmtNode[i];
        if (interPElmtNodeData.Element1Index < 0)
        {
          // target not included in source
          target[i] = _deleteValue;
          continue;
        }

        // Do interpolation inside (element-element-node) triangle, 
        // disregarding any delete values.
        double sourceElementValue = sourceElementValues[interPElmtNodeData.Element1Index];
        if (sourceElementValue != _deleteValue)
        {
          double value  = sourceElementValue * interPElmtNodeData.Element1Weight;
          double weight = interPElmtNodeData.Element1Weight;

          {
            double otherElmentValue = sourceElementValues[interPElmtNodeData.Element2Index];
            if (otherElmentValue != _deleteValue)
            {
              CircularValueHandler.ToReference(_circularType, ref otherElmentValue, sourceElementValue);
              value  += otherElmentValue * interPElmtNodeData.Element2Weight;
              weight += interPElmtNodeData.Element2Weight;
            }
          }

          {
            double nodeValue = _nodeValues[interPElmtNodeData.NodeIndex];
            if (nodeValue != _deleteValue)
            {
              CircularValueHandler.ToReference(_circularType, ref nodeValue, sourceElementValue);
              value  += nodeValue * interPElmtNodeData.NodeWeight;
              weight += interPElmtNodeData.NodeWeight;
            }
          }

          value    /= weight;
          CircularValueHandler.ToCircular(_circularType, ref value);
          target[i] =  value;
        }
        else
        {
          target[i] = _deleteValue;
        }
      }
    }


    /// <summary>
    /// Interpolate values from source node values to target points.
    /// </summary>
    public void InterpolateNodeToTarget(double[] sourceNodeValues, double[] target)
    {
      double           delVal  = DeleteValueFloat;
      InterpQuadrangle interpQ = new InterpQuadrangle() {DelVal = delVal};
      InterpTriangle   interpT = new InterpTriangle() { DelVal  = delVal};

      for (int i = 0; i < _targetsNode.Count; i++)
      {
        target[i] = (float)InterpolateNodeToTarget(sourceNodeValues, i, interpQ, interpT);
      }
    }

    /// <summary>
    /// Interpolate values from source node values to target points.
    /// </summary>
    private double InterpolateNodeToTarget(double[] nodeValues, int i, InterpQuadrangle interpQ, InterpTriangle interpT)
    {
      double delVal  = DeleteValue;

      InterPNodeData w = _targetsNode[i];
      int elmtIndex = w.ElementIndex;

      if (elmtIndex < 0)
      {
        // target not included in source
        return delVal;
      }

      if (_smesh == null)
        throw new NotSupportedException("Node interpolation is only supported by SMeshData objects");

      int[] elmtNodes = _smesh.ElementTable[elmtIndex];

      double circReference = delVal;
      if (_circularType != CircularValueTypes.Normal)
      {
        for (int j = 0; j < elmtNodes.Length; j++)
        {
          double nodeValue = nodeValues[elmtNodes[j]];
          if (nodeValue != delVal)
          {
            circReference = nodeValue;
            break;
          }
        }
      }

      double res;
      if (elmtNodes.Length == 3)
      {
        double v1 = CircularValueHandler.ToReference(_circularType, nodeValues[elmtNodes[0]], circReference);
        double v2 = CircularValueHandler.ToReference(_circularType, nodeValues[elmtNodes[1]], circReference);
        double v3 = CircularValueHandler.ToReference(_circularType, nodeValues[elmtNodes[2]], circReference);

        res = interpT.GetValue(w.w1, w.w2, w.w3, v1, v2, v3);
      }
      else
      {
        double v1 = CircularValueHandler.ToReference(_circularType, nodeValues[elmtNodes[0]], circReference);
        double v2 = CircularValueHandler.ToReference(_circularType, nodeValues[elmtNodes[1]], circReference);
        double v3 = CircularValueHandler.ToReference(_circularType, nodeValues[elmtNodes[2]], circReference);
        double v4 = CircularValueHandler.ToReference(_circularType, nodeValues[elmtNodes[3]], circReference);

        res = interpQ.GetValue(w.w1, w.w2, v1, v2, v3, v4);
      }

      if (res != delVal)
      {
        CircularValueHandler.ToCircular(_circularType, ref res);
      }

      return res;
    }
  }
}
