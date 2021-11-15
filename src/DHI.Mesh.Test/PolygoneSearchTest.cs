using System;
using System.Collections.Generic;
using System.IO;
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
        public void GetElementsIndicesWithWeight_PolygonWithinOneMeshElement()
        {
            var dfsufilepath = Path.GetFullPath(Path.Combine(@"..\..\..\..\..\", @"TestData\TestingDfsu1.dfsu"));
            var mesh = GetMesh(dfsufilepath);
            var gf = new GeometryFactory();

            var coordinates = new Coordinate();

            var polygon1 = gf.CreatePolygon(new Coordinate[] { new Coordinate(-0.1, 0.1), new Coordinate(-0.05, 0.1) , new Coordinate(-0.1, 0.15) });
            var polygon2 = gf.CreatePolygon(new Coordinate[] { new Coordinate(0.05, 0.1), new Coordinate(0.1, 0.1), new Coordinate(0.07, 0.15) });

            var searcher = new MeshSearcher(mesh);

            var search1 = searcher.FindElementsAndWeight(polygon1);
            var search2 = searcher.FindElementsAndWeight(polygon2);


            Assert.Equals(1, search1.Count());

            Assert.Equals(0, search1[0].Item1.Id);
            Assert.Equals(1.0, search1[0].Item2);

            Assert.Equals(13, search2[0].Item1.Id);
            Assert.Equals(1.0, search2[0].Item2);
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
