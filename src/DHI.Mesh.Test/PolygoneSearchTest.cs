using System.IO;
using System.Linq;
using System.Reflection;
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
        public void GetElementsIndicesWithWeight_PolygonWithinOneMeshElement()
        {
            var dfsufilepath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\TestData\TestingDfsu1.dfsu"));
            var mesh = GetMesh(dfsufilepath);
            var gf = new GeometryFactory();

            var polygon1 = gf.CreatePolygon(new Coordinate[] { new Coordinate(-0.1, 0.1), new Coordinate(-0.05, 0.1) , new Coordinate(-0.1, 0.15), new Coordinate(-0.1, 0.1) });
            var polygon2 = gf.CreatePolygon(new Coordinate[] { new Coordinate(0.05, 0.1), new Coordinate(0.1, 0.1), new Coordinate(0.07, 0.15), new Coordinate(0.05, 0.1) });

            var searcher = new MeshSearcher(mesh);
            var search1 = searcher.FindElementsAndWeight(polygon1);
            var search2 = searcher.FindElementsAndWeight(polygon2);


            Assert.AreEqual(1, search1.Count());

            Assert.AreEqual(0, search1[0].Item1.Index);
            Assert.AreEqual(1.0, search1[0].Item2);

            Assert.AreEqual(13, search2[0].Item1.Index);
            Assert.AreEqual(1.0, search2[0].Item2);
        }


        [Test]
        public void GetElementsIndicesWithWeight_PolygonAccrossMeshElement()
        {
            var dfsufilepath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\TestData\TestingDfsu1.dfsu"));
            var mesh = GetMesh(dfsufilepath);
            var gf = new GeometryFactory();

            var polygon1 = gf.CreatePolygon(new Coordinate[] { new Coordinate(0.1, 0.04), new Coordinate(0.2, 0.04), new Coordinate(0.15, 0.0), new Coordinate(0.1, 0.04) });
            var searcher = new MeshSearcher(mesh);
            var search1 = searcher.FindElementsAndWeight(polygon1);
 
            Assert.AreEqual(3, search1[0].Item1.Index);
            Assert.AreEqual(11, search1[1].Item1.Index);
            Assert.AreEqual(14, search1[2].Item1.Index);
            Assert.AreEqual(1.0, search1.Sum( w => w.Item2 ), 1e-6);
        }


        [Test]
        public void GetElementsIndicesWithWeight_PolygonEnveloppingAMeshElement()
        {
            var dfsufilepath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\TestData\TestingDfsu1.dfsu"));
            var mesh = GetMesh(dfsufilepath);
            var gf = new GeometryFactory();

            var polygon1 = gf.CreatePolygon(new Coordinate[] { new Coordinate(-0.05, 0.12), new Coordinate(0.2, 0.12), new Coordinate(0.1, -0.14), new Coordinate(-0.05, -0.14), new Coordinate(-0.05, 0.12) });
            var searcher = new MeshSearcher(mesh);
            var search1 = searcher.FindElementsAndWeight(polygon1);
            var indices = search1.Select(i => i.Item1.Id).ToList();

            CollectionAssert.Contains(indices, 1);
            CollectionAssert.Contains(indices, 8);
            CollectionAssert.Contains(indices, 13);
            CollectionAssert.Contains(indices, 15);
            CollectionAssert.Contains(indices, 7);
            CollectionAssert.Contains(indices, 4);
            CollectionAssert.DoesNotContain(indices, 5);
            Assert.AreEqual(13, indices.Count);
            Assert.AreEqual(1.0, search1.Sum(w => w.Item2), 1e-6);
        }

        [Test]
        public void GetElementsIndicesWithWeight_PolygonPartiallyOutside()
        {
            var dfsufilepath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\TestData\TestingDfsu1.dfsu"));
            var mesh = GetMesh(dfsufilepath);
            var gf = new GeometryFactory();


            var polygon1 = gf.CreatePolygon(new Coordinate[] { new Coordinate(0.3, -0.1), new Coordinate(0.6, -0.1), new Coordinate(0.6, -0.15), new Coordinate(0.3, -0.15), new Coordinate(0.3, -0.1) });
            var searcher = new MeshSearcher(mesh);
            var search1 = searcher.FindElementsAndWeight(polygon1);
            var indices = search1.Select(i => i.Item1.Id).ToList();

            CollectionAssert.Contains(indices, 3);
            Assert.AreEqual(1, indices.Count);
            Assert.AreEqual(1.0, search1.Sum(w => w.Item2), 1e-6);
        }

        [Test]
        public void GetElementsIndicesWithWeight_PolygonOutside()
        {
            var dfsufilepath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\TestData\TestingDfsu1.dfsu"));
            var mesh = GetMesh(dfsufilepath);
            var gf = new GeometryFactory();

            var polygon1 = gf.CreatePolygon(new Coordinate[] { new Coordinate(100, 100), new Coordinate(110, 100), new Coordinate(110, 150), new Coordinate(100, 100) });
            var searcher = new MeshSearcher(mesh);
            var search1 = searcher.FindElementsAndWeight(polygon1);

            Assert.IsNull(search1);
        }

        private MeshData GetMesh(string dfsufilepath)
        {
            MeshData mesh;

            DfsuFile file = DfsFileFactory.DfsuFileOpen(dfsufilepath);
            mesh = MeshData.CreateMesh(file.Projection.WKTString, file.NodeIds, file.X, file.Y, file.Z.ToDoubleArray(), file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased());
            return mesh;
        }
    }
}
