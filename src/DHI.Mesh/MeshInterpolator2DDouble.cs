using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Mesh
{
  // These methods are put in seperate file to ease comparision of float and double version
  public partial class MeshInterpolator2D
  {
    /// <summary>
    /// Interpolate values from source (element values) to target points.
    /// </summary>
    public void InterpolateToTarget(double[] sourceElementValues, double[] target)
    {
      // Firstly, interpolate to node values
      _nodeInterpolator.Interpolate(sourceElementValues, _nodeValues);

      for (int i = 0; i < _targets.Count; i++)
      {
        InterPData interPData = _targets[i];
        if (interPData.Element1Index < 0)
        {
          // target not included in source
          target[i] = _deleteValue;
          continue;
        }

        // Do interpolation inside (element-element-node) triangle, 
        // disregarding any delete values.
        double sourceElementValue = sourceElementValues[interPData.Element1Index];
        if (sourceElementValue != _deleteValue)
        {
          double value  = sourceElementValue * interPData.Element1Weight;
          double weight = interPData.Element1Weight;

          {
            double otherElmentValue = sourceElementValues[interPData.Element2Index];
            if (otherElmentValue != _deleteValue)
            {
              CircularValueHandler.ToReference(_circularType, ref otherElmentValue, sourceElementValue);
              value  += otherElmentValue * interPData.Element2Weight;
              weight += interPData.Element2Weight;
            }
          }

          {
            double nodeValue = _nodeValues[interPData.NodeIndex];
            if (nodeValue != _deleteValue)
            {
              CircularValueHandler.ToReference(_circularType, ref nodeValue, sourceElementValue);
              value  += nodeValue * interPData.NodeWeight;
              weight += interPData.NodeWeight;
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
  }
}
