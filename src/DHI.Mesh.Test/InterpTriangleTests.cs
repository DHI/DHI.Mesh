using NUnit.Framework;

namespace DHI.Mesh.Test
{
  [TestFixture]
  public class InterpTriangleTests
  {
    double d = 1e-35;

    /// <summary>
    /// Delete values should influence its 
    /// </summary>
    [Test]
    public void DeleteValueTest()
    {
      double x0 = 0.0;
      double x1 = 1.0;
      double x2 = 0.0;
      double y0 = 0.0;
      double y1 = 0.0;
      double y2 = 1.0;
      double z0 = 0;
      double z1 = 1;
      double z2 = 2;

      // Test handling of delete value on each sid of the 3 mid-line diagonals of the triangle.
      //    P2
      //    |\.
      //    |  `\.
      //    | D2  `\.
      //    |________`\.
      //    |\.   C  | `\.
      //    |  `\.   |    `\.
      //    | D0  `\.|  D1   `\.
      //    L--------V----------
      // P0                   P1
      // When Px is delete value, Dx is delete value area
      // When two delete values are present, also C is delete value area

      DeleteValueTest(false, 0.51,       0,          1, d, 2, x0, y0, x1, y1, x2, y2);
      DeleteValueTest(true , 0.49,       0,          1, d, 2, x0, y0, x1, y1, x2, y2);
      DeleteValueTest(false, 0.5 + 0.01, 0.5 - 0.01, 1, d, 2, x0, y0, x1, y1, x2, y2);
      DeleteValueTest(true,  0.5 - 0.01, 0.5,        1, d, 2, x0, y0, x1, y1, x2, y2);

      // On the line from mid(p0,p1) to mid(p1,p2): w2 = 0.5
      for (int i = 0; i < 9; i++)
      {
        double frac = i / 10.0;
        double x    = 0.5;
        double y    = 0.5 * frac;
        WeightTest(x, y, d, 0.5, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x+0.01, y, 1, d, 2, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true , x-0.01, y, 1, d, 2, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true , x+0.01, y, d, 2, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x-0.01, y, d, 2, d, x0, y0, x1, y1, x2, y2);
      }

      // On the line from mid(p0,p1) to mid(p2,p3): w3 = 0.5
      for (int i = 0; i < 9; i++)
      {
        double frac = i / 10.0;
        double x    = 0.5 * frac;
        double y    = 0.5;
        WeightTest(x, y, d, d, 0.5, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x, y+0.01, 1, 2, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true , x, y-0.01, 1, 2, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true , x, y+0.01, d, d, 3, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x, y-0.01, d, d, 3, x0, y0, x1, y1, x2, y2);
      }

      for (int i = 1; i < 9; i++)
      {
        double frac = i / 10.0;
        double x    = 0.5 - 0.5 * frac;
        double y    = 0.5 * frac;
        WeightTest(x, y, 0.5, d, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x - 0.01, y - 0.01, d, 2, 3, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true , x + 0.01, y + 0.01, d, 2, 3, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true , x - 0.01, y - 0.01, 1, d, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x + 0.01, y + 0.01, 1, d, d, x0, y0, x1, y1, x2, y2);
      }
    }

    void WeightTest(double x, double y, double w0, double w1, double w2, double t0x, double t0y, double t1x, double t1y, double t2x, double t2y)
    {
      InterpTriangle interp = InterpTriangle.InterpolationWeights(x, y, t0x, t0y, t1x, t1y, t2x, t2y);
      Assert.AreEqual(1.0, interp.w0+ interp.w1 + interp.w2, 1e-12);
      if (w0 != d) Assert.AreEqual(w0, interp.w0, 1e-12);
      if (w1 != d) Assert.AreEqual(w1, interp.w1, 1e-12);
      if (w2 != d) Assert.AreEqual(w2, interp.w2, 1e-12);
    }

    void DeleteValueTest(bool valOk, double x, double y, double z0, double z1, double z2, double t0x, double t0y, double t1x, double t1y, double t2x, double t2y)
    {
      InterpTriangle interp = InterpTriangle.InterpolationWeights(x, y, t0x, t0y, t1x, t1y, t2x, t2y);
      double val = interp.GetValue(z0, z1, z2, d);
      if (valOk)
        Assert.AreNotEqual(d, val);
      else
        Assert.AreEqual(d, val);
    }
  }
}
