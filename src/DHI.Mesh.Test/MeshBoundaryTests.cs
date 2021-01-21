using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
set style line 1 linecolor rgb '#00ad60' linetype 1 linewidth 2 pointtype 7 pointsize 1.5
set style line 2 linecolor rgb '#dd181f' linetype 1 linewidth 2 pointtype 5 pointsize 1
set size ratio -1
plot "out_odense_rough-gp-bndcode.txt" index 0 with linespoints linestyle 1, '' index 1 with linespoints linestyle 2
  */
    [Test]
    public void MeshBoundaryToFileTest()
    {
      string triMesh = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(triMesh);

      Stopwatch timer = new Stopwatch();
      timer.Start();
      MeshData           mesh       = meshFile.ToMeshData();
      List<MeshBoundary> boundaries = mesh.BuildBoundaryList();
      timer.Stop();
      Console.Out.WriteLine("time:" + timer.Elapsed.TotalSeconds);

      Assert.AreEqual(2, boundaries.Count);
      Assert.AreEqual(1, boundaries[0].Code);
      Assert.AreEqual(2, boundaries[1].Code);

      Assert.AreEqual(2, boundaries[0].Segments.Count);
      Assert.AreEqual(1, boundaries[1].Segments.Count);
      Assert.AreEqual(9, boundaries[1].Segments[0].Count);

      // First node of the first code-1 boundary segment is the last node of the code-2 boundary segment
      Assert.IsTrue(boundaries[0].Segments[0][0].FromNode == boundaries[1].Segments[0].Last().ToNode);
      Assert.IsTrue(boundaries[0].Segments[0].Last().ToNode == boundaries[1].Segments[0][0].FromNode);

      StreamWriter writer = new StreamWriter(UnitTestHelper.TestDataDir + "out_odense_rough-gp-bndcode.txt");
      foreach (MeshBoundary meshBoundary in boundaries)
      {
        writer.WriteLine("# "+meshBoundary.Code);
        foreach (List<MeshFace> segment in meshBoundary.Segments)
        {
          writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", segment[0].FromNode.X, segment[0].FromNode.Y));
          foreach (MeshFace face in segment)
          {
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", face.ToNode.X, face.ToNode.Y));
          }
          writer.WriteLine("");
        }
        writer.WriteLine("");
      }
      writer.Close();

    }

    [Test]
    public void SMeshBoundaryToFileTest()
    {
      string   triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(triMesh);

      Stopwatch timer    = new Stopwatch();
      timer.Start();
      SMeshData           mesh       = meshFile.ToSMeshData();
      List<SMeshBoundary> boundaries = mesh.BuildBoundaryList();
      timer.Stop();
      Console.Out.WriteLine("time:" + timer.Elapsed.TotalSeconds);

      Assert.AreEqual(2, boundaries.Count);
      Assert.AreEqual(1, boundaries[0].Code);
      Assert.AreEqual(2, boundaries[1].Code);

      Assert.AreEqual(2, boundaries[0].Segments.Count);
      Assert.AreEqual(1, boundaries[1].Segments.Count);
      Assert.AreEqual(9, boundaries[1].Segments[0].Count);

      // First node of the first code-1 boundary segment is the last node of the code-2 boundary segment
      Assert.IsTrue(boundaries[0].Segments[0][0].FromNode == boundaries[1].Segments[0].Last().ToNode);
      Assert.IsTrue(boundaries[0].Segments[0].Last().ToNode == boundaries[1].Segments[0][0].FromNode);

      StreamWriter writer = new StreamWriter(UnitTestHelper.TestDataDir + "out_odense_rough-gp-sbndcode.txt");
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
          writer.WriteLine("");
        }
        writer.WriteLine("");
      }
      writer.Close();

    }


    [Test]
    public void MeshBoundaryPolygonTest()
    {
      string   triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(triMesh);

      Stopwatch timer = new Stopwatch();
      timer.Start();
      MeshData  mesh          = meshFile.ToMeshData();
      IGeometry boundaryGeom  = mesh.BuildBoundaryGeometry();
      timer.Stop();
      Console.Out.WriteLine("time:" + timer.Elapsed.TotalSeconds);

      Polygon boundaryPoly = boundaryGeom as Polygon;
      Assert.AreEqual(1, boundaryPoly.Holes.Length);

      // Write Polygon to GeoJson
      var gjws    = new JsonSerializerSettings() { Formatting = Formatting.Indented };
      var    gjw     = new GeoJsonWriter() { SerializerSettings  = gjws };
      string polystr = gjw.Write(boundaryGeom);
      File.WriteAllText(UnitTestHelper.TestDataDir + "out_odense_rough-gj-bnd.txt", polystr);

      // Write Multi-polygon to GeoJson
      IGeometry boundaryGeom2 = mesh.BuildBoundaryGeometry(true);
      string    poly2str      = gjw.Write(boundaryGeom2);
      File.WriteAllText(UnitTestHelper.TestDataDir + "out_odense_rough-gj-bnd2.txt", poly2str);


      // Write to GnuPlot format
      StreamWriter writer = new StreamWriter(UnitTestHelper.TestDataDir + "out_odense_rough-gp-bnd.txt");
      writer.WriteLine("# " + 0);
      GnuPlotWrite(writer, boundaryPoly.Shell);
      for (int i = 0; i < boundaryPoly.Holes.Length; i++)
      {
        writer.WriteLine();
        writer.WriteLine("# " + (i + 1));
        GnuPlotWrite(writer, boundaryPoly.Holes[i]);
      }
      writer.Close();

    }

    [Test]
    public void SMeshBoundaryPolygonTest()
    {
      string   triMesh  = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(triMesh);

      Stopwatch timer = new Stopwatch();
      timer.Start();
      SMeshData mesh          = meshFile.ToSMeshData();
      IGeometry boundaryGeom  = mesh.BuildBoundaryGeometry();
      timer.Stop();
      Console.Out.WriteLine("time:" + timer.Elapsed.TotalSeconds);

      Polygon boundaryPoly = boundaryGeom as Polygon;
      Assert.AreEqual(1, boundaryPoly.Holes.Length);

      // Write Polygon to GeoJson
      var gjws    = new JsonSerializerSettings() { Formatting = Formatting.Indented };
      var    gjw     = new GeoJsonWriter() { SerializerSettings  = gjws };
      string polystr = gjw.Write(boundaryGeom);
      File.WriteAllText(UnitTestHelper.TestDataDir + "out_odense_rough-gj-sbnd.txt", polystr);

      // Write Multi-polygon to GeoJson
      IGeometry boundaryGeom2 = mesh.BuildBoundaryGeometry(true);
      string    poly2str      = gjw.Write(boundaryGeom2);
      File.WriteAllText(UnitTestHelper.TestDataDir + "out_odense_rough-gj-sbnd2.txt", poly2str);

      // Write to GnuPlot format
      StreamWriter writer = new StreamWriter(UnitTestHelper.TestDataDir + "out_odense_rough-gp-sbnd.txt");
      writer.WriteLine("# " + 0);
      GnuPlotWrite(writer, boundaryPoly.Shell);
      for (int i = 0; i < boundaryPoly.Holes.Length; i++)
      {
        writer.WriteLine();
        writer.WriteLine("# " + (i + 1));
        GnuPlotWrite(writer, boundaryPoly.Holes[i]);
      }
      writer.Close();
    }


    private static void GnuPlotWrite(StreamWriter writer, ILinearRing ring)
    {
      foreach (Coordinate coord in ring.Coordinates)
      {
        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", coord.X, coord.Y));
      }
      writer.WriteLine("");
    }
  }
}
