using System;

namespace DHI.Mesh
{

  /// <summary>
  /// Type of circular value
  /// </summary>
  public enum CircularValueTypes
  {
    Normal,
    /// <summary> Value between -180 and 180 </summary>
    Degrees180,
    /// <summary> Value between 0 and 360 </summary>
    Degrees360,
    /// <summary> Value between -pi and pi </summary>
    RadiansPi,
    /// <summary> Value between 0 and 2*pi </summary>
    Radians2Pi,
  }

  /// <summary>
  /// Handling of circular values
  /// </summary>
  public class CircularValueHandler
  {

    /// <summary>
    /// Check if <paramref name="sourceValue"/> can be used as reference value,
    /// and update <paramref name="refValue"/>. 
    /// </summary>
    /// <param name="sourceValue">Source value</param>
    /// <param name="refValue">Reference value</param>
    /// <param name="delVal">Delete value</param>
    /// <returns>True if <paramref name="refValue"/> was updated</returns>
    public static bool AsReference(double sourceValue, ref double refValue, double delVal)
    {
      if (sourceValue != delVal)
      {
        refValue = sourceValue;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Make <paramref name="sourceValue"/> sufficiently close to <paramref name="refValue"/>
    /// in order to interpolate on angular values.
    /// </summary>
    public static void ToReference(CircularValueTypes circularType, ref double sourceValue, double refValue)
    {
      switch (circularType)
      {
        case CircularValueTypes.RadiansPi:
        case CircularValueTypes.Radians2Pi:
          if      (sourceValue - refValue >  Math.PI)
            sourceValue -= 2 * Math.PI;
          else if (sourceValue - refValue < -Math.PI)
            sourceValue += 2 * Math.PI;
          break;
        case CircularValueTypes.Degrees180:
        case CircularValueTypes.Degrees360:
          if      (sourceValue - refValue >  180.0)
            sourceValue -= 360.0;
          else if (sourceValue - refValue < -180.0)
            sourceValue += 360.0;
          break;
      }
    }

    /// <summary>
    /// Make <paramref name="sourceValue"/> sufficiently close to <paramref name="refValue"/>
    /// in order to interpolate on angular values.
    /// </summary>
    public static double ToReference(CircularValueTypes circularType, double sourceValue, double refValue)
    {
      switch (circularType)
      {
        case CircularValueTypes.RadiansPi:
        case CircularValueTypes.Radians2Pi:
          if      (sourceValue - refValue >  Math.PI)
            return sourceValue - 2 * Math.PI;
          else if (sourceValue - refValue < -Math.PI)
            return sourceValue + 2 * Math.PI;
          break;
        case CircularValueTypes.Degrees180:
        case CircularValueTypes.Degrees360:
          if      (sourceValue - refValue >  180.0)
            return sourceValue - 360.0;
          else if (sourceValue - refValue < -180.0)
            return sourceValue + 360.0;
          break;
      }
      return sourceValue;
    }

    /// <summary>
    /// Make <paramref name="value"/> within limits of circular value.
    /// </summary>
    public static void ToCircular(CircularValueTypes circularType, ref double value)
    {
      switch (circularType)
      {
        case CircularValueTypes.RadiansPi:
          if      (value >  Math.PI)
            value -= 2 * Math.PI;
          else if (value < -Math.PI)
            value += 2 * Math.PI;
          break;
        case CircularValueTypes.Radians2Pi:
          if      (value > 2 * Math.PI)
            value -= 2 * Math.PI;
          else if (value < 0)
            value += 2 * Math.PI;
          break;
        case CircularValueTypes.Degrees180:
          if      (value >  180.0)
            value -= 360.0;
          else if (value < -180.0)
            value += 360.0;
          break;
        case CircularValueTypes.Degrees360:
          if      (value > 360.0)
            value -= 360.0;
          else if (value < 0.0)
            value += 360.0;
          break;
      }
    }

    /// <summary>
    /// Make <paramref name="value"/> within limits of circular value.
    /// </summary>
    public static double ToCircular(CircularValueTypes circularType, double value)
    {
      switch (circularType)
      {
        case CircularValueTypes.RadiansPi:
          if (value > Math.PI)
            value -= 2 * Math.PI;
          else if (value < -Math.PI)
            value += 2 * Math.PI;
          break;
        case CircularValueTypes.Radians2Pi:
          if (value > 2 * Math.PI)
            value -= 2 * Math.PI;
          else if (value < 0)
            value += 2 * Math.PI;
          break;
        case CircularValueTypes.Degrees180:
          if (value > 180.0)
            value -= 360.0;
          else if (value < -180.0)
            value += 360.0;
          break;
        case CircularValueTypes.Degrees360:
          if (value > 360.0)
            value -= 360.0;
          else if (value < 0.0)
            value += 360.0;
          break;
      }

      return value;
    }

  }
}
