using System;
using System.Collections.Generic;
using System.Text;

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

  }
}
