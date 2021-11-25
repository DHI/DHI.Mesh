using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfsu;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DHI.Mesh.Test
{
  [TestFixture]
  public class MeshBoundaryTests
  {
    /// <summary>
    /// The result of this test can be plotted in GnuPlot using the commands below
    /// </summary>
    /*
    set style line 1 linecolor rgb '#00ad60' linetype 1 linewidth 1 pointtype 7 pointsize 1.5
    set style line 2 linecolor rgb '#0060ad' linetype 1 linewidth 1 pointtype 5 pointsize 1
    set size ratio -1
    plot "out_odense_rough-gp-bndcode.txt" with lines linestyle 1, '' index 0 with lines linestyle 2
      */

    #region MeshBoundary tests

    [Test]
    public void BoundaryListSMeshTest()
    {
      string   triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      List<SMeshBoundary> boundaries = BoundaryListSMeshTest(triMesh);

      Assert.AreEqual(2, boundaries.Count);
      Assert.AreEqual(1, boundaries[0].Code);
      Assert.AreEqual(2, boundaries[1].Code);

      Assert.AreEqual(2, boundaries[0].Segments.Count);
      Assert.AreEqual(1, boundaries[1].Segments.Count);
      Assert.AreEqual(9, boundaries[1].Segments[0].Count);

      // First node of the first code-1 boundary segment is the last node of the code-2 boundary segment
      Assert.IsTrue(boundaries[0].Segments[0][0].FromNode == boundaries[1].Segments[0].Last().ToNode);
      Assert.IsTrue(boundaries[0].Segments[0].Last().ToNode == boundaries[1].Segments[0][0].FromNode);
    }

    [Test]
    public void BoundaryListMeshTest()
    {
      string              triMesh    = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      List<MeshBoundary> boundaries = BoundaryListMeshTest(triMesh);

      Assert.AreEqual(2, boundaries.Count);
      Assert.AreEqual(1, boundaries[0].Code);
      Assert.AreEqual(2, boundaries[1].Code);

      Assert.AreEqual(2, boundaries[0].Segments.Count);
      Assert.AreEqual(1, boundaries[1].Segments.Count);
      Assert.AreEqual(9, boundaries[1].Segments[0].Count);

      // First node of the first code-1 boundary segment is the last node of the code-2 boundary segment
      Assert.IsTrue(boundaries[0].Segments[0][0].FromNode == boundaries[1].Segments[0].Last().ToNode);
      Assert.IsTrue(boundaries[0].Segments[0].Last().ToNode == boundaries[1].Segments[0][0].FromNode);
    }


    [Test]
    [Explicit("Run Manual")]
    public void BoundaryListMeshMedium()
    {
      string triMesh = @"C:\Work\DHIGitHub\DHI.Mesh\TestData\MaxWD-PostDev.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: " + triMesh);
      BoundaryListMeshTest(triMesh);
    }

    [Test]
    [Explicit("Run Manual")]
    public void BoundaryListSMeshMedium()
    {
      string triMesh = @"C:\Work\DHIGitHub\DHI.Mesh\TestData\MaxWD-PostDev.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: " + triMesh);
      BoundaryListSMeshTest(triMesh);
    }

    [Test]
    [Explicit("Run Manual")]
    public void BoundaryListMeshBig()
    {
      string triMesh = @"C:\Work\TestData\BigDfsu.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: "+triMesh);
      BoundaryListMeshTest(triMesh);
    }

    [Test]
    [Explicit("Run Manual")]
    public void BoundaryListSMeshBig()
    {
      string triMesh = @"C:\Work\TestData\BigDfsu.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: " + triMesh);
      BoundaryListSMeshTest(triMesh);
    }

    public List<MeshBoundary> BoundaryListMeshTest(string meshPath)
    {
      string fileName = Path.GetFileName(meshPath);

      Stopwatch timer;
      MeshData mesh;
      if (Path.GetExtension(meshPath) == ".dfsu")
      {
        DfsuFile file = DfsFileFactory.DfsuFileOpen(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh = MeshData.CreateMesh(file.Projection.WKTString, file.NodeIds, file.X, file.Y, file.Z.ToDoubleArray(), file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased());
        file.Close();
      }
      else
      {
        MeshFile meshFile = MeshFile.ReadMesh(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh  = meshFile.ToMeshData();
      }
      Console.Out.WriteLine("(#nodes,#elmts)=({0},{1}) ({2})", mesh.NumberOfNodes, mesh.NumberOfElements, mesh.NumberOfNodes + mesh.NumberOfElements);
      timer.ReportAndRestart("Create");

      List<MeshBoundary> boundaries = mesh.BuildBoundaryList();
      timer.ReportAndRestart("Time  ");

      string gpFileName = UnitTestHelper.TestDataDir + "test_"+fileName+"-bndcode.txt";
      GnuPlotWriteBoundaryList(gpFileName, boundaries);

      return boundaries;
    }

    public List<SMeshBoundary> BoundaryListSMeshTest(string meshPath)
    {
      string fileName = Path.GetFileName(meshPath);

      Stopwatch timer;
      SMeshData mesh;
      if (Path.GetExtension(meshPath) == ".dfsu")
      {
        DfsuFile file = DfsFileFactory.DfsuFileOpen(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh = SMeshData.CreateMesh(file.Projection.WKTString, file.NodeIds, file.X, file.Y, file.Z.ToDoubleArray(), file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased());
        file.Close();
      }
      else
      {
        MeshFile meshFile = MeshFile.ReadMesh(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh  = meshFile.ToSMeshData();
      }
      Console.Out.WriteLine("(#nodes,#elmts)=({0},{1}) ({2})", mesh.NumberOfNodes, mesh.NumberOfElements, mesh.NumberOfNodes + mesh.NumberOfElements);

      timer.ReportAndRestart("Create");

      List<SMeshBoundary> boundaries = mesh.BuildBoundaryList();
      timer.ReportAndRestart("Time  ");

      string gpFileName = UnitTestHelper.TestDataDir + "test_" + fileName + "-bndscode.txt";
      GnuPlotWriteBoundaryList(mesh, gpFileName, boundaries);

      return boundaries;
    }

    private static void GnuPlotWriteBoundaryList(string gpFileName, List<MeshBoundary> boundaries)
    {
      StreamWriter writer = new StreamWriter(gpFileName);
      foreach (MeshBoundary meshBoundary in boundaries)
      {
        writer.WriteLine("# " + meshBoundary.Code);
        foreach (List<MeshFace> segment in meshBoundary.Segments)
        {
          writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", segment[0].FromNode.X,
            segment[0].FromNode.Y));
          foreach (MeshFace face in segment)
          {
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", face.ToNode.X, face.ToNode.Y));
          }
          writer.WriteLine(""); // New segment
        }
        writer.WriteLine(""); // New index for each boundary code value
      }
      writer.Close();
    }

    private static void GnuPlotWriteBoundaryList(SMeshData mesh, string gpFileName, List<SMeshBoundary> boundaries)
    {
      StreamWriter writer = new StreamWriter(gpFileName);
      foreach (SMeshBoundary meshBoundary in boundaries)
      {
        writer.WriteLine("# " + meshBoundary.Code);
        foreach (List<SMeshFace> segment in meshBoundary.Segments)
        {
          writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", mesh.X[segment[0].FromNode], mesh.Y[segment[0].FromNode]));
          foreach (SMeshFace face in segment)
          {
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", mesh.X[face.ToNode], mesh.Y[face.ToNode]));
          }
          writer.WriteLine(""); // New segment
        }
        writer.WriteLine(""); // New index for each boundary code value
      }
      writer.Close();
    }

    #endregion

    #region Boundary Polygon tests

    [Test]
    public void BoundaryPolygonMeshOdenseTest()
    {
      string    triMesh       = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      IGeometry boundaryGeom  = BoundaryPolygonMeshTest(triMesh, false);
      IGeometry boundaryGeom2 = BoundaryPolygonMeshTest(triMesh, true, "2");

      Polygon boundaryPoly = boundaryGeom as Polygon;
      Assert.AreEqual(1, boundaryPoly.Holes.Length);
    }

    [Explicit("Run Manual")]
    [Test]
    public void BoundaryPolygonMeshDfsuBigTest()
    {
      string triMesh = @"C:\Work\TestData\BigDfsu.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: " + triMesh);
      BoundaryPolygonMeshTest(triMesh);
    }
    [Test]
    [Explicit("Run Manual")]
    public void BoundaryPolygonMeshDfsuMediumTest()
    {
      string triMesh = @"C:\Work\DHIGitHub\DHI.Mesh\TestData\MaxWD-PostDev.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: " + triMesh);
      BoundaryPolygonMeshTest(triMesh);
    }


    [Test]
    public void BoundaryPolygonSMeshOdenseTest()
    {
      string    triMesh      = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      IGeometry boundaryGeom  = BoundaryPolygonSMeshTest(triMesh, false);
      IGeometry boundaryGeom2 = BoundaryPolygonSMeshTest(triMesh, true, "2");

      Polygon boundaryPoly = boundaryGeom as Polygon;
      Assert.AreEqual(1, boundaryPoly.Holes.Length);
    }
    [Test]
    [Explicit("Run Manual")]
    public void BoundaryPolygonSMeshDfsuBigTest()
    {
      string triMesh = @"C:\Work\TestData\BigDfsu.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: " + triMesh);
      BoundaryPolygonSMeshTest(triMesh);
    }
    [Test]
    [Explicit("Run Manual")]
    public void BoundaryPolygonSMeshDfsuMediumTest()
    {
      string triMesh = @"C:\Work\DHIGitHub\DHI.Mesh\TestData\MaxWD-PostDev.dfsu";
      if (!File.Exists(triMesh)) Assert.Ignore("Not found: " + triMesh);
      BoundaryPolygonSMeshTest(triMesh);
    }

    public IGeometry BoundaryPolygonMeshTest(string meshPath, bool alwaysMultiPolygon = true, string extra = "")
    {

      string fileName = Path.GetFileName(meshPath);

      Stopwatch timer;
      MeshData  mesh;
      if (Path.GetExtension(meshPath) == ".dfsu")
      {
        DfsuFile file = DfsFileFactory.DfsuFileOpen(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh = MeshData.CreateMesh(file.Projection.WKTString, file.NodeIds, file.X, file.Y, file.Z.ToDoubleArray(), file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased());
        file.Close();
      }
      else
      {
        MeshFile meshFile = MeshFile.ReadMesh(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh  = meshFile.ToMeshData();
      }
      Console.Out.WriteLine("(#nodes,#elmts)=({0},{1}) ({2})", mesh.NumberOfNodes, mesh.NumberOfElements, mesh.NumberOfNodes + mesh.NumberOfElements);

      timer.ReportAndRestart("Create ");

      IGeometry boundaryGeom = mesh.BuildBoundaryGeometry(alwaysMultiPolygon);
      timer.ReportAndRestart("Build  ");

      BoundaryPolygonWriter(fileName, "-bnd" + extra, boundaryGeom, timer);
      return boundaryGeom;
    }

    public IGeometry BoundaryPolygonSMeshTest(string meshPath, bool alwaysMultiPolygon = true, string extra = "")
    {
      string   fileName = Path.GetFileName(meshPath);

      Stopwatch timer;
      SMeshData mesh;
      if (Path.GetExtension(meshPath) == ".dfsu")
      {
        DfsuFile  file  = DfsFileFactory.DfsuFileOpen(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh = SMeshData.CreateMesh(file.Projection.WKTString, file.NodeIds, file.X, file.Y, file.Z.ToDoubleArray(), file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased());
        file.Close();
      }
      else
      {
        MeshFile  meshFile = MeshFile.ReadMesh(meshPath);
        timer = MeshExtensions.StartTimer();
        mesh     = meshFile.ToSMeshData();
      }

      Console.Out.WriteLine("(#nodes,#elmts)=({0},{1}) ({2})", mesh.NumberOfNodes, mesh.NumberOfElements, mesh.NumberOfNodes + mesh.NumberOfElements);
      timer.ReportAndRestart("Create ");

      IGeometry boundaryGeom = mesh.BuildBoundaryGeometry(alwaysMultiPolygon);
      timer.ReportAndRestart("Build  ");

      BoundaryPolygonWriter(fileName, "-sbnd" + extra, boundaryGeom, timer);
      return boundaryGeom;
    }

    private static void BoundaryPolygonWriter(string fileName, string extra, IGeometry boundaryGeom, Stopwatch timer)
    {
      var    gjws    = new JsonSerializerSettings() {Formatting = Formatting.Indented};
      var    gjw     = new GeoJsonWriter() {SerializerSettings  = gjws};
      string polystr = gjw.Write(boundaryGeom);
      File.WriteAllText(UnitTestHelper.TestDataDir + "test_" + fileName + "-gj" + extra + ".txt", polystr);
      timer.ReportAndRestart("Write  ");

      MultiPolygon boundaryMPoly = boundaryGeom as MultiPolygon;

      // Write to GnuPlot format
      if (boundaryMPoly != null)
      {
        StreamWriter writer = new StreamWriter(UnitTestHelper.TestDataDir + "test_" + fileName + "-gp" + extra + ".txt");
        // First write all shells
        foreach (IGeometry geometry in boundaryMPoly.Geometries)
        {
          Polygon boundaryPoly = geometry as Polygon;
          GnuPlotWrite(writer, boundaryPoly.Shell);
        }

        writer.WriteLine();
        // Then write all holes
        foreach (IGeometry geometry in boundaryMPoly.Geometries)
        {
          Polygon boundaryPoly = geometry as Polygon;
          for (int i = 0; i < boundaryPoly.Holes.Length; i++)
          {
            writer.WriteLine();
            writer.WriteLine("# " + (i + 1));
            GnuPlotWrite(writer, boundaryPoly.Holes[i]);
          }
        }

        writer.Close();
      }
    }

    private static void GnuPlotWrite(StreamWriter writer, ILinearRing ring)
    {
      foreach (Coordinate coord in ring.Coordinates)
      {
        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", coord.X, coord.Y));
      }
      writer.WriteLine("");
    }

    #endregion

  }
}
