using System;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfs123;
using NUnit.Framework;

namespace DHI.Mesh.Test
{
  [TestFixture]
  public class InterpQuadrangleTests
  {
    /// <summary> Delete value </summary>
    static double d = 1e-35;

    /// <summary>
    /// First test, showing usage of <see cref="InterpQuadrangle"/>
    /// </summary>
    [Test]
    public void FirstTest()
    {

      double x0 = 0.0;
      double x1 = 10.0;
      double x2 = 8.0;
      double x3 = 2.0;
      double y0 = 0.0;
      double y1 = 1.0;
      double y2 = 8.0;
      double y3 = 9.0;
      double z0 = -10.0;
      double z1 = -7.0;
      double z2 = -8.0;
      double z3 = -12.0;

      double x = 5.0;
      double y = 5.0;

      InterpQuadrangle         interpolator = new InterpQuadrangle() { DelVal = d };
      InterpQuadrangle.Weights weights = InterpQuadrangle.InterpolationWeights(x, y, x0, y0, x1, y1, x2, y2, x3, y3);

      double value = interpolator.GetValue(weights, z0, z1, z2, z3);
      Assert.AreEqual(-9.34375, value);

    }


    /// <summary>
    /// Testing that interpolation produces delete values correctly.
    /// </summary>
    [Test]
    public void DeleteValueTests()
    {
      // Vertical line, x = 0.5, y = [0;10] 
      for (int i = 0; i < 19; i++)
      {
        double frac = i / 20.0;
        double x    = 5;
        double y    = 10 * frac;
        WeightTest(x, y);
        DeleteValueTest(false, x + 0.01, y, 1, d, d, 4, false);
        DeleteValueTest(true , x - 0.01, y, 1, d, d, 4, false);
        DeleteValueTest(false, x + 0.01, y, 1, d, d, 4, true);
        DeleteValueTest(true , x - 0.01, y, 1, d, d, 4, true);

        DeleteValueTest(true , x + 0.01, y, d, 2, 3, d, false);
        DeleteValueTest(false, x - 0.01, y, d, 2, 3, d, false);
        DeleteValueTest(true , x + 0.01, y, d, 2, 3, d, true);
        DeleteValueTest(false, x - 0.01, y, d, 2, 3, d, true);

        if (y < 4.99999) // First half of line, P0 and P1 is relevant
        {
          DeleteValueTest(false, x + 0.01, y, 1, d, 3, 4, false);
          DeleteValueTest(true , x - 0.01, y, 1, d, 3, 4, false);

          DeleteValueTest(true , x + 0.01, y, d, 2, 3, 4, false);
          DeleteValueTest(false, x - 0.01, y, d, 2, 3, 4, false);
        }
        if (y > 5.00001) // Second half of line, P2 and P3 is relevant
        {
          DeleteValueTest(false, x + 0.01, y, 1, 2, d, 4, false);
          DeleteValueTest(true , x - 0.01, y, 1, 2, d, 4, false);

          DeleteValueTest(true , x + 0.01, y, 1, 2, 3, d, false);
          DeleteValueTest(false, x - 0.01, y, 1, 2, 3, d, false);
        }
      }

      // Horizontal line, x = [0;10], y = 5 
      for (int i = 0; i < 19; i++)
      {
        double frac = i / 20.0;
        double x    = 10 * frac;
        double y    = 5;
        WeightTest(x, y);
        DeleteValueTest(false, x, y + 0.01, 1, 2, d, d, false);
        DeleteValueTest(true , x, y - 0.01, 1, 2, d, d, false);
        DeleteValueTest(false, x, y + 0.01, 1, 2, d, d, true);
        DeleteValueTest(true , x, y - 0.01, 1, 2, d, d, true);

        DeleteValueTest(true , x, y + 0.01, d, d, 3, 4, false);
        DeleteValueTest(false, x, y - 0.01, d, d, 3, 4, false);
        DeleteValueTest(true , x, y + 0.01, d, d, 3, 4, true);
        DeleteValueTest(false, x, y - 0.01, d, d, 3, 4, true);

        if (x < 4.99999) // First half of line, P0 and P3 is relevant
        {
          DeleteValueTest(false, x, y + 0.01, 1, 2, 3, d, false);
          DeleteValueTest(true , x, y - 0.01, 1, 2, 3, d, false);

          DeleteValueTest(true , x, y + 0.01, d, 2, 3, 4, false);
          DeleteValueTest(false, x, y - 0.01, d, 2, 3, 4, false);
        }
        if (x > 5.00001) // Second half of line, P1 and P2 is relevant
        {
          DeleteValueTest(false, x, y + 0.01, 1, 2, d, 4, false);
          DeleteValueTest(true , x, y - 0.01, 1, 2, d, 4, false);

          DeleteValueTest(true , x, y + 0.01, 1, d, 3, 4, false);
          DeleteValueTest(false, x, y - 0.01, 1, d, 3, 4, false);
        }
      }

      for (int i = 1; i < 9; i++)
      {
        double frac = i / 10.0;
        double x, y;

        // P0 evaluation
        x = 5 * frac;
        y = 5 - 5 * frac;
        WeightTest(x, y);
        DeleteValueTest(true , x + 0.01, y, d, 2, 3, 4, true);
        DeleteValueTest(false, x - 0.01, y, d, 2, 3, 4, true);

        DeleteValueTest(false, x + 0.01, y, 1, d, d, d, true);
        DeleteValueTest(true , x - 0.01, y, 1, d, d, d, true);
        // Diagonal delete value - same as above
        DeleteValueTest(false, x + 0.01, y, 1, d, 3, d, true);
        DeleteValueTest(true , x - 0.01, y, 1, d, 3, d, true);

        // P1 evaluation
        x = 5 + 5 * frac;
        y = 5 * frac;
        WeightTest(x, y);
        DeleteValueTest(false, x + 0.01, y, 1, d, 3, 4, true);
        DeleteValueTest(true , x - 0.01, y, 1, d, 3, 4, true);

        DeleteValueTest(true , x + 0.01, y, d, 2, d, d, true);
        DeleteValueTest(false, x - 0.01, y, d, 2, d, d, true);
        // Diagonal delete value - same as above
        DeleteValueTest(true , x + 0.01, y, d, 2, d, 4, true);
        DeleteValueTest(false, x - 0.01, y, d, 2, d, 4, true);

        // P2 evaluation
        x = 10 - 5 * frac;
        y =  5 + 5 * frac;
        WeightTest(x, y);
        DeleteValueTest(false, x + 0.01, y, 1, 2, d, 4, true);
        DeleteValueTest(true , x - 0.01, y, 1, 2, d, 4, true);

        DeleteValueTest(true , x + 0.01, y, d, d, 3, d, true);
        DeleteValueTest(false, x - 0.01, y, d, d, 3, d, true);
        // Diagonal delete value - same as above
        DeleteValueTest(true , x + 0.01, y, 1, d, 3, d, true);
        DeleteValueTest(false, x - 0.01, y, 1, d, 3, d, true);

        // P3 evaluation
        x = 5 * frac;
        y = 5 + 5 * frac;
        WeightTest(x, y);
        DeleteValueTest(true , x + 0.01, y, 1, 2, 3, d, true);
        DeleteValueTest(false, x - 0.01, y, 1, 2, 3, d, true);

        DeleteValueTest(false, x + 0.01, y, d, d, d, 4, true);
        DeleteValueTest(true , x - 0.01, y, d, d, d, 4, true);
        // Diagonal delete value - same as above
        DeleteValueTest(false, x + 0.01, y, d, 2, d, 4, true);
        DeleteValueTest(true , x - 0.01, y, d, 2, d, 4, true);

      }
    }

    void DeleteValueTest(bool valOk, double x, double y, double z0, double z1, double z2, double z3, bool sdc = true)
    {
      InterpQuadrangle         interpolator = new InterpQuadrangle() { DelVal = d, SmoothDeleteChop = sdc };
      InterpQuadrangle.Weights weights      = InterpQuadrangle.InterpolationWeights(x, y, 0, 0, 10, 0, 10, 10, 0, 10);
      double                   val          = interpolator.GetValue(weights, z0, z1, z2, z3);
      if (valOk)
        Assert.AreNotEqual(d, val);
      else
        Assert.AreEqual(d, val);
    }

    /// <summary>
    /// Test data for 3x3 quadrangles
    /// </summary>
    double[] xcoords = new[]
    {
      10, 11.0, 17.0, 20.0,
      9, 11.0, 18.0, 21.0,
      7, 10.0, 20.0, 22.0,
      6, 10.0, 21.0, 24.0,
    };

    double[] ycoords = new[]
    {
      24.0, 23.0, 20.0, 21.0,
      21.0, 20.0, 18.0, 18.0,
      11.0, 10.0, 13.0, 14.0,
      8.0,  6.0, 9.0,  9.0,
    };

    double[] zcoords = new[]
    {
      13, 13  ,  10,  6,
      13, 12.0,  8.0, 6,
      12, 10.0,  7.0, 6,
      11, 11.0,  6,   6,
    };

    /// <summary>
    /// Indexing from bottom left corner
    /// </summary>
    public static int ind(int ir, int jr)
    {
      return ir + (3 - jr) * 4;
    }

    struct QuadWeights
    {
      public int IR;
      public int JR;
      public InterpQuadrangle.Weights Weights;
    }

    [Test]
    public void DeleteValueVisualDfs2Test()
    {
      DeleteValueVisualDfs2Test(true);
      DeleteValueVisualDfs2Test(false);
    }

    /// <summary>
    /// Create DFS2 file with interpolated values from the 3x3 quadrangles,
    /// with various delete values applied in each time step.
    /// </summary>
    /// <param name="centerOnly">Only use center quadrangle</param>
    public void DeleteValueVisualDfs2Test(bool centerOnly)
    {

      DfsFactory  factory     = new DfsFactory();
      Dfs2Builder dfs2Builder = new Dfs2Builder();

      dfs2Builder.SetDataType(0);
      dfs2Builder.SetTemporalAxis(factory.CreateTemporalEqTimeAxis(eumUnit.eumUsec, 0, 1));
      dfs2Builder.SetSpatialAxis(factory.CreateAxisEqD2(eumUnit.eumUmeter, 200, 0, 0.1, 200, 0, 0.1));
      dfs2Builder.SetGeographicalProjection(factory.CreateProjectionUndefined());

      dfs2Builder.AddDynamicItem("DeleteValueSmooth", eumQuantity.UnDefined, DfsSimpleType.Float , DataValueType.Instantaneous);
      dfs2Builder.AddDynamicItem("DeleteValueBox", eumQuantity.UnDefined, DfsSimpleType.Float , DataValueType.Instantaneous);
      dfs2Builder.DeleteValueFloat = (float) d;

      if (centerOnly)
        dfs2Builder.CreateFile(UnitTestHelper.TestDataDir + "test_InterpQuadCenter.dfs2");
      else
        dfs2Builder.CreateFile(UnitTestHelper.TestDataDir + "test_InterpQuad.dfs2");

      Dfs2File dfs2File = dfs2Builder.GetFile();

      // Calculate interpolation weights
      QuadWeights[][] weights = new QuadWeights[200][];
      for (int j = 0; j < 200; j++)
      {
        double y = 5+ 0.1*j + 0.05;
        weights[j] = new QuadWeights[200];
        for (int i = 0; i < 200; i++)
        {
          double x = 5+0.1*i + 0.05;

          weights[j][i].Weights = InterpQuadrangle.UndefinedWeights();
          for (int jr = 0; jr < 3; jr++)
          {
            for (int ir = 0; ir < 3; ir++)
            {
              if (centerOnly && (jr != 1 || ir != 1))
                continue;

              double x0 = xcoords[ind(ir  ,jr  )];
              double x1 = xcoords[ind(ir+1,jr  )];
              double x2 = xcoords[ind(ir+1,jr+1)];
              double x3 = xcoords[ind(ir  ,jr+1)];
              double y0 = ycoords[ind(ir  ,jr  )];
              double y1 = ycoords[ind(ir+1,jr  )];
              double y2 = ycoords[ind(ir+1,jr+1)];
              double y3 = ycoords[ind(ir  ,jr+1)];
              if (MeshExtensions.IsPointInsideQuadrangle(x, y, x0, y0, x1, y1, x2, y2, x3, y3))
              {
                weights[j][i].IR = ir;
                weights[j][i].JR = jr;
                weights[j][i].Weights = InterpQuadrangle.InterpolationWeights(x, y, x0, y0, x1, y1, x2, y2, x3, y3);
              }
            }
          }
        }
      }

      // Original center quadrangle values
      double  z0   = zcoords[ind(1  ,1  )];
      double  z1   = zcoords[ind(1+1,1  )];
      double  z2   = zcoords[ind(1+1,1+1)];
      double  z3   = zcoords[ind(1  ,1+1)];

      float[] data = new float[200*200];
      VisualDfs2Data(weights, data, z0, z1, z2, z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, z1, z2, z3, false); dfs2File.WriteItemTimeStepNext(0, data);

      // One delete value
      VisualDfs2Data(weights, data, d , z1, z2, z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , z1, z2, z3, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , z2, z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , z2, z3, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, z1, d , z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, z1, d , z3, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, z1, z2, d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, z1, z2, d , false); dfs2File.WriteItemTimeStepNext(0, data);

      // Two adjacent delete values
      VisualDfs2Data(weights, data, d , d , z2, z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , d , z2, z3, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , d , z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , d , z3, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, z1, d , d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, z1, d , d , false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , z1, z2, d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , z1, z2, d , false); dfs2File.WriteItemTimeStepNext(0, data);

      // Two diagonal delete values
      VisualDfs2Data(weights, data, d , z1, d , z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , z1, d , z3, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , z2, d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , z2, d , false); dfs2File.WriteItemTimeStepNext(0, data);

      // Three delete values
      VisualDfs2Data(weights, data, d , d , d , z3, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , d , d , z3, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , d , z2, d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , d , z2, d , false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , z1, d , d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , z1, d , d , false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , d , d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z0, d , d , d , false); dfs2File.WriteItemTimeStepNext(0, data);

      // All delete values
      VisualDfs2Data(weights, data, d , d , d , d , true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d , d , d , d , false); dfs2File.WriteItemTimeStepNext(0, data);

      dfs2File.Close();
    }

    private void VisualDfs2Data(QuadWeights[][] weights, float[] data, double v0, double v1, double v2, double v3, bool sdc)
    {
      InterpQuadrangle interpolator = new InterpQuadrangle();
      interpolator.DelVal           = d;
      interpolator.SmoothDeleteChop = sdc;

      // Replace original quadrangle values for the 4 center nodes with new, 
      // where now some are delete values
      double[] vcoords = new double[zcoords.Length];
      Array.Copy(zcoords, vcoords, zcoords.Length);
      vcoords[ind(1  ,1  )] = v0;
      vcoords[ind(1+1,1  )] = v1;
      vcoords[ind(1+1,1+1)] = v2;
      vcoords[ind(1  ,1+1)] = v3;

      int k = 0;
      for (int j = 0; j < 200; j++)
      {
        for (int i = 0; i < 200; i++)
        {
          if (weights[j][i].Weights.IsDefined)
          {
            int    ir = weights[j][i].IR;
            int    jr = weights[j][i].JR;
            double z0 = vcoords[ind(ir  ,jr  )];
            double z1 = vcoords[ind(ir+1,jr  )];
            double z2 = vcoords[ind(ir+1,jr+1)];
            double z3 = vcoords[ind(ir  ,jr+1)];

            data[k] = (float)interpolator.GetValue(weights[j][i].Weights, z0, z1, z2, z3);
          }
          else if (i > 100) // Some arbitrary out-of-bounds values
            data[k] = (float)13;
          else
            data[k] = (float)6;
          k++;
        }
      }

    }

    void WeightTest(double x, double y)
    {
      InterpQuadrangle.Weights interp = InterpQuadrangle.InterpolationWeights(x, y, 0, 0, 10, 0, 10, 10, 0, 10);
      Assert.AreEqual(x * 0.1, interp.dx, 1e-12);
      Assert.AreEqual(y * 0.1, interp.dy, 1e-12);
    }


  }
}
