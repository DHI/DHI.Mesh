using System;
using System.Collections.Generic;
using System.Net;

namespace DHI.Mesh
{
  public static class MeshExtensions
  {
    /// <summary>
    /// Returns true if the face is a boundary face.
    /// </summary>
    public static bool IsBoundaryFace(this MeshFace face)
    {
      return face.RightElement == null;
    }

    /// <summary>
    /// Returns true if the face is a boundary face.
    /// </summary>
    public static bool IsBoundaryFace(this SMeshFace face)
    {
      return face.RightElement < 0;
    }

    /// <summary>
    /// Returns true if mesh element is a quadrilateral element
    /// </summary>
    public static bool IsQuadrilateral(this MeshElement element)
    {
      // TODO: Should probably check on type?
      int nodesCount = element.Nodes.Count;
      if (nodesCount == 4 || nodesCount == 8)
        return true;
      return false;
    }

    /// <summary>
    /// Returns true if mesh element is a quadrilateral element
    /// </summary>
    public static bool IsQuadrilateral(this SMeshData mesh, int element)
    {
      // TODO: Should probably check on type?
      int nodesCount = mesh.ElementTable[element].Length;
      if (nodesCount == 4 || nodesCount == 8)
        return true;
      return false;
    }

    /// <summary>
    /// Get the other node of a <see cref="MeshFace"/>, when
    /// looking from <paramref name="fromNode"/>.
    /// </summary>
    public static MeshNode ToNode(this MeshFace face, MeshNode fromNode)
    {
      if (ReferenceEquals(face.FromNode, fromNode))
        return face.ToNode;
      return face.FromNode;
    }

    /// <summary>
    /// Get the other element of a <see cref="MeshFace"/>, when
    /// looking from <paramref name="element"/>.
    /// <para>
    /// For boundary faces, null is returned.
    /// </para>
    /// </summary>
    public static MeshElement OtherElement(this MeshFace face, MeshElement element)
    {
      if (ReferenceEquals(face.LeftElement, element))
        return face.RightElement;
      if (ReferenceEquals(face.RightElement, element))
        return face.LeftElement;
      throw new Exception("element is not part of face");
    }

    /// <summary>
    /// Get the other element of a <see cref="MeshFace"/>, when
    /// looking from <paramref name="element"/>.
    /// <para>
    /// For boundary faces, null is returned.
    /// </para>
    /// </summary>
    public static int OtherElement(this SMeshFace face, int element)
    {
      if (face.LeftElement == element)
        return face.RightElement;
      if (face.RightElement == element)
        return face.LeftElement;
      throw new Exception("element is not part of face");
    }

    /// <summary>
    /// Returns true if coordinate (x,y) is inside element
    /// </summary>
    public static bool Includes(this MeshElement element, double x, double y)
    {
      bool isQuad = element.IsQuadrilateral();

      List<MeshNode> elementNodes = element.Nodes;
      if (!isQuad)
      {
        return
          (LeftOf(x, y, elementNodes[0], elementNodes[1]) >= 0 &&
           LeftOf(x, y, elementNodes[1], elementNodes[2]) >= 0 &&
           LeftOf(x, y, elementNodes[2], elementNodes[0]) >= 0);
      }
      return
        (LeftOf(x, y, elementNodes[0], elementNodes[1]) >= 0 &&
         LeftOf(x, y, elementNodes[1], elementNodes[2]) >= 0 &&
         LeftOf(x, y, elementNodes[2], elementNodes[3]) >= 0 &&
         LeftOf(x, y, elementNodes[3], elementNodes[0]) >= 0);

    }

    /// <summary>
    /// Returns true if coordinate (x,y) is inside element
    /// </summary>
    public static bool Includes(this SMeshData mesh, int element, double x, double y)
    {
      bool isQuad = mesh.IsQuadrilateral(element);

      int[] nodes = mesh.ElementTable[element];
      if (!isQuad)
      {
        return
          (LeftOf(x, y, mesh.X[nodes[0]], mesh.Y[nodes[0]], mesh.X[nodes[1]], mesh.Y[nodes[1]] ) >= 0 &&
           LeftOf(x, y, mesh.X[nodes[1]], mesh.Y[nodes[1]], mesh.X[nodes[2]], mesh.Y[nodes[2]] ) >= 0 &&
           LeftOf(x, y, mesh.X[nodes[2]], mesh.Y[nodes[2]], mesh.X[nodes[0]], mesh.Y[nodes[0]] ) >= 0);
      }
      return
        (LeftOf(x, y, mesh.X[nodes[0]], mesh.Y[nodes[0]], mesh.X[nodes[1]], mesh.Y[nodes[1]]) >= 0 &&
         LeftOf(x, y, mesh.X[nodes[1]], mesh.Y[nodes[1]], mesh.X[nodes[2]], mesh.Y[nodes[2]]) >= 0 &&
         LeftOf(x, y, mesh.X[nodes[2]], mesh.Y[nodes[2]], mesh.X[nodes[3]], mesh.Y[nodes[3]]) >= 0 &&
         LeftOf(x, y, mesh.X[nodes[3]], mesh.Y[nodes[3]], mesh.X[nodes[0]], mesh.Y[nodes[0]]) >= 0);

    }

    /// <summary>
    /// Returns true if point is inside triangle limited by the points (t1, t2, t3)
    /// </summary>
    public static bool IsPointInsideTriangle(double x, double y, double t1x, double t1y, double t2x, double t2y, double t3x, double t3y)
    {
      return (LeftOf(x, y, t1x, t1y, t2x, t2y) >= 0 &&
              LeftOf(x, y, t2x, t2y, t3x, t3y) >= 0 &&
              LeftOf(x, y, t3x, t3y, t1x, t1y) >= 0);
    }

    /// <summary>
    /// Returns true if point is inside quadrangle limited by the points (t0, t1, t2, t3)
    /// </summary>
    public static bool IsPointInsideQuadrangle(double x, double y, double t0x, double t0y, double t1x, double t1y, double t2x, double t2y, double t3x, double t3y)
    {
      return (LeftOf(x, y, t0x, t0y, t1x, t1y) >= 0 &&
              LeftOf(x, y, t1x, t1y, t2x, t2y) >= 0 &&
              LeftOf(x, y, t2x, t2y, t3x, t3y) >= 0 &&
              LeftOf(x, y, t3x, t3y, t0x, t0y) >= 0);
    }

    /// <summary>
    /// Returns true if (x,y) coordinate is within the space limited by right line and left line, where left and right line
    /// originate from the center point, i.e. right line is (center)-(right) and left line is (center)-(left).
    /// </summary>
    public static bool IsPointInsideLines(double x, double y, double centerX, double centerY, double rightX, double rightY, double leftX, double leftY)
    {
      return (LeftOf(x, y, centerX, centerY, rightX, rightY) >= 0 &&
              LeftOf(x, y, centerX, centerY, leftX, leftY) <= 0);
    }

    /// <summary>
    /// Returns true if the point (x,y) is left-of the line from point (l1) to (l2)
    /// </summary>
    private static double LeftOf(double x, double y, MeshNode l1, MeshNode l2)
    {
      // Line vector from l1 to l2
      double vx = l2.X - l1.X;
      double vy = (l2.Y - l1.Y);
      // Left perpendicular vector is (-vy, vx)
      // Dot product between Left perpendicular and vector from l1 to (x,y)
      return - (x - l1.X) * vy + (y - l1.Y) * vx;
    }

    /// <summary>
    /// Returns true if the point (x,y) is left-of the line from point (l1) to (l2)
    /// </summary>
    private static double LeftOf(double x, double y, double l1X, double l1Y, double l2X, double l2Y)
    {
      // Line vector from l1 to l2
      double vx = l2X - l1X;
      double vy = l2Y - l1Y;
      // Left perpendicular vector is (-vy, vx)
      // Dot product between Left perpendicular and vector from l1 to (x,y)
      return -(x - l1X) * vy + (y - l1Y) * vx;
    }

    /// <summary>
    /// Calculate interpolation weights for the point (x,y) inside the triangle defined by
    /// the three points (t1,t2,t3).
    /// <para>
    /// The weigts (w1, w2, w3) returned can be used to calculate a value v at the point (x,y)
    /// from values at the three triangle points (v1,v2,v3) as
    /// <code>
    ///    v = w1*v1 + w2*v2 + w3*v3;
    /// </code>
    /// </para>
    /// <para>
    /// if the point (x,y) is not inside the triangle, results are undefined.
    /// </para>
    /// </summary>
    /// <returns>Interpolation weights (w1,w2,w3)</returns>
    public static (double w1, double w2, double w3) InterpolationWeights(double x, double y, double t1x, double t1y, double t2x, double t2y, double t3x, double t3y)
    {
      double denom = ((t2y - t3y) * (t1x - t3x) + (t3x - t2x) * (t1y - t3y));
      double w1    = ((t2y - t3y) * (  x - t3x) + (t3x - t2x) * (  y - t3y)) / denom;
      double w2    = ((t3y - t1y) * (  x - t3x) + (t1x - t3x) * (  y - t3y)) / denom;

      if (w1 < 0) w1 = 0;
      if (w2 < 0) w2 = 0;
      double w12 = w1 + w2;
      if (w12 > 1)
      {
        w1 /= w12;
        w2 /= w12;
      }

      double w3 = 1 - w1 - w2;

      return (w1,w2,w3);
    }

    /// <summary>
    /// Convert from 1-based connectivity structure to zero-based connectivity structure
    /// </summary>
    public static int[][] ToZeroBased(this int[][] connectivity1Based)
    {
      int[][] con0 = new int[connectivity1Based.Length][];
      for (int i = 0; i < connectivity1Based.Length; i++)
      {
        int[] con1Nodes = connectivity1Based[i];
        con0[i]   = new int[con1Nodes.Length];
        for (int j = 0; j < con1Nodes.Length; j++)
        {
          con0[i][j] = con1Nodes[j]-1;
        }
      }
      return con0;
    }

    /// <summary>
    /// Convert from zero-based connectivity structure to 1-based connectivity structure
    /// </summary>
    public static int[][] ToOneBased(this int[][] connectivity0Based)
    {
      int[][] con1 = new int[connectivity0Based.Length][];
      for (int i = 0; i < connectivity0Based.Length; i++)
      {
        int[] con0Nodes = connectivity0Based[i];
        con1[i] = new int[con0Nodes.Length];
        for (int j = 0; j < con0Nodes.Length; j++)
        {
          con1[i][j] = con0Nodes[j] + 1;
        }
      }
      return con1;
    }

    /// <summary>
    /// Convert a <see cref="MeshFile"/> class into a <see cref="MeshData"/> class.
    /// </summary>
    public static MeshData ToMeshData(this MeshFile file)
    {
      return MeshData.CreateMesh(file.Projection, file.NodeIds, file.X, file.Y, file.Z, file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased(), file.ZUnit);
    }

    /// <summary>
    /// Convert a <see cref="MeshFile"/> class into a <see cref="MeshData"/> class.
    /// </summary>
    public static SMeshData ToSMeshData(this MeshFile file)
    {
      return SMeshData.CreateMesh(file.Projection, file.NodeIds, file.X, file.Y, file.Z, file.Code, file.ElementIds, file.ElementType, file.ElementTable.ToZeroBased(), file.ZUnit);
    }


    /// <summary>
    /// Convert a <see cref="MeshFile"/> class into a <see cref="MeshData"/> class.
    /// </summary>
    public static MeshFile ToMeshFile(this MeshData meshData)
    {
      int numberOfNodes = meshData.Nodes.Count;
      int[] nodeId = new int[numberOfNodes];
      double[] x   = new double[numberOfNodes];
      double[] y   = new double[numberOfNodes];
      double[] z   = new double[numberOfNodes];
      int[] code   = new int[numberOfNodes];

      for (int i = 0; i < numberOfNodes; i++)
      {
        MeshNode node = meshData.Nodes[i];
        nodeId[i] = node.Id;
        x[i] = node.X;
        y[i] = node.Y;
        z[i] = node.Z;
        code[i] = node.Code;
      }

      int numberOfElements = meshData.Elements.Count;
      int[] elmtId = new int[numberOfElements];
      int[][] elmtTable = new int[numberOfElements][];
      for (int i = 0; i < numberOfElements; i++)
      {
        MeshElement elmt = meshData.Elements[i];
        elmtId[i] = elmt.Id;
        int[] elmtNodes = new int[elmt.Nodes.Count];
        elmtTable[i] = elmtNodes;
        for (int j = 0; j < elmt.Nodes.Count; j++)
        {
          elmtNodes[j] = elmt.Nodes[j].Index + 1;
        }
      }

      MeshFileBuilder builder = new MeshFileBuilder();
      builder.SetProjection(meshData.Projection);
      builder.SetZUnit(meshData.ZUnit);
      builder.SetNodeIds(nodeId);
      builder.SetNodes(x, y, z, code);
      builder.SetElementIds(elmtId);
      builder.SetElements(elmtTable);

      MeshFile meshFile = builder.CreateMesh();
      return meshFile;
    }


    /// <summary>
    /// Write mesh data to mesh file
    /// </summary>
    public static void Write(this MeshData meshData, string filename)
    {
      meshData.ToMeshFile().Write(filename);
    }

    public static System.Diagnostics.Stopwatch StartTimer()
    {
      System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
      timer.Start();
      return timer;
    }

    public static void Report(this System.Diagnostics.Stopwatch timer, string txt)
    {
      timer.Stop();
      Console.Out.WriteLine("{0}: {1}", txt, timer.Elapsed.TotalSeconds);
      timer.Reset();
    }

    public static void ReportAndRestart(this System.Diagnostics.Stopwatch timer, string txt)
    {
      timer.Stop();
      Console.Out.WriteLine("{0}: {1}", txt, timer.Elapsed.TotalSeconds);
      timer.Reset();
      timer.Start();
    }


  }
}