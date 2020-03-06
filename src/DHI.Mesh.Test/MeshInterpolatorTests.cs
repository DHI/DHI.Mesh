using System;
using System.Linq;
using NUnit.Framework;

namespace DHI.Mesh.Test
{
  [TestFixture]
  public class MeshInterpolatorTests
  {
    /// <summary>
    /// Example of how to interpolate from element center values
    /// to arbitrary target values, specified as (x,y) target points.
    /// </summary>
    [Test]
    public void InterpolateElementValuesToXYExample()
    {
      // Source mesh
      string triMesh = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(triMesh);
      MeshData mesh     = meshFile.ToMeshData();
      mesh.BuildDerivedData();

      // Element center values - usually read from dfsu file - here
      // we just calculate some arbitrary linear function of (x,y)
      double[] sourceValues = new double[meshFile.NumberOfElements];
      for (int i = 0; i < meshFile.NumberOfElements; i++)
      {
        sourceValues[i] = 2*mesh.Elements[i].XCenter + mesh.Elements[i].YCenter;
      }

      // Mesh interpolator
      MeshInterpolator2D interpolator = new MeshInterpolator2D(mesh);

      // Coordinates to interpolate values to
      interpolator.SetTargetSize(3);
      interpolator.AddTarget(216600, 6159900);
      interpolator.AddTarget(216700, 6159900);
      interpolator.AddTarget(216700, 6160000);

      // Array to interpolate values to
      double[] targetValues = new double[3];

      // Interpolate element values to target values
      interpolator.InterpolateToTarget(sourceValues, targetValues);

      // Test that values are really 2*x+y
      Assert.AreEqual(2*216600 + 6159900, targetValues[0], 1e-6);
      Assert.AreEqual(2*216700 + 6159900, targetValues[1], 1e-6);
      Assert.AreEqual(2*216700 + 6160000, targetValues[2], 1e-6);
    }


    /// <summary>
    /// Tests that interpolation from element center values to to node values is
    /// second order accurate by specifying a plane function for element center values,
    /// and checking that values are exactly interpolated to nodes.
    /// </summary>
    [Test]
    public void NodeInterpolationAccuracyTest()
    {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

      string triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      NodeInterpolationAccuracyTest(triMesh,  CircularValueTypes.Normal);
      NodeInterpolationAccuracyTest(quadMesh, CircularValueTypes.Normal);
      NodeInterpolationAccuracyTest(triMesh,  CircularValueTypes.Degrees180);
      NodeInterpolationAccuracyTest(quadMesh, CircularValueTypes.Degrees180);
      NodeInterpolationAccuracyTest(triMesh,  CircularValueTypes.Degrees360);
      NodeInterpolationAccuracyTest(quadMesh, CircularValueTypes.Degrees360);
      NodeInterpolationAccuracyTest(triMesh,  CircularValueTypes.RadiansPi);
      NodeInterpolationAccuracyTest(quadMesh, CircularValueTypes.RadiansPi);
      NodeInterpolationAccuracyTest(triMesh,  CircularValueTypes.Radians2Pi);
      NodeInterpolationAccuracyTest(quadMesh, CircularValueTypes.Radians2Pi);
    }

    public void NodeInterpolationAccuracyTest(string meshFileName, CircularValueTypes cvt = CircularValueTypes.Normal)
    {
      // Source mesh
      MeshFile meshFile = MeshFile.ReadMesh(meshFileName);
      MeshData mesh     = meshFile.ToMeshData();
      mesh.BuildDerivedData();

      // Allow for extrapolation on boundary nodes (disable clipping)
      MeshNodeInterpolation interpolation = new MeshNodeInterpolation(mesh) { AllowExtrapolation = true,};
      interpolation.Setup();
      Interpolator nodeInterpolator = interpolation.NodeInterpolator;
      nodeInterpolator.CircularType = cvt;

      // Find reference x and y value as the smallest x and y value
      double xMin = mesh.Nodes.Select(mn => mn.X).Min();
      double xMax = mesh.Nodes.Select(mn => mn.X).Max();
      double yMin = mesh.Nodes.Select(mn => mn.Y).Min();
      double yMax = mesh.Nodes.Select(mn => mn.Y).Max();

      // Function over the (x,y) plane.
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
    /// Tests that interpolation from element center values to point values is
    /// second order accurate by specifying a plane function for element center values,
    /// and checking that values are exactly interpolated to those points.
    /// </summary>
    [Test]
    public void InterpolationAccuracyTest()
    {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

      string triMesh = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      InterpolationAccuracyTest(triMesh, quadMesh, CircularValueTypes.Normal);
      InterpolationAccuracyTest(quadMesh, triMesh, CircularValueTypes.Normal);
      InterpolationAccuracyTest(triMesh, quadMesh, CircularValueTypes.Degrees180);
      InterpolationAccuracyTest(quadMesh, triMesh, CircularValueTypes.Degrees180);
      InterpolationAccuracyTest(triMesh, quadMesh, CircularValueTypes.Degrees360);
      InterpolationAccuracyTest(quadMesh, triMesh, CircularValueTypes.Degrees360);
      InterpolationAccuracyTest(triMesh, quadMesh, CircularValueTypes.RadiansPi);
      InterpolationAccuracyTest(quadMesh, triMesh, CircularValueTypes.RadiansPi);
      InterpolationAccuracyTest(triMesh, quadMesh, CircularValueTypes.Radians2Pi);
      InterpolationAccuracyTest(quadMesh, triMesh, CircularValueTypes.Radians2Pi);
    }

    public void InterpolationAccuracyTest(string sourceMeshFileName, string targetMeshFileName, CircularValueTypes cvt = CircularValueTypes.Normal)
    {
      // Source mesh
      MeshFile meshFile = MeshFile.ReadMesh(sourceMeshFileName);
      MeshData mesh = meshFile.ToMeshData();
      mesh.BuildDerivedData();

      // Mesh to interpolate to
      MeshFile targetFile = MeshFile.ReadMesh(targetMeshFileName);
      MeshData targetmesh = targetFile.ToMeshData();

      // Setup interpolator
      MeshInterpolator2D interpolator = new MeshInterpolator2D(mesh) { CircularType = cvt, AllowExtrapolation = true };
      interpolator.SetupNodeInterpolation();
      interpolator.SetTarget(targetmesh);

      // Find reference x and y value as the smallest x and y value
      double xMin = mesh.Nodes.Select(mn => mn.X).Min();
      double xMax = mesh.Nodes.Select(mn => mn.X).Max();
      double yMin = mesh.Nodes.Select(mn => mn.Y).Min();
      double yMax = mesh.Nodes.Select(mn => mn.Y).Max();

      // Function over the (x,y) plane.
      Func<double, double, double> function = ValueFunction(cvt, xMin, yMin, xMax, yMax);

      // Calculate element center values of function
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
