using System;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfs123;
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

      DeleteValueTest(false, 0.51,       0,          1, d, 2, x0, y0, x1, y1, x2, y2);
      DeleteValueTest(true,  0.49,       0,          1, d, 2, x0, y0, x1, y1, x2, y2);
      DeleteValueTest(false, 0.5 + 0.01, 0.5 - 0.01, 1, d, 2, x0, y0, x1, y1, x2, y2);
      DeleteValueTest(true,  0.5 - 0.01, 0.5,        1, d, 2, x0, y0, x1, y1, x2, y2);

      // On the line from mid(p0,p1) to mid(p1,p2): w2 = 0.5
      for (int i = 0; i < 9; i++)
      {
        double frac = i / 10.0;
        double x    = 0.5;
        double y    = 0.5 * frac;
        WeightTest(x, y, d, 0.5, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x + 0.01, y, 1, d, 2, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true,  x - 0.01, y, 1, d, 2, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true,  x + 0.01, y, d, 2, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x - 0.01, y, d, 2, d, x0, y0, x1, y1, x2, y2);
      }

      // On the line from mid(p0,p1) to mid(p2,p3): w3 = 0.5
      for (int i = 0; i < 9; i++)
      {
        double frac = i / 10.0;
        double x    = 0.5 * frac;
        double y    = 0.5;
        WeightTest(x, y, d, d, 0.5, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x, y + 0.01, 1, 2, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true,  x, y - 0.01, 1, 2, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true,  x, y + 0.01, d, d, 3, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x, y - 0.01, d, d, 3, x0, y0, x1, y1, x2, y2);
      }

      for (int i = 1; i < 9; i++)
      {
        double frac = i / 10.0;
        double x    = 0.5 - 0.5 * frac;
        double y    = 0.5 * frac;
        WeightTest(x, y, 0.5, d, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x - 0.01, y - 0.01, d, 2, 3, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true,  x + 0.01, y + 0.01, d, 2, 3, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(true,  x - 0.01, y - 0.01, 1, d, d, x0, y0, x1, y1, x2, y2);
        DeleteValueTest(false, x + 0.01, y + 0.01, 1, d, d, x0, y0, x1, y1, x2, y2);
      }
    }

    void WeightTest(double x, double y, double w1, double w2, double w3, double t1x, double t1y, double t2x, double t2y,
      double t3x, double t3y)
    {
      InterpTriangle.Weights interp = InterpTriangle.InterpolationWeights(x, y, t1x, t1y, t2x, t2y, t3x, t3y);
      Assert.AreEqual(1.0, interp.w1 + interp.w2 + interp.w3, 1e-12);
      if (w1 != d) Assert.AreEqual(w1, interp.w1, 1e-12);
      if (w2 != d) Assert.AreEqual(w2, interp.w2, 1e-12);
      if (w3 != d) Assert.AreEqual(w3, interp.w3, 1e-12);
    }

    void DeleteValueTest(bool valOk, double x, double y, double z1, double z2, double z3, double t1x, double t1y,
      double t2x, double t2y, double t3x, double t3y)
    {
      InterpTriangle         interpolator = new InterpTriangle() {DelVal = d};
      InterpTriangle.Weights weights      = InterpTriangle.InterpolationWeights(x, y, t1x, t1y, t2x, t2y, t3x, t3y);
      double                 val          = interpolator.GetValue(weights, z1, z2, z3);
      if (valOk)
        Assert.AreNotEqual(d, val);
      else
        Assert.AreEqual(d, val);
    }

    struct MeshWeights
    {
      public int                      ElmtIndex;
      public InterpQuadrangle.Weights QuadWeights;
      public InterpTriangle.Weights   TriWeights;
    }

    private SMeshData _meshVisual;

    [Test]
    public void DeleteValueVisualDfs2Test()
    {
      DeleteValueVisualDfs2DoTest();
    }


    /// <summary>
    /// Create DFS2 file with iterpolated values from the 3x3 quadrangles,
    /// with various delete values applied in each time step.
    /// </summary>
    public void DeleteValueVisualDfs2DoTest()
    {
      string   meshFileName = UnitTestHelper.TestDataDir + "small.mesh";
      MeshFile file         = MeshFile.ReadMesh(meshFileName);

      _meshVisual = file.ToSMeshData();

      DfsFactory  factory     = new DfsFactory();
      Dfs2Builder dfs2Builder = new Dfs2Builder();

      dfs2Builder.SetDataType(0);
      dfs2Builder.SetTemporalAxis(factory.CreateTemporalEqTimeAxis(eumUnit.eumUsec, 0, 1));
      dfs2Builder.SetSpatialAxis(factory.CreateAxisEqD2(eumUnit.eumUmeter, 80, 0, 0.01, 80, 0, 0.01));
      dfs2Builder.SetGeographicalProjection(factory.CreateProjectionUndefined());

      dfs2Builder.AddDynamicItem("DeleteValueSmooth", eumQuantity.UnDefined, DfsSimpleType.Float, DataValueType.Instantaneous);
      dfs2Builder.AddDynamicItem("DeleteValueBox", eumQuantity.UnDefined, DfsSimpleType.Float, DataValueType.Instantaneous);
      dfs2Builder.DeleteValueFloat = (float) d;

      dfs2Builder.CreateFile(UnitTestHelper.TestDataDir + "test_InterpTri.dfs2");

      Dfs2File dfs2File = dfs2Builder.GetFile();

      // Calculate interpolation weights
      MeshWeights[][] weights = new MeshWeights[80][];
      for (int j = 0; j < 80; j++)
      {
        double y = 0.2 + 0.01 * j + 0.005;
        weights[j] = new MeshWeights[80];
        for (int i = 0; i < 80; i++)
        {
          double x = 0.4 + 0.01 * i + 0.005;

          weights[j][i].QuadWeights = InterpQuadrangle.UndefinedWeights();
          weights[j][i].TriWeights  = InterpTriangle.UndefinedWeights();
          for (int ielmt = 0; ielmt < _meshVisual.NumberOfElements; ielmt++)
          {
            var elmtNodes = _meshVisual.ElementTable[ielmt];
            if (elmtNodes.Length == 4)
            {
              double x0 = _meshVisual.X[elmtNodes[0]];
              double x1 = _meshVisual.X[elmtNodes[1]];
              double x2 = _meshVisual.X[elmtNodes[2]];
              double x3 = _meshVisual.X[elmtNodes[3]];
              double y0 = _meshVisual.Y[elmtNodes[0]];
              double y1 = _meshVisual.Y[elmtNodes[1]];
              double y2 = _meshVisual.Y[elmtNodes[2]];
              double y3 = _meshVisual.Y[elmtNodes[3]];
              if (MeshExtensions.IsPointInsideQuadrangle(x, y, x0, y0, x1, y1, x2, y2, x3, y3))
              {
                weights[j][i].ElmtIndex = ielmt;
                weights[j][i].QuadWeights = InterpQuadrangle.InterpolationWeights(x, y, x0, y0, x1, y1, x2, y2, x3, y3);
              }
            }
            else
            {
              double x0 = _meshVisual.X[elmtNodes[0]];
              double x1 = _meshVisual.X[elmtNodes[1]];
              double x2 = _meshVisual.X[elmtNodes[2]];
              double y0 = _meshVisual.Y[elmtNodes[0]];
              double y1 = _meshVisual.Y[elmtNodes[1]];
              double y2 = _meshVisual.Y[elmtNodes[2]];
              if (MeshExtensions.IsPointInsideTriangle(x, y, x0, y0, x1, y1, x2, y2))
              {
                weights[j][i].ElmtIndex   = ielmt;
                weights[j][i].TriWeights = InterpTriangle.InterpolationWeights(x, y, x0, y0, x1, y1, x2, y2);
              }
            }
          }
        }
      }

      // Original center quadrangle values
      double z4 = _meshVisual.Z[3];
      double z6 = _meshVisual.Z[5];
      double z8 = _meshVisual.Z[7];

      float[] data = new float[80 * 80];
      VisualDfs2Data(weights, data, z4, z6, z8, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z4, z6, z8, false); dfs2File.WriteItemTimeStepNext(0, data);

      // One delete value
      VisualDfs2Data(weights, data, d,  z6, z8, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d,  z6, z8, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z4, d,  z8, true ); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z4, d,  z8, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z4, z6, d,  true);  dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z4, z6, d,  false); dfs2File.WriteItemTimeStepNext(0, data);

      // Two adjacent delete values
      VisualDfs2Data(weights, data, d,  d,  z8, true);  dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d,  d,  z8, false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z4, d,  d,  true);  dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, z4, d,  d,  false); dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d,  z6, d,  true);  dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d,  z6, d,  false); dfs2File.WriteItemTimeStepNext(0, data);

      // All delete values
      VisualDfs2Data(weights, data, d, d, d, true);     dfs2File.WriteItemTimeStepNext(0, data);
      VisualDfs2Data(weights, data, d, d, d, false);    dfs2File.WriteItemTimeStepNext(0, data);

      dfs2File.Close();
    }

    private void VisualDfs2Data(MeshWeights[][] weights, float[] data, double v1, double v2,
      double v3, bool sdc)
    {
      InterpQuadrangle interpQ = new InterpQuadrangle();
      interpQ.DelVal           = d;
      interpQ.SmoothDeleteChop = sdc;
      InterpTriangle interpT = new InterpTriangle();
      interpT.DelVal           = d;

      // Replace original quadrangle values for the 4 center nodes with new, 
      // where now some are delete values
      double[] vcoords = new double[_meshVisual.NumberOfNodes];
      Array.Copy(_meshVisual.X, vcoords, _meshVisual.NumberOfNodes);
      vcoords[3] = v1;
      vcoords[5] = v2;
      vcoords[7] = v3;

      int k = 0;
      for (int j = 0; j < 80; j++)
      {
        for (int i = 0; i < 80; i++)
        {
          int   ielmt = weights[j][i].ElmtIndex;
          int[] nodes  = _meshVisual.ElementTable[ielmt];
          if (weights[j][i].QuadWeights.IsDefined)
          {
            double z1 = vcoords[nodes[0]];
            double z2 = vcoords[nodes[1]];
            double z3 = vcoords[nodes[2]];
            double z4 = vcoords[nodes[3]];

            data[k] = (float) interpQ.GetValue(weights[j][i].QuadWeights, z1, z2, z3, z4);
          }
          else if (weights[j][i].TriWeights.IsDefined)
          {
            double z1 = vcoords[nodes[0]];
            double z2 = vcoords[nodes[1]];
            double z3 = vcoords[nodes[2]];
            data[k] = (float)interpT.GetValue(weights[j][i].TriWeights, z1, z2, z3);

          }
          else
            data[k] = -7f;

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