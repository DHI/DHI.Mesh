
namespace DHI.Mesh
{
  // These methods are put in seperate file to ease comparision of float and double version
  public partial class Interpolator
  {
    /// <summary>
    /// Interpolate source values to target
    /// </summary>
    public void Interpolate(float[] sourceValues, double[] targetValues)
    {
      // Loop over all target elements/nodes
      for (int i = 0; i < _interpData.Length; i++)
      {
        InterPData interPData = _interpData[i];

        double value  = 0;
        double weight = 0;

        int[]    indices = interPData.Indices;
        double[] weights = interPData.Weights;

        if (_circularType != CircularValueTypes.Normal)
        {
          // For angular-type data, find reference value
          double refValue = 0;
          for (int j = 0; j < indices.Length; j++)
          {
            double sourceValue = sourceValues[indices[j]];
            if (sourceValue != _deleteValue)
            {
              // First one found is good, break out.
              refValue = sourceValue;
              break;
            }
          }
          // Loop over all source elements connected to target
          for (int j = 0; j < indices.Length; j++)
          {
            double sourceValue = sourceValues[indices[j]];
            if (sourceValue != _deleteValue)
            {
              // For angular type values, correct to match reference value
              CircularValueHandler.ToReference(_circularType, ref sourceValue, refValue);
              value  += sourceValue * weights[j];
              weight += weights[j];
            }
          }
          if (weight == 0) // all element values were delete values
            targetValues[i] = _deleteValue;
          else
          {
            value /= weight;
            // For angular type values, correct to match angular span.
            CircularValueHandler.ToCircular(_circularType, ref value);
            targetValues[i] = value;
          }
        }
        else
        {
          // Loop over all source elements connected to target
          for (int j = 0; j < indices.Length; j++)
          {
            double sourceValue = sourceValues[indices[j]];
            if (sourceValue != _deleteValue)
            {
              value  += sourceValue * weights[j];
              weight += weights[j];
            }
          }
          if (weight == 0) // all element values were delete values
            targetValues[i] = _deleteValue;
          else
          {
            value /= weight;
            targetValues[i] = value;
          }
        }
      }
    }
  }
}