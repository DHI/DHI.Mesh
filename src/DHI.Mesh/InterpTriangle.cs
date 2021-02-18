using System.Collections.Generic;
using System.Text;

namespace DHI.Mesh
{
  /// <summary>
  /// Class holding interpolation weights for a triangle
  /// </summary>
  /// <remarks>
  /// When calling <see cref="GetValue"/>, delete values are handled as follows:
  ///<code>
  ///  P2
  ///    |\.
  ///    |  `\.
  ///    | D2  `\.
  ///    |________`\.
  ///    |\.    C | `\.
  ///    |  `\.   |    `\.
  ///    | D0  `\.|  D1   `\.
  ///    L--------V----------
  ///  P0                   P1
  /// </code>
  /// When Px is delete value, Dx is delete value area
  /// When two delete values are present, also C is delete value area
  /// </remarks>
  public struct InterpTriangle
  {
    /// <summary> Weight on first node value</summary>
    public double w0;
    /// <summary> Weight on second node value</summary>
    public double w1;
    /// <summary> Weight on third node value</summary>
    public double w2;

    /// <summary>
    /// Default constructor. Weights must sum to 1.
    /// </summary>
    /// <param name="w0">Weight on first node value, value between [0;1]</param>
    /// <param name="w1">Weight on second node value, value between [0;1]</param>
    /// <param name="w2">Weight on third node value, value between [0;1]</param>
    public InterpTriangle(double w0, double w1, double w2)
    {
      this.w0 = w0;
      this.w1 = w1;
      this.w2 = w2;
    }

    /// <summary>
    /// Calculate interpolation weights for the point (x,y) inside the triangle defined by
    /// the three points (t1,t2,t3).
    /// <para>
    /// The weigts (w1, w2, w3) returned can be used to calculate a value v at the point (x,y)
    /// from values at the three triangle points (v1,v2,v3) as
    /// <code>
    ///    v = w1*v1 + w2*v2 + w3*v3;
    /// </code>
    /// </para>
    /// <para>
    /// if the point (x,y) is not inside the triangle, results are undefined.
    /// </para>
    /// </summary>
    /// <returns>Interpolation weights (w1,w2,w3)</returns>
    public static InterpTriangle InterpolationWeights(double x, double y, double t1x, double t1y, double t2x, double t2y, double t3x, double t3y)
    {
      double denom = ((t2y - t3y) * (t1x - t3x) + (t3x - t2x) * (t1y - t3y));
      double w1    = ((t2y - t3y) * (x - t3x) + (t3x - t2x) * (y - t3y)) / denom;
      double w2    = ((t3y - t1y) * (x - t3x) + (t1x - t3x) * (y - t3y)) / denom;

      if (w1 < 0) w1 = 0;
      if (w2 < 0) w2 = 0;
      double w12     = w1 + w2;
      if (w12 > 1)
      {
        w1 /= w12;
        w2 /= w12;
      }

      double w3 = 1 - w1 - w2;

      return new InterpTriangle(w1, w2, w3);
    }


    public double GetValue(
      double T0,
      double T1,
      double T2,
      double delVal
    )
    {
      double value      = 0;
      double weight     = 0;
      int    delValMask = 0;

      // Delete value mask values for each node
      const int dvm0 = 0x1;
      const int dvm1 = 0x2;
      const int dvm2 = 0x4;

      if (T0 != delVal)
      {
        value  += w0 * T0;
        weight += w0;
      }
      // T0 is delete value - If we are close to this node, return delVal
      else if (w0 > 0.5)
        return delVal;
      else
        delValMask |= dvm0;

      if (T1 != delVal)
      {
        value  += w1 * T1;
        weight += w1;
      }
      // T1 is delete value - If we are close to this node, return delVal
      else if (w1 > 0.5)
        return delVal;
      else
        delValMask |= dvm1;

      if (T2 != delVal)
      {
        value  += w2 * T2;
        weight += w2;
      }
      // T2 is delete value - If we are close to this node, return delVal
      else if (w2 > 0.5)
        return delVal;
      else
        delValMask |= dvm2;

      // Check if only one non-delete value is present
      switch (delValMask)
      {
        case 0: // All had values
          return value;

        case dvm0: // Only one delete value, and we are outside the
        case dvm1: // "delete value area".
        case dvm2: // Weight them accordingly
          return value / weight;

        case dvm0 + dvm1: // Only T2 is non-delete
          // If we are close to T2, use that, otherwise delVal
          return (w2 > 0.5) ? T2 : delVal;
        case dvm1 + dvm2: // Only T0 is non-delete
          // If we are close to T0, use that, otherwise delVal
          return (w0 > 0.5) ? T0 : delVal;
        case dvm2 + dvm0: // Only T1 is non-delete
          // If we are close to T1, use that, otherwise delVal
          return (w1 > 0.5) ? T1 : delVal;

      }

      // Should never get down here, but we need to return something...
      return delVal;
    }



  }
}
