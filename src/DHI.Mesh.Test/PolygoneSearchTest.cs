using System.Linq;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfsu;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace DHI.Mesh.Test
{
  internal class PolygoneSearchTest
  {

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void GetElementsIndicesWithWeight_PolygonWithinOneMeshElement(bool smesh)
    {
      var dfsufilepath = UnitTestHelper.TestDataDir + @"TestingDfsu1.dfsu";
      var mesh = GetMesh(dfsufilepath, smesh);
      var gf = new GeometryFactory();

      var polygon1 = gf.CreatePolygon(new Coordinate[] {new Coordinate(-0.1, 0.1), new Coordinate(-0.05, 0.1), new Coordinate(-0.1, 0.15), new Coordinate(-0.1, 0.1)});
      var polygon2 = gf.CreatePolygon(new Coordinate[] {new Coordinate(0.05, 0.1), new Coordinate(0.1, 0.1), new Coordinate(0.07, 0.15), new Coordinate(0.05, 0.1)});

      var searcher = MeshFactory.CreateIntersectionCalculator(mesh);
      var search1 = searcher.CalculateWeights(polygon1);
      var search2 = searcher.CalculateWeights(polygon2);

      Assert.AreEqual(1, search1.Count());
      Assert.AreEqual(0, search1[0].ElementIndex);
      Assert.AreEqual(1.0, search1[0].Weight);
      Assert.AreEqual(13, search2[0].ElementIndex);
      Assert.AreEqual(1.0, search2[0].Weight);

      searcher.WeightType = WeightType.Fraction;
      search1 = searcher.CalculateWeights(polygon1);
      search2 = searcher.CalculateWeights(polygon2);
      Assert.AreEqual(1, search1.Count());
      Assert.AreEqual(0, search1[0].ElementIndex);
      Assert.AreEqual(0.06890034, search1[0].Weight, 1e-6);
      Assert.AreEqual(13, search2[0].ElementIndex);
      Assert.AreEqual(0.08729904, search2[0].Weight, 1e-6);

      searcher.WeightType = WeightType.Area;
      search1 = searcher.CalculateWeights(polygon1);
      search2 = searcher.CalculateWeights(polygon2);
      Assert.AreEqual(1, search1.Count());
      Assert.AreEqual(0, search1[0].ElementIndex);
      Assert.AreEqual(polygon1.Area, search1[0].Weight);
      Assert.AreEqual(13, search2[0].ElementIndex);
      Assert.AreEqual(polygon2.Area, search2[0].Weight);
      Assert.AreEqual(polygon2.Area, searcher.IntersectionArea, 1e-6);

    }


    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void GetElementsIndicesWithWeight_PolygonAccrossMeshElement(bool smesh)
    {
      var dfsufilepath = UnitTestHelper.TestDataDir + @"TestingDfsu1.dfsu";
      var mesh = GetMesh(dfsufilepath, smesh);
      var gf = new GeometryFactory();

      var polygon1 = gf.CreatePolygon(new Coordinate[]
      { new Coordinate(0.1, 0.04), new Coordinate(0.2, 0.04), new Coordinate(0.15, 0.0), new Coordinate(0.1, 0.04) });
      var searcher = MeshFactory.CreateIntersectionCalculator(mesh);
      var search1 = searcher.CalculateWeights(polygon1);

      Assert.AreEqual(3, search1.Count);
      Assert.AreEqual(3, search1[0].ElementIndex);
      Assert.AreEqual(11, search1[1].ElementIndex);
      Assert.AreEqual(14, search1[2].ElementIndex);
      Assert.AreEqual(1.0, search1.Sum(w => w.Weight), 1e-6);

      searcher.WeightType = WeightType.Fraction;
      search1 = searcher.CalculateWeights(polygon1);
      Assert.AreEqual(3, search1.Count);
      Assert.AreEqual(0.1353821, search1.Sum(w => w.Weight), 1e-6);

      searcher.WeightType = WeightType.Area;
      search1 = searcher.CalculateWeights(polygon1);
      Assert.AreEqual(3, search1.Count);
      Assert.AreEqual(polygon1.Area, search1.Sum(w => w.Weight), 1e-6);
      Assert.AreEqual(polygon1.Area, searcher.IntersectionArea, 1e-6);

    }


    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void GetElementsIndicesWithWeight_PolygonEnveloppingAMeshElement(bool smesh)
    {
      var dfsufilepath = UnitTestHelper.TestDataDir + @"TestingDfsu1.dfsu";
      var mesh = GetMesh(dfsufilepath, smesh);
      var gf = new GeometryFactory();

      var polygon1 = gf.CreatePolygon(new Coordinate[] 
      { new Coordinate(-0.05, 0.12), new Coordinate(0.2, 0.12), new Coordinate(0.1, -0.14), 
        new Coordinate(-0.05, -0.14), new Coordinate(-0.05, 0.12)});
      var searcher = MeshFactory.CreateIntersectionCalculator(mesh);
      var search1 = searcher.CalculateWeights(polygon1);
      var indices = search1.Select(i => i.ElementIndex).ToList();

      Assert.AreEqual(13, indices.Count);
      Assert.AreEqual(1.0, search1.Sum(w => w.Weight), 1e-6);
      CollectionAssert.Contains(indices, 1);
      CollectionAssert.Contains(indices, 8);
      CollectionAssert.Contains(indices, 13);
      CollectionAssert.Contains(indices, 15);
      CollectionAssert.Contains(indices, 7);
      CollectionAssert.Contains(indices, 5);
      CollectionAssert.DoesNotContain(indices, 4);

      searcher.WeightType = WeightType.Fraction;
      search1 = searcher.CalculateWeights(polygon1);
      Assert.AreEqual(13, search1.Count);
      Assert.AreEqual(3.600211, search1.Sum(w => w.Weight), 1e-6);

      searcher.WeightType = WeightType.Area;
      search1 = searcher.CalculateWeights(polygon1);
      Assert.AreEqual(13, search1.Count);
      Assert.AreEqual(polygon1.Area, search1.Sum(w => w.Weight), 1e-6);
      Assert.AreEqual(polygon1.Area, searcher.IntersectionArea, 1e-6);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void GetElementsIndicesWithWeight_PolygonPartiallyOutside(bool smesh)
    {
      var dfsufilepath = UnitTestHelper.TestDataDir + @"TestingDfsu1.dfsu";
      var mesh = GetMesh(dfsufilepath, smesh);
      var gf = new GeometryFactory();


      var polygon1 = gf.CreatePolygon(new Coordinate[]
      { new Coordinate(0.3, -0.1), new Coordinate(0.6, -0.1), new Coordinate(0.6, -0.15), 
        new Coordinate(0.3, -0.15), new Coordinate(0.3, -0.1)});
      var searcher = MeshFactory.CreateIntersectionCalculator(mesh);
      var search1 = searcher.CalculateWeights(polygon1);
      var indices = search1.Select(i => i.ElementIndex).ToList();

      CollectionAssert.Contains(indices, 2);
      Assert.AreEqual(1, indices.Count);
      Assert.AreEqual(1.0, search1.Sum(w => w.Weight), 1e-6);

      searcher.WeightType = WeightType.Fraction;
      search1 = searcher.CalculateWeights(polygon1);
      Assert.AreEqual(1, search1.Count);
      Assert.AreEqual(0.12871149, search1.Sum(w => w.Weight), 1e-6);

      searcher.WeightType = WeightType.Area;
      search1 = searcher.CalculateWeights(polygon1);
      Assert.AreEqual(1, search1.Count);
      Assert.AreEqual(0.015, polygon1.Area, 1e-6);
      Assert.AreEqual(0.0024292, search1.Sum(w => w.Weight), 1e-6);
      Assert.AreEqual(0.0024292, searcher.IntersectionArea, 1e-6);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void GetElementsIndicesWithWeight_PolygonOutside(bool smesh)
    {
      var dfsufilepath = UnitTestHelper.TestDataDir + @"TestingDfsu1.dfsu";
      var mesh = GetMesh(dfsufilepath, smesh);
      var gf = new GeometryFactory();

      var polygon1 = gf.CreatePolygon(new Coordinate[]
        {new Coordinate(100, 100), new Coordinate(110, 100), new Coordinate(110, 150), new Coordinate(100, 100)});
      var searcher = MeshFactory.CreateIntersectionCalculator(mesh);
      var search1 = searcher.CalculateWeights(polygon1);

      Assert.IsNull(search1);
    }

    private IMeshData GetMesh(string dfsufilepath, bool smesh)
    {
      IMeshData mesh;

      DfsuFile file = DfsFileFactory.DfsuFileOpen(dfsufilepath);
      if (smesh)
      {
        mesh = SMeshData.CreateMesh(file.Projection.WKTString, file.NodeIds, file.X, file.Y, file.Z.ToDoubleArray(),
          file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased());
        return mesh;
      }
      else
      {
        mesh = MeshData.CreateMesh(file.Projection.WKTString, file.NodeIds, file.X, file.Y, file.Z.ToDoubleArray(),
          file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased());
        return mesh;
      }
    }
  }
}
