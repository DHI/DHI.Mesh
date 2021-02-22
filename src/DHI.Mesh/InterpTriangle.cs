
using System.Collections.Generic;

namespace DHI.Mesh
{
  /// <summary>
  /// Class holding interpolation weights for a triangle
  /// </summary>
  /// <remarks>
  /// When calling the <code>GetValue</code> methods, delete values are handled as follows:
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
  /// - When Px is delete value, Dx is delete value area.
  /// - When two delete values are present, also C is delete value area
  /// </remarks>
  public class InterpTriangle
  {

    /// <summary>
    /// Delete/undefined value
    /// </summary>
    public double DelVal { get; set; }

    /// <summary>
    /// Type of value, for interpolation of radians and degrees
    /// </summary>
    public CircularValueTypes CircularType { get; set; } = CircularValueTypes.Normal;

    /// <summary>
    /// Struct holding triangular interpolation weights
    /// </summary>
    public struct Weights
    {
      /// <summary> Weight on first node value</summary>
      public double w1;

      /// <summary> Weight on second node value</summary>
      public double w2;

      /// <summary> Weight on third node value</summary>
      public double w3;

      /// <summary>
      /// Default constructor. Weights must sum to 1.
      /// </summary>
      /// <param name="w1">Weight on first node value, value between [0;1]</param>
      /// <param name="w2">Weight on second node value, value between [0;1]</param>
      /// <param name="w3">Weight on third node value, value between [0;1]</param>
      public Weights(double w1, double w2, double w3)
      {
        this.w1 = w1;
        this.w2 = w2;
        this.w3 = w3;
      }

      /// <summary>
      /// Returns true if the interpolation is defined.
      /// </summary>
      public bool IsDefined
      {
        get { return w1 >= 0; }
      }
    }

    /// <summary>
    /// Return a <see cref="Weights"/> structure with undefined weights.
    /// </summary>
    public static Weights UndefinedWeights()
    {
      return new Weights(-1, -1, -1);
    }

    /// <summary>
    /// Calculate interpolation weights for the point (x,y) inside the triangle defined by
    /// the nodes in <paramref name="elmtNodes"/>.
    ///<para>
    /// Check
    /// <see cref="InterpolationWeights(double,double,double,double,double,double,double,double)"/>
    /// for details.
    /// </para>
    /// </summary>
    /// <param name="x">Point X coordinate</param>
    /// <param name="y">Point Y coordinate</param>
    /// <param name="smesh">MeshData object</param>
    /// <param name="elmtNodes">Nodes in element</param>
    /// <returns>Interpolation weights (w1,w2,w3)</returns>
    public static Weights InterpolationWeights(double x, double y, SMeshData smesh, int[] elmtNodes)
    {
      double x1 = smesh.X[elmtNodes[0]];
      double x2 = smesh.X[elmtNodes[1]];
      double x3 = smesh.X[elmtNodes[2]];
      double y1 = smesh.Y[elmtNodes[0]];
      double y2 = smesh.Y[elmtNodes[1]];
      double y3 = smesh.Y[elmtNodes[2]];
      return InterpTriangle.InterpolationWeights(x, y, x1, y1, x2, y2, x3, y3);
    }

    /// <summary>
    /// Calculate interpolation weights for the point (x,y) inside the triangle defined by
    /// the nodes in <paramref name="elmtNodes"/>.
    ///<para>
    /// Check
    /// <see cref="InterpolationWeights(double,double,double,double,double,double,double,double)"/>
    /// for details.
    /// </para>
    /// </summary>
    /// <param name="x">Point X coordinate</param>
    /// <param name="y">Point Y coordinate</param>
    /// <param name="elmtNodes">Nodes in element</param>
    /// <returns>Interpolation weights (w1,w2,w3)</returns>
    public static Weights InterpolationWeights(double x, double y, IList<MeshNode> elmtNodes)
    {
      double x1 = elmtNodes[0].X;
      double x2 = elmtNodes[1].X;
      double x3 = elmtNodes[2].X;
      double y1 = elmtNodes[0].Y;
      double y2 = elmtNodes[1].Y;
      double y3 = elmtNodes[2].Y;
      return InterpTriangle.InterpolationWeights(x, y, x1, y1, x2, y2, x3, y3);
    }

    /// <summary>
    /// Calculate interpolation weights for the point (x,y) inside the triangle defined by
    /// the three points (t1,t2,t3).
    /// <para>
    /// The weights (w1, w2, w3) returned can be used to calculate a value v at the point (x,y)
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
    public static Weights InterpolationWeights(double x, double y, double t1x, double t1y, double t2x, double t2y, double t3x, double t3y)
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

      return new Weights(w1, w2, w3);
    }


    /// <summary>
    /// Returns interpolated value based on 3 node values
    /// <para>
    /// In case values are one of the circular types in <see cref="CircularValueTypes"/>,
    /// then the values must first be re-referenced, <see cref="CircularValueHandler"/>.
    /// </para>
    /// </summary>
    /// <param name="weights">Triangular interpolation weights</param>
    /// <param name="elmtNodes">Nodes in element</param>
    /// <param name="smesh">Mesh data object</param>
    /// <param name="nodeValues">Node values</param>
    /// <returns>Interpolated value</returns>
    public double GetValue(
      Weights weights,
      int[] elmtNodes,
      SMeshData smesh,
      double[] nodeValues
    )
    {
      double z1 = nodeValues[elmtNodes[0]];
      double z2 = nodeValues[elmtNodes[1]];
      double z3 = nodeValues[elmtNodes[2]];
      return GetValue(weights.w1, weights.w2, weights.w3, z1, z2, z3);
    }

    /// <summary>
    /// Returns interpolated value based on 3 node values
    /// <para>
    /// In case values are one of the circular types in <see cref="CircularValueTypes"/>,
    /// then the values must first be re-referenced, <see cref="CircularValueHandler"/>.
    /// </para>
    /// </summary>
    /// <param name="weights">Triangular interpolation weights</param>
    /// <param name="elmtNodes">Nodes in element</param>
    /// <param name="smesh">Mesh data object</param>
    /// <param name="nodeValues">Node values</param>
    /// <returns>Interpolated value</returns>
    public double GetValue(
      Weights weights,
      int[] elmtNodes,
      SMeshData smesh,
      float[] nodeValues
    )
    {
      double z1 = nodeValues[elmtNodes[0]];
      double z2 = nodeValues[elmtNodes[1]];
      double z3 = nodeValues[elmtNodes[2]];
      return GetValue(weights.w1, weights.w2, weights.w3, z1, z2, z3);
    }

    /// <summary>
    /// Returns interpolated value based on 3 node values
    /// <para>
    /// In case values are one of the circular types in <see cref="CircularValueTypes"/>,
    /// then the values must first be re-referenced, <see cref="CircularValueHandler"/>.
    /// </para>
    /// </summary>
    /// <param name="weights">Triangular interpolation weights</param>
    /// <param name="T1">Node value 1</param>
    /// <param name="T2">Node value 2</param>
    /// <param name="T3">Node value 3</param>
    /// <returns>Interpolated value</returns>
    public double GetValue(
      Weights weights,
      double T1,
      double T2,
      double T3
    )
    {
      return GetValue(weights.w1, weights.w2, weights.w3, T1, T2, T3);
    }

    /// <summary>
    /// Returns interpolated value based on 3 node values
    /// <para>
    /// In case values are one of the circular types in <see cref="CircularValueTypes"/>,
    /// then the values must first be re-referenced, <see cref="CircularValueHandler"/>.
    /// </para>
    /// </summary>
    /// <param name="T1">Node value 1</param>
    /// <param name="T2">Node value 2</param>
    /// <param name="T3">Node value 3</param>
    /// <param name="w1">Weights for node value 1</param>
    /// <param name="w2">Weights for node value 2</param>
    /// <param name="w3">Weights for node value 3</param>
    /// <returns>Interpolated value</returns>
    public double GetValue(
      double w1,
      double w2,
      double w3,
      double T1,
      double T2,
      double T3
    )
    {

      if (CircularType != CircularValueTypes.Normal)
      {
        double circReference = DelVal;
        // Try find circReference value - the first non-delete value
        if (CircularValueHandler.AsReference(T1, ref circReference, DelVal) ||
            CircularValueHandler.AsReference(T2, ref circReference, DelVal) ||
            CircularValueHandler.AsReference(T3, ref circReference, DelVal))
        {
          CircularValueHandler.ToReference(CircularType, ref T1, circReference, DelVal);
          CircularValueHandler.ToReference(CircularType, ref T2, circReference, DelVal);
          CircularValueHandler.ToReference(CircularType, ref T3, circReference, DelVal);
        }
      }

      double value      = 0;
      double weight     = 0;
      int    delValMask = 0;

      // Delete value mask values for each node
      const int dvm1 = 0x1;
      const int dvm2 = 0x2;
      const int dvm3 = 0x4;

      if (T1 != DelVal)
      {
        value  += w1 * T1;
        weight += w1;
      }
      // T1 is delete value - If we are close to this node, return delVal
      else if (w1 > 0.5)
        return DelVal;
      else
        delValMask |= dvm1;

      if (T2 != DelVal)
      {
        value  += w2 * T2;
        weight += w2;
      }
      // T2 is delete value - If we are close to this node, return delVal
      else if (w2 > 0.5)
        return DelVal;
      else
        delValMask |= dvm2;

      if (T3 != DelVal)
      {
        value  += w3 * T3;
        weight += w3;
      }
      // T3 is delete value - If we are close to this node, return delVal
      else if (w3 > 0.5)
        return DelVal;
      else
        delValMask |= dvm3;

      // Check if only one non-delete value is present
      switch (delValMask)
      {
        //case 0: // All had values
        //  return value;

        case dvm1: // Only one delete value, and we are outside the
        case dvm2: // "delete value area".
        case dvm3: // Weight them accordingly
          value = value / weight;
          break;

        case dvm1 + dvm2: // Only T3 is non-delete
          // If we are close to T3, use that, otherwise delVal
          value = (w3 > 0.5) ? T3 : DelVal;
          break;
        case dvm2 + dvm3: // Only T1 is non-delete
          // If we are close to T1, use that, otherwise delVal
          value = (w1 > 0.5) ? T1 : DelVal;
          break;
        case dvm3 + dvm1: // Only T2 is non-delete
          // If we are close to T2, use that, otherwise delVal
          value = (w2 > 0.5) ? T2 : DelVal;
          break;

      }

      CircularValueHandler.ToCircular(CircularType, ref value);
      return value;
    }



  }
}
