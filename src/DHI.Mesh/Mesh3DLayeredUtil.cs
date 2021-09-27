using System;
using System.Collections.Generic;

namespace DHI.Mesh
{
  public static class Mesh3DLayeredUtil
  {

    /// <summary>
    /// Find element indices (zero-based) of the elements being the upper-most element
    /// in its column.
    /// <para>
    /// Each column is identified by matching node id numbers. For 3D elements the
    /// last half of the node numbers of the bottom element must match the first half
    /// of the node numbers in the top element. For 2D vertical elements the order of 
    /// the node numbers in the bottom element (last half number of nodes) are reversed 
    /// compared to those in the top element (first half number of nodes).
    /// </para>
    /// </summary>
    /// <returns>A list of element indices of top layer elements</returns>
    /// <remarks>
    /// To find the number of elements in each column, assuming the result
    /// is stored in res:
    /// <para>
    /// For the first column it is res[0]+1.
    /// </para>
    /// <para>
    /// For the i'th column, it is res[i]-res[i-1].
    /// </para>
    /// </remarks>
    public static IList<int> FindTopLayerElements(this SMeshData mesh)
    {
      MeshType meshType = MeshType.Mesh3DSigma;
      if (!(meshType == MeshType.Mesh3DSigma ||
            meshType == MeshType.Mesh3DSigmaZ ||
            meshType == MeshType.MeshVerticalProfileSigma ||
            meshType == MeshType.MeshVerticalProfileSigmaZ))
        throw new InvalidOperationException("Can not extract top layer elements of a 2D mesh");

      return FindTopLayerElements(mesh.ElementTable);
    }


    /// <summary>
    /// Find element indices (zero based) of the elements being the upper-most element
    /// in its column.
    /// <para>
    /// Each column is identified by matching node id numbers. For 3D elements the
    /// last half of the node numbers of the bottom element must match the first half
    /// of the node numbers in the top element. For 2D vertical elements the order of 
    /// the node numbers in the bottom element (last half number of nodes) are reversed 
    /// compared to those in the top element (first half number of nodes).
    /// </para>
    /// </summary>
    /// <returns>A list of element indices of top layer elements</returns>
    /// <remarks>
    /// To find the number of elements in each column, assuming the result
    /// is stored in res:
    /// <para>
    /// For the first column it is res[0]+1.
    /// </para>
    /// <para>
    /// For the i'th column, it is res[i]-res[i-1].
    /// </para>
    /// </remarks>
    public static IList<int> FindTopLayerElements(int[][] elementTable)
    {
      List<int> topLayerElments = new List<int>();

      // Find top layer elements by matching the number numers of the last half of elmt i 
      // with the first half of element i+1.
      // Elements always start from the bottom, and the element of one columne are following
      // each other in the element table.
      for (int i = 0; i < elementTable.Length - 1; i++)
      {
        int[] elmt1 = elementTable[i];
        int[] elmt2 = elementTable[i + 1];

        if (elmt1.Length != elmt2.Length)
        {
          // elements with different number of nodes can not be on top of each other, 
          // so elmt2 must be another column, and elmt1 must be a top element
          topLayerElments.Add(i);
          continue;
        }

        if (elmt1.Length % 2 != 0)
        {
          throw new Exception("In a layered mesh, each element must have an even number of elements (element index " + i + ")");
        }

        // Number of nodes in a 2D element
        int elmt2DSize = elmt1.Length / 2;

        for (int j = 0; j < elmt2DSize; j++)
        {
          if (elmt2DSize > 2)
          {
            if (elmt1[j + elmt2DSize] != elmt2[j])
            {
              // At least one node number did not match
              // so elmt2 must be another column, and elmt1 must be a top element
              topLayerElments.Add(i);
              break;
            }
          }
          else
          {
            // for 2D vertical profiles the nodes in the element on the
            // top is in reverse order of those in the bottom.
            if (elmt1[j + elmt2DSize] != elmt2[(elmt2DSize - 1) - j])
            {
              // At least one node number did not match
              // so elmt2 must be another column, and elmt1 must be a top element
              topLayerElments.Add(i);
              break;
            }
          }
        }
      }

      // The last element will always be a top layer element
      topLayerElments.Add(elementTable.Length - 1);

      return (topLayerElments);
    }

    /// <summary>
    /// Find the maximum number of layers, based on the indices of
    /// all top layer elements.
    /// </summary>
    /// <remarks>
    /// Assuming that the <paramref name="topLayerElements"/> comes
    /// ordered.
    /// </remarks>
    public static int FindMaxNumberOfLayers(IList<int> topLayerElements)
    {
      // the first column has top-element-index + 1 layers
      int maxLayers = topLayerElements[0] + 1;
      for (int i = 1; i < topLayerElements.Count; i++)
      {
        int layers = topLayerElements[i] - topLayerElements[i - 1];
        if (layers > maxLayers)
          maxLayers = layers;
      }
      return (maxLayers);
    }

    /// <summary>
    /// Find the minimum number of layers, based on the indices of
    /// all top layer elements.
    /// </summary>
    /// <remarks>
    /// Assuming that the <paramref name="topLayerElements"/> comes
    /// ordered.
    /// </remarks>
    public static int FindMinNumberOfLayers(IList<int> topLayerElements)
    {
      // the first column has top-element-index + 1 layers
      int minLayers = topLayerElements[0] + 1;
      for (int i = 1; i < topLayerElements.Count; i++)
      {
        int layers = topLayerElements[i] - topLayerElements[i - 1];
        if (layers < minLayers)
          minLayers = layers;
      }
      return (minLayers);
    }


    /// <summary>
    /// Extract a 2D mesh from a 3D layered mesh.
    /// </summary>
    public static SMeshData Extract2DMesh(SMeshData mesh)
    {

      MeshType meshType = MeshType.Mesh3DSigma;
      // Check that mesh file is a 3D mesh.
      switch (meshType)
      {
        case MeshType.Mesh2D:
        case MeshType.MeshVerticalColumn:
        case MeshType.MeshVerticalProfileSigma:
        case MeshType.MeshVerticalProfileSigmaZ:
          throw new InvalidOperationException("Input mesh is not a 3D mesh");
      }

      double[] xv = mesh.X;
      double[] yv = mesh.Y;
      double[] zv = mesh.Z;
      int[]    cv = mesh.Code;

      // --------------------------------------------------
      // Extract 2D mesh from 3D mesh

      // List of new 2D nodes
      int node2DCount = 0;
      List<double> xv2 = new List<double>();
      List<double> yv2 = new List<double>();
      List<double> zv2 = new List<double>();
      List<int> cv2 = new List<int>();

      // Renumbering array, from 3D node numbers to 2D node numbers
      // i.e. if a 3D element refers to node number k, the 2D element node number is renumber[k]
      int[] renumber = new int[mesh.NumberOfNodes];

      // Coordinates of last created node
      double xr2 = -1e-10;
      double yr2 = -1e-10;

      // Create 2D nodes, by skipping nodes with equal x,y coordinates
      for (int i = 0; i < mesh.NumberOfNodes; i++)
      {
        // If 3D x,y coordinates are equal to the last created 2D node,
        // map this node to the last created 2D node, otherwise
        // create new 2D node and map to that one
        if (xv[i] != xr2 || yv[i] != yr2)
        {
          // Create new node
          node2DCount++;
          xr2 = xv[i];
          yr2 = yv[i];
          double zr2 = zv[i];
          int    cr2 = cv[i];
          xv2.Add(xr2);
          yv2.Add(yr2);
          zv2.Add(zr2);
          cv2.Add(cr2);
        }
        // Map this 3D node to the last created 2D node.
        renumber[i] = node2DCount;
      }

      // Find indices of top layer elements
      IList<int> topLayer = mesh.FindTopLayerElements();

      // Create element table for 2D mesh
      int[][] elmttable2 = new int[topLayer.Count][];
      for (int i = 0; i < topLayer.Count; i++)
      {
        // 3D element nodes
        int[] elmt3 = mesh.ElementTable[topLayer[i]];
        // 2D element nodes, only half as big, so copy over the first half
        int[] elmt2 = new int[elmt3.Length / 2];
        for (int j = 0; j < elmt2.Length; j++)
        {
          elmt2[j] = renumber[elmt3[j]];
        }
        elmttable2[i] = elmt2;
      }


      // --------------------------------------------------
      // Create 2D mesh
      SMeshData mesh2D = 
        SMeshData.CreateMesh(
          mesh.Projection,
          null,
          xv2.ToArray(), yv2.ToArray(), zv2.ToArray(), cv2.ToArray(),
          null,
          null, 
          elmttable2
          );
      mesh2D.ZUnit = mesh.ZUnit;

      return mesh2D;
    }
  } }
