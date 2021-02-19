using System;

namespace DHI.Mesh
{
  /// <summary>
  /// Class doing bilinear interpolation on a quadrangle
  /// </summary>
  /// <remarks>
  /// When calling <see cref="GetValue"/>, delete values are handled as follows:
  ///<code>
  /// P3 = T01          P2 = T11
  ///    |-----------------|
  ///    | D3   ./|\.   D2 |
  ///    |    /´  |  `\.   |
  ///    |./´  C3 | C2  `\.|
  ///    |--------|--------|
  ///    |\.   C0 | C1   ./|
  ///    |  `\.   |    /´  |
  ///    | D0  `\.|./´  D1 |
  ///    |-----------------|
  /// P0 = T00          P1 = T10
  /// </code>
  /// Depending on the smooth-delete-chop parameter:
  /// <para>
  /// When smooth version is enabled:
  /// - When a single Px is delete value, Dx is delete value area.
  /// - When two neighboring Px are delete values, their Cx and Dx is delete value area.
  /// - When two diagonal Px are delete values, their Px and all Cx is delete value area.
  /// </para>
  /// <para>
  /// When smooth version is disabled:
  /// - When Px is delete value, Dx and Cx is delete value area.
  /// </para>
  /// </remarks>
  // Code is identical to the MzChart class CMzQuadrangle
  // To keep them it as close as possible, names do not follow the standard C# konventions
  public struct InterpQuadrangle
  {

    /// <summary>
    /// Delete/undefined value
    /// </summary>
    public double DelVal           { get; set; }

    /// <summary>
    /// Specifies whether or not delete value chop-off should be done smoothly
    /// </summary>
    public bool   SmoothDeleteChop { get; set; }

    /// <summary>
    /// Struct holding bilinear interpolation weights for a quadrangle
    /// </summary>
    public struct Weights
    {
      /// <summary> Local quadrangle bilinear x coordinate, [0:1] </summary>
      public double dx;
      /// <summary> Local quadrangle bilinear y coordinate, [0:1] </summary>
      public double dy;

      /// <summary>
      /// Default constructor
      /// </summary>
      /// <param name="dx">Bilinear x coordinate, value between [0;1]</param>
      /// <param name="dy">Bilinear y coordinate, value between [0;1]</param>
      public Weights(double dx, double dy)
      {
        this.dx = dx;
        this.dy = dy;
      }

      /// <summary>
      /// Returns true if the interpolation is defined.
      /// </summary>
      public bool IsDefined
      {
        get { return dx >= 0; }
      }
    }

    /// <summary>
    /// Return a <see cref="Weights"/> structure with undefined weights.
    /// </summary>
    public static Weights UndefinedWeights()
    {
      return new Weights(-1, -1);
    }

    /// <summary>
    /// Calculate bilinear interpolation weights for the point (x,y) inside the
    /// quadrangle defined by the three points (t1,t2,t3).
    /// <para>
    /// The weigts can be used to calculate a value v at the point (x,y)
    /// from values at the four quadrangle points using bilinear interpolation
    /// </para>
    /// <para>
    /// if the point (x,y) is not inside the quadrangle, results are undefined.
    /// </para>
    /// </summary>
    /// <returns>Bilinear interpolation weights (dx,dy)</returns>
    /// <param name="x">Point X coordinate</param>
    /// <param name="y">Point Y coordinate</param>
    /// <param name="t1x">Node 1 X coordinate</param>
    /// <param name="t2x">Node 2 X coordinate</param>
    /// <param name="t3x">Node 3 X coordinate</param>
    /// <param name="t4x">Node 4 X coordinate</param>
    /// <param name="t1y">Node 1 Y coordinate</param>
    /// <param name="t2y">Node 2 Y coordinate</param>
    /// <param name="t3y">Node 3 Y coordinate</param>
    /// <param name="t4y">Node 4 Y coordinate</param>
    // Matching CMzQuadrangle::_Rectify
    public static Weights InterpolationWeights(
      double x,
      double y,
      double t1x,
      double t1y,
      double t2x,
      double t2y,
      double t3x,
      double t3y,
      double t4x,
      double t4y
    ) 
    {
      double dx;
      double dy;

      double m_dA1 = t1x;
      double m_dA2 = t1y;
      double m_dB1 = t2x - t1x;
      double m_dB2 = t2y - t1y;
      double m_dC1 = t4x - t1x;
      double m_dC2 = t4y - t1y;
      double m_dD1 = t3x - t2x + t1x - t4x;
      double m_dD2 = t3y - t2y + t1y - t4y;

      {
        double a = m_dD1 * m_dB2 - m_dD2 * m_dB1;
        double b = m_dD2 * x - m_dD1 * y - m_dD2 * m_dA1 + m_dD1 * m_dA2 + m_dC1 * m_dB2 - m_dC2 * m_dB1;
        double c = m_dC2 * x - m_dC1 * y + m_dC1 * m_dA2 - m_dC2 * m_dA1;

        double dx1;
        double dx2;
        if (0 == a)
        {
          dx1 = 10;
          dx2 = -c/b;
        }
        else
        {
          double D     = Math.Max(b * b - 4 * a * c, 0);
          double sqrtD = Math.Sqrt(D);
          if (b >= 0)
          {
            dx1 = (-b - sqrtD)/(2* a);
            dx2 = (2* c)/(-b - sqrtD);
          }
          else
          {
            dx1 = (-b + sqrtD)/(2* a);
            dx2 = (2* c)/(-b + sqrtD);
          }
        }
  
        if ((0 <= dx1) && (dx1 <= 1))
        {
          dx = dx1;
          if (0 != m_dC1 + m_dD1* dx)
            dy = (x - m_dA1 - m_dB1* dx)/(m_dC1 + m_dD1* dx);
          else
            dy = (y - m_dA2 - m_dB2* dx)/(m_dC2 + m_dD2* dx);
        }
        else
        {
          dx = dx2;
          if (0 != m_dC1 + m_dD1* dx)
            dy = (x - m_dA1 - m_dB1* dx)/(m_dC1 + m_dD1* dx);
          else
            dy = (y - m_dA2 - m_dB2* dx)/(m_dC2 + m_dD2* dx);
        }
      }
      // Getting rid of rounding errors
      dx = Math.Min(Math.Max(dx, 0), 1);
      dy = Math.Min(Math.Max(dy, 0), 1);
      return new Weights(dx, dy);
    }



    /// <summary>
    /// Calculate delete value mask
    /// </summary>
    /// <param name="T00">Node value 1</param>
    /// <param name="T10">Node value 2</param>
    /// <param name="T11">Node value 3</param>
    /// <param name="T01">Node value 4</param>
    // Matching CMzQuadrangle::_CalculateDeleteValueMask
    private int DeleteValueMask(
      double T00,
      double T10,
      double T11,
      double T01
    )
    {
      // ReSharper disable CompareOfFloatsByEqualityOperator
      int deleteValueMask = 0x0;
      if (T00 == DelVal)
        deleteValueMask |= 0x1;
      if (T10 == DelVal)
        deleteValueMask |= 0x2;
      if (T01 == DelVal)
        deleteValueMask |= 0x4;
      if (T11 == DelVal)
        deleteValueMask |= 0x8;
      if (SmoothDeleteChop)
        deleteValueMask |= 0x10;
      return deleteValueMask;
      // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    /// <summary>
    /// Returns interpolated value based on 4 node values
    /// <para>
    /// In case values are one of the circular types in <see cref="CircularValueTypes"/>,
    /// then the values must first be re-referenced, <see cref="CircularValueHandler"/>.
    /// </para>
    /// </summary>
    /// <param name="weights">Bilinear interpolation weights</param>
    /// <param name="T00">Node value 1</param>
    /// <param name="T10">Node value 2</param>
    /// <param name="T11">Node value 3</param>
    /// <param name="T01">Node value 4</param>
    /// <returns>Interpolated value</returns>
    // Matching CMzQuadrangle::GetValue
    public double GetValue(
      Weights weights,
      double T00,
      double T10,
      double T11,
      double T01
    )
    {
      double dx = weights.dx;
      double dy = weights.dy;
      return GetValue(dx, dy, T00, T10, T11, T01);
    }

    /// <summary>
    /// Returns interpolated value based on 4 node values
    /// <para>
    /// In case values are one of the circular types in <see cref="CircularValueTypes"/>,
    /// then the values must first be re-referenced, <see cref="CircularValueHandler"/>.
    /// </para>
    /// </summary>
    /// <param name="dx">Bilinear dx interpolation weights</param>
    /// <param name="dy">Bilinear dy interpolation weights</param>
    /// <param name="T00">Node value 1</param>
    /// <param name="T10">Node value 2</param>
    /// <param name="T11">Node value 3</param>
    /// <param name="T01">Node value 4</param>
    /// <returns>Interpolated value</returns>
    // Matching CMzQuadrangle::GetValue
    public double GetValue(
      double dx, 
      double dy,
      double T00,
      double T10,
      double T11,
      double T01
    )
    {


      int    deleteValueMask = DeleteValueMask(T00, T10, T11, T01);

      const double m_xc = 0.5;
      const double m_yc = 0.5;


      double z;
      switch (deleteValueMask)
      {
        case 0: // Default
          if ((T00 == T10) && (T00 == T01) && (T00 == T11))
            z = T00;
          else
            z = (1 - dx)*(1 - dy)*T00 + dx*(1 - dy)*T10 +
                (1 - dx)* dy     *T01 + dx* dy     *T11;
          break;
        case 1: // Delete value: T00
          if ((dx<m_xc) && (dy<m_yc))
            z = DelVal;
          else
            z = (dx*(1 - dy)*T10 + (1 - dx)*dy* T01 + dx* dy*T11)/
                (dx + dy - dx* dy);
          break;
        case 2: // Delete value: T10
          if ((dx >= m_xc) && (dy<m_yc))
            z = DelVal;
          else
            z = ((1 - dx)*(1 - dy)*T00 + (1 - dx)*dy* T01 + dx* dy*T11)/
                (1 - dx + dx* dy);
          break;
        case 3: // Delete value: T00+T10
          if (dy<m_yc)
            z = DelVal;
          else
            z = ((1 - dx)*T01 + dx* T11);
          break;
        case 4: // Delete value: T01
          if ((dx<m_xc) && (dy >= m_yc))
            z = DelVal;
          else
            z = ((1 - dx)*(1 - dy)*T00 + dx* (1 - dy)*T10 + dx* dy*T11)/
                (1 - dy + dx* dy);
          break;
        case 5: // Delete value: T00+T01
          if (dx<m_xc)
            z = DelVal;
          else
            z = (1 - dy)*T10 + dy* T11;
          break;
        case 6: // Delete value: T10+T01
          if (((dx < m_xc) && (dy >= m_yc)) || ((dx >= m_xc) && (dy < m_yc)))
            z = DelVal;
          else
            z = ((1 - dx) * (1 - dy) * T00 + dx * dy * T11) /
                (1 - dx - dy + 2 * dx * dy);
          break;
        case 7: // Delete value: T00+T10+T01
          if ((dx >= m_xc) && (dy >= m_yc))
            z = T11;
          else
            z = DelVal;
          break;
        case 8: // Delete value: T11
          if ((dx >= m_xc) && (dy >= m_yc))
            z = DelVal;
          else
            z = ((1 - dx)*(1 - dy)*T00 + dx* (1 - dy)*T10 +
                 (1 - dx)*dy* T01)/
                (1 - dx* dy);
          break;
        case 9: // Delete value: T00+T11
          if (((dx<m_xc) && (dy<m_yc)) || ((dx >= m_xc) && (dy >= m_yc)))
            z = DelVal;
          else
            z = (dx * (1 - dy) * T10 + (1 - dx) * dy * T01) /
                (dx + dy - 2 * dx * dy);
          break;
        case 10: // Delete value: T10+T11
          if (dx >= m_xc)
            z = DelVal;
          else
            z = (1 - dy)*T00 + dy* T01;
          break;
        case 11: // Delete value: T00+T10+T11
          if ((dx<m_xc) && (dy >= m_yc))
            z = T01;
          else
            z = DelVal;
          break;
        case 12: // Delete value: T01+T11
          if (dy >= m_yc)
            z = DelVal;
          else
            z = (1 - dx)*T00 + dx* T10;
          break;
        case 13: // Delete value: T00+T01+T11
          if ((dx >= m_xc) && (dy<m_yc))
            z = T10;
          else
            z = DelVal;
          break;
        case 14: // Delete value: T10+T01+T11
          if ((dx<m_xc) && (dy<m_yc))
            z = T00;
          else
            z = DelVal;
          break;
        case 15: // Delete value: T00+T10+T01+T11
          z = DelVal;
          break;

        //-------------------------
        // Smooth delete value chop variants
        // Calculation of z is identical, though the chopping is different
        case 16: // 0: Default
          if ((T00 == T10) && (T00 == T01) && (T00 == T11))
            z = T00;
          else
            z = (1 - dx)*(1 - dy)*T00 + dx*(1 - dy)*T10 +
                (1 - dx)* dy     *T01 + dx* dy     *T11;
          break;
        case 17: // 1: Delete value: T00
          if (dx + dy< 0.5)
            z = DelVal;
          else
            z = (dx*(1 - dy)*T10 + (1 - dx)*dy* T01 + dx* dy*T11)/
                (dx + dy - dx* dy);
          break;
        case 18: // 2: Delete value: T10
          if (dx - dy >= 0.5)
            z = DelVal;
          else
            z = ((1 - dx)*(1 - dy)*T00 + (1 - dx)*dy* T01 + dx* dy*T11)/
                (1 - dx + dx* dy);
          break;
        case 19: // 3: Delete value: T00+T10
          if (dy< 0.5)
            z = DelVal;
          else
            z = ((1 - dx)*T01 + dx* T11);
          break;
        case 20: // 4: Delete value: T01
          if (dx - dy< -0.5)
            z = DelVal;
          else
            z = ((1 - dx)*(1 - dy)*T00 + dx* (1 - dy)*T10 + dx* dy*T11)/
                (1 - dy + dx* dy);
          break;
        case 21: // 5: Delete value: T00+T01
          if (dx< 0.5)
            z = DelVal;
          else
            z = (1 - dy)*T10 + dy* T11;
          break;
        case 22: // 6: Delete value: T10+T01
          if ((dx + dy >= 0.5) && (dx + dy< 1.5))
            z = DelVal;
          else
            z = ((1 - dx) * (1 - dy) * T00 + dx * dy * T11) /
                (1 - dx - dy + 2 * dx * dy);
          break;
        case 23: // 7: Delete value: T00+T10+T01
          if (dx + dy< 1.5)
            z = DelVal;
          else
            z = T11;
          break;
        case 24: // 8: Delete value: T11
          if (dx + dy >= 1.5)
            z = DelVal;
          else
            z = ((1 - dx)*(1 - dy)*T00 + dx* (1 - dy)*T10 +
                 (1 - dx)*dy* T01)/
                (1 - dx* dy);
          break;
        case 25: // 9: Delete value: T00+T11
          if ((dx - dy >= -0.5) && (dx - dy < 0.5))
            z = DelVal;
          else
            z = (dx * (1 - dy) * T10 + (1 - dx) * dy * T01) /
                (dx + dy - 2 * dx * dy);
          break;
        case 26: // 10: Delete value: T10+T11
          if (dx >= 0.5)
            z = DelVal;
          else
            z = (1 - dy)*T00 + dy* T01;
          break;
        case 27: // 11: Delete value: T00+T10+T11
          if (dx - dy > -0.5)
            z = DelVal;
          else
            z = T01;
          break;
        case 28: // 12: Delete value: T01+T11
          if (dy >= 0.5)
            z = DelVal;
          else
            z = (1 - dx)*T00 + dx* T10;
          break;
        case 29: // 13: Delete value: T00+T01+T11
          if (dx - dy< 0.5)
            z = DelVal;
          else
            z = T10;
          break;
        case 30: // 14: Delete value: T10+T01+T11
          if (dx + dy > 0.5)
            z = DelVal;
          else
            z = T00;
          break;
        case 31: // 15: Delete value: T00+T10+T01+T11
          z = DelVal;
          break;
        default:
          z = DelVal;
          break;
      }
      return z;
    }
  }
}