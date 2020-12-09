using System;
using System.Collections.Generic;
using System.Threading;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfsu;
using NUnit.Framework;

namespace DHI.Mesh.Test
{
  [TestFixture]

  public class MeshDataTests
  {
    [Test]
    public void ElementIncludesTest()
    {
      MeshElement element = new MeshElement();
      element.Nodes = new List<MeshNode>(3);
      element.Nodes.Add(new MeshNode() { X = 1.1, Y = 1.0 });
      element.Nodes.Add(new MeshNode() { X = 2.2, Y = 1.1 });
      element.Nodes.Add(new MeshNode() { X = 1.6, Y = 2.0 });

      // Corner points are inside
      Assert.True(element.Includes(1.1, 1.0));
      Assert.True(element.Includes(2.2, 1.1));
      Assert.True(element.Includes(1.6, 2.0));

      // Points on face lines are inside
      // Mid point, first face
      Assert.True(element.Includes(1.65,  1.05));
      Assert.True(element.Includes(1.65,  1.05 + 0.0000001));
      Assert.False(element.Includes(1.65, 1.05 - 0.0000001));

      // Mid point, second face
      Assert.True(element.Includes(1.9,  1.55));
      Assert.True(element.Includes(1.9,  1.55 - 0.00000001));
      Assert.False(element.Includes(1.9, 1.55 + 0.00000001));

      // Mid point, third face
      Assert.True(element.Includes(1.35,  1.5));
      Assert.True(element.Includes(1.35,  1.5 - 0.000000001));
      Assert.False(element.Includes(1.35, 1.5 + 0.000000001));

    }

    [Test]
    public void MeshDataOdenseQuadsTest()
    {
      string   quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(quadMesh);
      MeshData mesh     = meshFile.ToMeshData();

      Assert.AreEqual(535,                  mesh.Nodes.Count);
      Assert.AreEqual(4,                    mesh.Nodes[4].Index);
      Assert.AreEqual(5,                    mesh.Nodes[4].Id);
      Assert.AreEqual(212827.81746849261,   mesh.Nodes[4].X);
      Assert.AreEqual(6156804.9152286667,   mesh.Nodes[4].Y);
      Assert.AreEqual(-0.42102556956959569, mesh.Nodes[4].Z);
      Assert.AreEqual(1,                    mesh.Nodes[5].Code);

      Assert.AreEqual(724, mesh.Elements.Count);
      Assert.AreEqual(4,   mesh.Elements[4].Index);
      Assert.AreEqual(5,   mesh.Elements[4].Id);
      Assert.AreEqual(3,   mesh.Elements[4].Nodes.Count);
      Assert.AreEqual(62,  mesh.Elements[4].Nodes[0].Index); // Remember: Index here is zero based, while mesh file is one-based
      Assert.AreEqual(367, mesh.Elements[4].Nodes[1].Index);
      Assert.AreEqual(358, mesh.Elements[4].Nodes[2].Index);

      mesh.BuildNodeElements();
      Assert.AreEqual(4, mesh.Nodes[4].Elements.Count);
      Assert.AreEqual(33, mesh.Nodes[4].Elements[0].Id);
      Assert.AreEqual(36, mesh.Nodes[4].Elements[1].Id);
      Assert.AreEqual(43, mesh.Nodes[4].Elements[2].Id);
      Assert.AreEqual(58, mesh.Nodes[4].Elements[3].Id);

      mesh.BuildFaces(true, true);
      FaceRevert(mesh, mesh.Faces);
      mesh.Faces.Sort(FaceSortComparer);
      Assert.AreEqual(1259, mesh.Faces.Count);
      Assert.AreEqual(1,    mesh.Faces[0].FromNode.Id);
      Assert.AreEqual(42,   mesh.Faces[0].ToNode.Id);
      Assert.AreEqual(1,    mesh.Faces[1].FromNode.Id);
      Assert.AreEqual(251,  mesh.Faces[1].ToNode.Id);
      Assert.AreEqual(1,    mesh.Faces[2].FromNode.Id);
      Assert.AreEqual(309,  mesh.Faces[2].ToNode.Id);
      int ind = mesh.Faces.FindIndex(mf => mf.FromNode.Id == 68);
      Assert.AreEqual(202,  ind);
      Assert.AreEqual(68,   mesh.Faces[ind].FromNode.Id);
      Assert.AreEqual(43,   mesh.Faces[ind].ToNode.Id);
      Assert.AreEqual(1,    mesh.Faces[ind].Code);
      Assert.AreEqual(366,  mesh.Faces[ind].LeftElement.Id);
      Assert.AreEqual(null, mesh.Faces[ind].RightElement);

    }

    [Test]
    public void SMeshDataOdenseQuadsTest()
    {
      string quadMesh = UnitTestHelper.TestDataDir + "odense_rough_quads.mesh";
      MeshFile meshFile = MeshFile.ReadMesh(quadMesh);
      SMeshData mesh = meshFile.ToSMeshData();

      Assert.AreEqual(535,                  mesh.NumberOfNodes);
      Assert.AreEqual(5,                    mesh.NodeIds[4]);
      Assert.AreEqual(212827.81746849261,   mesh.X[4]);
      Assert.AreEqual(6156804.9152286667,   mesh.Y[4]);
      Assert.AreEqual(-0.42102556956959569, mesh.Z[4]);
      Assert.AreEqual(1,                    mesh.Code[5]);

      Assert.AreEqual(724, mesh.NumberOfElements);
      Assert.AreEqual(5, mesh.ElementIds[4]);
      Assert.AreEqual(3, mesh.ElementTable[4].Length);
      Assert.AreEqual(62, mesh.ElementTable[4][0]); // Remember: Index here is zero based, while mesh file is one-based
      Assert.AreEqual(367, mesh.ElementTable[4][1]);
      Assert.AreEqual(358, mesh.ElementTable[4][2]);

      mesh.BuildNodeElements();
      Assert.AreEqual(4,  mesh.NodesElmts[4].Count);
      Assert.AreEqual(33, mesh.NodesElmts[4][0]+1);
      Assert.AreEqual(36, mesh.NodesElmts[4][1]+1);
      Assert.AreEqual(43, mesh.NodesElmts[4][2]+1);
      Assert.AreEqual(58, mesh.NodesElmts[4][3]+1);

      //mesh.BuildFaces(true, true, false);
      //int reverts = FaceRevert(mesh, mesh.Faces);
      //Console.Out.WriteLine("reverts: " + reverts);
      //mesh.Faces.Sort(FaceSortComparer);
      //List<MeshFace> facesOld = mesh.Faces;

      //mesh.BuildFaces(true, true, true);
      //reverts = FaceRevert(mesh, mesh.Faces);
      //Console.Out.WriteLine("reverts: " + reverts);
      //mesh.Faces.Sort(FaceSortComparer);
      //List<MeshFace> facesNew = mesh.Faces;
      //for (int i = 0; i < facesOld.Count; i++)
      //{
      //  bool ok = FaceEquals(facesOld[i], facesNew[i]);
      //  Assert.IsTrue(ok, "Face " + i);
      //}

      //Assert.AreEqual(1259, mesh.Faces.Count);
      //Assert.AreEqual(1, mesh.Faces[0].FromNode.Id);
      //Assert.AreEqual(42, mesh.Faces[0].ToNode.Id);
      //Assert.AreEqual(1, mesh.Faces[1].FromNode.Id);
      //Assert.AreEqual(251, mesh.Faces[1].ToNode.Id);
      //Assert.AreEqual(1, mesh.Faces[2].FromNode.Id);
      //Assert.AreEqual(309, mesh.Faces[2].ToNode.Id);
      //int ind = mesh.Faces.FindIndex(mf => mf.FromNode.Id == 68);
      //Assert.AreEqual(202, ind);
      //Assert.AreEqual(68, mesh.Faces[ind].FromNode.Id);
      //Assert.AreEqual(43, mesh.Faces[ind].ToNode.Id);
      //Assert.AreEqual(1, mesh.Faces[ind].Code);
      //Assert.AreEqual(366, mesh.Faces[ind].LeftElement.Id);
      //Assert.AreEqual(null, mesh.Faces[ind].RightElement);

    }


    private int FaceRevert(MeshData mesh, List<MeshFace> faces)
    {
      int count = 0;
      for (int i = 0; i < faces.Count; i++)
      {
        MeshFace meshFace = faces[i];
        if (meshFace.RightElement != null &&
            meshFace.FromNode.Index > meshFace.ToNode.Index)
        {
          int fn = meshFace.FromNode.Index;
          int tn = meshFace.ToNode.Index;
          meshFace.FromNode = mesh.Nodes[tn];
          meshFace.ToNode   = mesh.Nodes[fn];
          int le = meshFace.LeftElement.Index;
          int re = meshFace.RightElement.Index;
          meshFace.LeftElement  = mesh.Elements[re];
          meshFace.RightElement = mesh.Elements[le];
          count++;
        }
      }

      return count;
    }

    private int FaceSortComparer(MeshFace x, MeshFace y)
    {
      int rc = x.FromNode.Index.CompareTo(y.FromNode.Index);
      if (rc == 0)
        rc = x.ToNode.Index.CompareTo(y.ToNode.Index);
      return rc;
    }
    private bool FaceEquals(MeshFace x, MeshFace y)
    {
      bool ok = true;
      ok &= x.Code == y.Code;
      ok &= x.FromNode == y.FromNode;
      ok &= x.ToNode   == y.ToNode;
      ok &= x.LeftElement   == y.LeftElement;
      ok &= x.RightElement == y.RightElement;
      return ok;
    }
  }
}