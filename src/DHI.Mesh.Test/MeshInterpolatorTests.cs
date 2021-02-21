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
    public void InterpolateToXYExample()
    {
      InterpolateToXYExample(false);
      InterpolateToXYExample(true);
    }

    public void InterpolateToXYExample(bool nodeInterp)
    {
      // Source mesh
      string   triMesh  = UnitTestHelper.TestDataDir + "small.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(triMesh);
      SMeshData mesh     = meshFile.ToSMeshData();
      // Build derived data, required for the interpolation routines
      mesh.BuildDerivedData();

      // Create element center Z values array
      double[] elmtZ = new double[meshFile.NumberOfElements];
      Array.Copy(mesh.ElementZCenter, elmtZ, mesh.NumberOfElements);
      // Make a strong peak at element 5 - in the center of the mesh
      elmtZ[4] = -6;

      // Set up so source can be both element values and node values
      MeshValueType sourceType = MeshValueType.Elements | MeshValueType.Nodes;

      // Mesh interpolator
      MeshInterpolator2D interpolator = new MeshInterpolator2D(mesh, sourceType);
      if (nodeInterp)
        // Simpler interpolation type
        interpolator.ElementValueInterpolationType = MeshInterpolator2D.ElmtValueInterpolationType.NodeValues;

      // Interpolate elmtZ to nodeZ
      double[] nodeZInterp = new double[mesh.NumberOfNodes];
      interpolator.SetupElmtToNodeInterpolation();
      interpolator.NodeInterpolator.Interpolate(elmtZ, nodeZInterp);

      // Interpolation of values one-by-one, no storing of interpolation weights
      Assert.AreEqual(-5.999, interpolator.InterpolateElmtToXY(0.7833, 0.531, elmtZ, nodeZInterp), 1e-3);
      Assert.AreEqual(-3.543, interpolator.InterpolateNodeToXY(0.7833, 0.531, nodeZInterp), 1e-3);

      // Add targets, to store interpolation weights
      interpolator.SetTargetSize(mesh.NumberOfElements+1);
      interpolator.AddTarget(0.7833, 0.531); // Target at (almost) center of element 5
      for (int i = 0; i < mesh.NumberOfElements; i++)
        interpolator.AddTarget(mesh.ElementXCenter[i], mesh.ElementYCenter[i]);

      // Array to interpolate values to
      double[] targetValues = new double[mesh.NumberOfElements+1];
      // Interpolate to all target points
      interpolator.InterpolateElmtToTarget(elmtZ, targetValues);

      if (!nodeInterp)
      {
        // When element+node values are used, close to peak value of 6
        Assert.AreEqual(-5.999,  targetValues[0], 1e-3);
        Assert.AreEqual(-3.8225, targetValues[1], 1e-3);
        for (int i = 0; i < mesh.NumberOfElements; i++)
          Assert.AreEqual(elmtZ[i], targetValues[i+1]);
      }
      else // Using only node interpolation, the value is cut off
      {
        Assert.AreEqual(-3.543, targetValues[0], 1e-3);
        Assert.AreEqual(-3.649, targetValues[1], 1e-3);
      }

      // Interpolating in node Z values, matching to box center value of element.
      interpolator.InterpolateNodeToTarget(mesh.Z, targetValues);
      Assert.AreEqual(-4.376, targetValues[0], 1e-3);
      Assert.AreEqual(-4.376, mesh.ElementZCenter[4], 1e-3);

    }


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
      MeshInterpolator2D interpolator = new MeshInterpolator2D(mesh, MeshValueType.Elements);

      // Coordinates to interpolate values to
      interpolator.SetTargetSize(3);
      interpolator.AddTarget(216600, 6159900);
      interpolator.AddTarget(216700, 6159900);
      interpolator.AddTarget(216700, 6160000);

      // Array to interpolate values to
      double[] targetValues = new double[3];

      // Interpolate element values to target values
      interpolator.InterpolateElmtToTarget(sourceValues, targetValues);

      // Test that values are really 2*x+y
      Assert.AreEqual(2*216600 + 6159900, targetValues[0], 1e-6);
      Assert.AreEqual(2*216700 + 6159900, targetValues[1], 1e-6);
      Assert.AreEqual(2*216700 + 6160000, targetValues[2], 1e-6);
    }

    [Test]
    public void InterpolateNodeTest()
    {
      string triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      // Source mesh
      MeshFile sourcemeshFile = MeshFile.ReadMesh(triMesh);
      SMeshData sourcemesh    = sourcemeshFile.ToSMeshData();
      sourcemesh.BuildDerivedData();
      // Target mesh
      MeshFile targetMeshFile = MeshFile.ReadMesh(quadMesh);
      SMeshData targetmesh    = targetMeshFile.ToSMeshData();
      targetmesh.BuildDerivedData();

      MeshInterpolator2D interpolator = new MeshInterpolator2D(sourcemesh, MeshValueType.Nodes);
      interpolator.SetTarget(targetmesh, MeshValueType.Nodes);

      double[] target = new double[targetmesh.NumberOfNodes];
      interpolator.InterpolateNodeToTarget(sourcemesh.Z, target);

      Assert.False(target.Any(vv => vv == interpolator.DeleteValue));

      targetMeshFile.Z = target;
      targetMeshFile.Write(UnitTestHelper.TestDataDir + "test_odense_rough_quads-fromTri.mesh");

    }

    /// <summary>
    /// Tests that interpolation from element center values to to node values is
    /// second order accurate by specifying a plane function for element center values,
    /// and checking that values are exactly interpolated to nodes.
    /// </summary>
    [Test]
    public void InterpolateElmtToNodeAccuracyTest()
    {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

      string triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      InterpolateElmtToNodeAccuracyTest(triMesh,  CircularValueTypes.Normal);
      InterpolateElmtToNodeAccuracyTest(quadMesh, CircularValueTypes.Normal);
      InterpolateElmtToNodeAccuracyTest(triMesh,  CircularValueTypes.Degrees180);
      InterpolateElmtToNodeAccuracyTest(quadMesh, CircularValueTypes.Degrees180);
      InterpolateElmtToNodeAccuracyTest(triMesh,  CircularValueTypes.Degrees360);
      InterpolateElmtToNodeAccuracyTest(quadMesh, CircularValueTypes.Degrees360);
      InterpolateElmtToNodeAccuracyTest(triMesh,  CircularValueTypes.RadiansPi);
      InterpolateElmtToNodeAccuracyTest(quadMesh, CircularValueTypes.RadiansPi);
      InterpolateElmtToNodeAccuracyTest(triMesh,  CircularValueTypes.Radians2Pi);
      InterpolateElmtToNodeAccuracyTest(quadMesh, CircularValueTypes.Radians2Pi);
    }

    public void InterpolateElmtToNodeAccuracyTest(string meshFileName, CircularValueTypes cvt = CircularValueTypes.Normal)
    {
      // Source mesh
      MeshFile meshFile = MeshFile.ReadMesh(meshFileName);
      MeshData mesh     = meshFile.ToMeshData();
      mesh.BuildDerivedData();

      // Allow for extrapolation on boundary nodes (disable clipping)
      MeshNodeInterpolation interpolation = new MeshNodeInterpolation() { AllowExtrapolation = true,};
      interpolation.Setup(mesh);
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
    public void InterpolationElmtAccuracyTest()
    {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

      string triMesh = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      InterpolationElmtAccuracyTest(triMesh,  quadMesh, CircularValueTypes.Normal);
      InterpolationElmtAccuracyTest(quadMesh, triMesh,  CircularValueTypes.Normal);
      InterpolationElmtAccuracyTest(triMesh,  quadMesh, CircularValueTypes.Degrees180);
      InterpolationElmtAccuracyTest(quadMesh, triMesh,  CircularValueTypes.Degrees180);
      InterpolationElmtAccuracyTest(triMesh,  quadMesh, CircularValueTypes.Degrees360);
      InterpolationElmtAccuracyTest(quadMesh, triMesh,  CircularValueTypes.Degrees360);
      InterpolationElmtAccuracyTest(triMesh,  quadMesh, CircularValueTypes.RadiansPi);
      InterpolationElmtAccuracyTest(quadMesh, triMesh,  CircularValueTypes.RadiansPi);
      InterpolationElmtAccuracyTest(triMesh,  quadMesh, CircularValueTypes.Radians2Pi);
      InterpolationElmtAccuracyTest(quadMesh, triMesh,  CircularValueTypes.Radians2Pi);
    }

    public void InterpolationElmtAccuracyTest(string sourceMeshFileName, string targetMeshFileName, CircularValueTypes cvt = CircularValueTypes.Normal, int[] elmtsToSkipCompare = null)
    {
      // Source mesh
      MeshFile meshFile = MeshFile.ReadMesh(sourceMeshFileName);
      MeshData mesh = meshFile.ToMeshData();
      mesh.BuildDerivedData();

      // Mesh to interpolate to
      MeshFile targetFile = MeshFile.ReadMesh(targetMeshFileName);
      MeshData targetmesh = targetFile.ToMeshData();

      // Setup interpolator
      MeshInterpolator2D interpolator = new MeshInterpolator2D(mesh, MeshValueType.Elements) { CircularType = cvt, AllowExtrapolation = true };
      interpolator.SetupElmtToNodeInterpolation();
      interpolator.SetTarget(targetmesh, MeshValueType.Elements);

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
      interpolator.InterpolateElmtToTarget(elmtVals, targetValues);

      // Check node values
      bool ok = true;
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

        if (elmtsToSkipCompare != null && elmtsToSkipCompare.Contains(i))
          internalElmt = false;

        if (internalElmt && diff > 1e-6 * Math.Max(Math.Abs(exactValue), 1))
        {
          string msg = string.Format("FAIL: {0,2} : {1} - {2} = {3} ({4},{5})", i, exactValue, interpValue, diff, targetElmt.XCenter, targetElmt.YCenter);
          Console.Out.WriteLine(msg);
          ok = false;
        }
        if (!ok)
          Assert.Fail("");
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
