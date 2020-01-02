using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
plot "out_odense_rough-bnd.txt" index 0 with linespoints linestyle 1, '' index 1 with linespoints linestyle 2
  */
    [Test]
    public void MeshBoundaryToFileTest()
    {
      string triMesh = UnitTestHelper.TestDataDir + "odense_rough.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(triMesh);
      MeshData mesh     = meshFile.ToMeshData();

      List<MeshBoundary> boundaries = mesh.BuildBoundaryList();

      Assert.AreEqual(2, boundaries.Count);
      Assert.AreEqual(1, boundaries[0].Code);
      Assert.AreEqual(2, boundaries[1].Code);

      Assert.AreEqual(2, boundaries[0].Segments.Count);
      Assert.AreEqual(1, boundaries[1].Segments.Count);
      Assert.AreEqual(9, boundaries[1].Segments[0].Count);

      // First node of the first code-1 boundary segment is the last node of the code-2 boundary segment
      Assert.IsTrue(boundaries[0].Segments[0][0].FromNode == boundaries[1].Segments[0].Last().ToNode);
      Assert.IsTrue(boundaries[0].Segments[0].Last().ToNode == boundaries[1].Segments[0][0].FromNode);

      StreamWriter writer = new StreamWriter(UnitTestHelper.TestDataDir + "out_odense_rough-bnd.txt");
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

  }
}
