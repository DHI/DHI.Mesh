using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DHI.Mesh.Test
{
  [TestFixture]
  public class MeshInterpolatorTests
  {

    /// <summary>
    /// Test that interpolation to node values is second order accurate
    /// by specifying a plane function for element center values,
    /// and checking that values are exactly interpolated to nodes.
    /// </summary>
    [Test]
    public void NodeInterpolationTest()
    {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

      string triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      //NodeInterpolationTest(triMesh,  CircularValueTypes.Normal);
      //NodeInterpolationTest(quadMesh, CircularValueTypes.Normal);
      NodeInterpolationTest(triMesh,  CircularValueTypes.Degrees180);
      NodeInterpolationTest(quadMesh, CircularValueTypes.Degrees180);
      NodeInterpolationTest(triMesh,  CircularValueTypes.Degrees360);
      NodeInterpolationTest(quadMesh, CircularValueTypes.Degrees360);
      NodeInterpolationTest(triMesh,  CircularValueTypes.RadiansPi);
      NodeInterpolationTest(quadMesh, CircularValueTypes.RadiansPi);
      NodeInterpolationTest(triMesh,  CircularValueTypes.Radians2Pi);
      NodeInterpolationTest(quadMesh, CircularValueTypes.Radians2Pi);
    }

    public void NodeInterpolationTest(string meshFileName, CircularValueTypes cvt = CircularValueTypes.Normal)
    {
      MeshFile meshFile = MeshFile.ReadMesh(meshFileName);
      MeshData mesh     = meshFile.ToMeshData();

      // Disable clipping, allow for extrapolation on boundary nodes
      MeshNodeInterpolation interpolation = new MeshNodeInterpolation(mesh) { AllowExtrapolation = true,};
      interpolation.Setup();
      Interpolator nodeInterpolator = interpolation.NodeInterpolator;
      nodeInterpolator.CircularType = cvt;

      // Find reference x and y value as the smallest x and y value
      double xMin = mesh.Nodes.Select(mn => mn.X).Min();
      double xMax = mesh.Nodes.Select(mn => mn.X).Max();
      double yMin = mesh.Nodes.Select(mn => mn.Y).Min();
      double yMax = mesh.Nodes.Select(mn => mn.Y).Max();

      Func<double, double, double> function = ValueFunction(cvt, xMin, yMin, xMax, yMax);

      // Calculate element center values
      double[] elmtVals   = new double[mesh.Elements.Count];
      for (int i = 0; i < mesh.Elements.Count; i++)
      {
        MeshElement elmt = mesh.Elements[i];
        elmtVals[i] = function(elmt.XCenter, elmt.YCenter);
      }

      // Write out bounds, to check we got things right
      Console.Out.WriteLine("{0,10} (min,max) = ({1},{2})", cvt, elmtVals.Min(), elmtVals.Max());

      // Interpolate to nodes
      double[] nodeValues = new double[mesh.Nodes.Count];
      nodeInterpolator.Interpolate(elmtVals, nodeValues);

      // Check node values
      for (int i = 0; i < mesh.Nodes.Count; i++)
      {
        MeshNode node       = mesh.Nodes[i];
        double   exactValue = function(node.X, node.Y);
        double interpValue  = nodeValues[i];
        double   diff       = exactValue - interpValue ;
        // It can only extrapolate when there is at least three elements per node.
        // When there is two or less elements, the inverse distance weighting takes over
        // and the results are not correct, so we skip the check here.
        if (node.Elements.Count > 2 && diff > 1e-6)
        {
          string msg = string.Format("{0,2} {6}: {1}-{2}={3} ({4},{5})", i, exactValue, interpValue, diff, node.X, node.Y, node.Elements.Count );
          Console.Out.WriteLine(msg);
          Assert.Fail(msg);
        }
      }
    }

    /// <summary>
    /// Test that interpolation to point values is second order accurate
    /// by specifying a plane function for element center values,
    /// and checking that values are exactly interpolated to those points.
    /// </summary>
    [Test]
    public void InterpolationTest()
    {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

      string triMesh = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      InterpolationTest(triMesh, quadMesh, CircularValueTypes.Normal);
      InterpolationTest(quadMesh, triMesh, CircularValueTypes.Normal);
      InterpolationTest(triMesh, quadMesh, CircularValueTypes.Degrees180);
      InterpolationTest(quadMesh, triMesh, CircularValueTypes.Degrees180);
      InterpolationTest(triMesh, quadMesh, CircularValueTypes.Degrees360);
      InterpolationTest(quadMesh, triMesh, CircularValueTypes.Degrees360);
      InterpolationTest(triMesh, quadMesh, CircularValueTypes.RadiansPi);
      InterpolationTest(quadMesh, triMesh, CircularValueTypes.RadiansPi);
      InterpolationTest(triMesh, quadMesh, CircularValueTypes.Radians2Pi);
      InterpolationTest(quadMesh, triMesh, CircularValueTypes.Radians2Pi);
    }

    public void InterpolationTest(string sourceMeshFileName, string targetMeshFileName, CircularValueTypes cvt = CircularValueTypes.Normal)
    {
      MeshFile meshFile = MeshFile.ReadMesh(sourceMeshFileName);
      MeshData mesh = meshFile.ToMeshData();
      mesh.BuildDerivedData();

      MeshFile targetFile = MeshFile.ReadMesh(targetMeshFileName);
      MeshData targetmesh = targetFile.ToMeshData();

      MeshInterpolator2D interpolator = new MeshInterpolator2D(mesh) { CircularType = cvt, AllowExtrapolation = true };
      interpolator.SetupNodeInterpolation();
      interpolator.SetTarget(targetmesh);

      // Find reference x and y value as the smallest x and y value
      double xMin = mesh.Nodes.Select(mn => mn.X).Min();
      double xMax = mesh.Nodes.Select(mn => mn.X).Max();
      double yMin = mesh.Nodes.Select(mn => mn.Y).Min();
      double yMax = mesh.Nodes.Select(mn => mn.Y).Max();

      Func<double, double, double> function = ValueFunction(cvt, xMin, yMin, xMax, yMax);

      // Calculate element center values
      double[] elmtVals = new double[mesh.Elements.Count];
      for (int i = 0; i < mesh.Elements.Count; i++)
      {
        MeshElement elmt = mesh.Elements[i];
        elmtVals[i] = function(elmt.XCenter, elmt.YCenter);
      }

      // Write out bounds, to check we got things right
      Console.Out.WriteLine("{0,10} (min,max) = ({1},{2})", cvt, elmtVals.Min(), elmtVals.Max());

      // Interpolate to nodes
      double[] targetValues = new double[targetmesh.Elements.Count];
      interpolator.InterpolateToTarget(elmtVals, targetValues);

      // Check node values
      for (int i = 0; i < targetmesh.Elements.Count; i++)
      {
        MeshElement targetElmt = targetmesh.Elements[i];
        double exactValue = function(targetElmt.XCenter, targetElmt.YCenter);
        double interpValue = targetValues[i];
        double diff = exactValue - interpValue;

        // Check if target element has a boundary node.
        // Nodes on the boundary may not have correctly interpolated value due to 
        // inverse distance interpolation on the boundary, and hence also interpolation
        // to target element value will not be exact. So only check on those elements that are
        // fully internal (no boundary nodes).
        bool internalElmt = targetElmt.Nodes.Select(node => node.Code).All(code => code == 0);

        if (internalElmt && diff > 1e-6 * Math.Max(Math.Abs(exactValue), 1))
        {
          string msg = string.Format("{0,2} : {1}-{2}={3} ({4},{5})", i, exactValue, interpValue, diff, targetElmt.XCenter, targetElmt.YCenter);
          Console.Out.WriteLine(msg);
          Assert.Fail(msg);
        }
      }
    }

    /// <summary>
    /// Function that varies over the plane depending on the circular type
    /// </summary>
    private static Func<double, double, double> ValueFunction(CircularValueTypes cvt, double xMin, double yMin, double xMax, double yMax)
    {
      Func<double, double, double> function;
      switch (cvt)
      {
        case CircularValueTypes.Normal:
          function = (x, y) => 0.0001 * ((x - xMin) + 2.0 * (y - yMin));
          break;
        case CircularValueTypes.Degrees180:
          function = (x, y) => -180 + (360 * (x - xMin) / (xMax - xMin) + 360 * (y - yMin) / (yMax - yMin)) % 360.0;
          break;
        case CircularValueTypes.Degrees360:
          function = (x, y) => (360 * (x - xMin) / (xMax - xMin) + 360 * (y - yMin) / (yMax - yMin)) % 360.0;
          break;
        case CircularValueTypes.RadiansPi:
          function = (x, y) => -Math.PI + (2 * Math.PI * (x - xMin) / (xMax - xMin) + 2 * Math.PI * (y - yMin) / (yMax - yMin)) % (2 * Math.PI);
          break;
        case CircularValueTypes.Radians2Pi:
          function = (x, y) => (2 * Math.PI * (x - xMin) / (xMax - xMin) + 2 * Math.PI * (y - yMin) / (yMax - yMin)) % (2 * Math.PI);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(cvt), cvt, null);
      }

      return function;
    }
  }
}
