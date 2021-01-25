using System.Collections.Generic;
using CMeshData = DHI.Mesh.MeshData;
using CMeshFace = DHI.Mesh.MeshFace;

namespace DHI.Mesh
{
  /// <summary>
  /// Info for a sub mesh.
  /// </summary>
  public class SubMeshInfo
  {
    /// <summary> Id of sub mesh </summary>
    public int SubMeshId;
    /// <summary> Number of elements in sub mesh </summary>
    public int NumberOfElments;
  }

  /// <summary>
  /// This class defines a split of the mesh in connected sub-meshes.
  /// In many cases there is only one connected sub mesh, when the
  /// entire mesh is connected. More than one sub mesh can occur
  /// when a river splits the mesh in separate sub meshes.
  /// <para>
  /// A mesh element can belong to only one sub mesh.
  /// A mesh node can belong to several sub meshes.
  /// </para>
  /// <para>
  /// Sub meshes are given an integer Id that starts from 1 and goes
  /// to the number of sub meshes, stored in <see cref="SubMeshInfo.SubMeshId"/>.
  /// </para>
  /// </summary>
  public class SubMeshes
  {
    /// <summary> Number of sub meshes </summary>
    public int NumberOfSubMeshes { get { return SubMeshInfos.Count; } }
    /// <summary> Info on each sub mesh</summary>
    public List<SubMeshInfo> SubMeshInfos;
    /// <summary> For each element defines which sub-mesh it belongs to</summary>
    public int[] ElmtSubMesh;
  }


  /// <summary>
  /// Extension methods for the <see cref="SubMeshes"/> class.
  /// </summary>
  public static partial class SubMeshesExtensions
  {
    /// <summary>
    /// Find connected sub meshes.
    /// </summary>
    public static SubMeshes FindConnectedSubMeshes(this CMeshData meshData)
    {
      // We need faces information for each element.
      meshData.BuildFaces(true);

      BFSSubMesher subMesher = new BFSSubMesher(meshData);
      SubMeshes subMeshes = subMesher.Process();
      // Sort such that the sub mesh with most element is first in the list
      subMeshes.SubMeshInfos.Sort((smi1, smi2) => -smi1.NumberOfElments.CompareTo(smi2.NumberOfElments));
      return subMeshes;
    }

    /// <summary>
    /// Class performing simple Breadth First Search to
    /// identify connected component sub-meshes.
    /// </summary>
    private class BFSSubMesher
    {
      private CMeshData _meshData;
      public  int[]     ElmtSubMesh;

      /// <summary> stack to reuse between Visits </summary>
      private Stack<int> _stack;

      public BFSSubMesher(CMeshData meshData)
      {
        _meshData = meshData;
      }

      public SubMeshes Process()
      {
        // Create a stack with an initial capacity matching Sqrt(#elmts), which will approximately match for 
        // a square mesh with quads.
        _stack = new Stack<int>((int)System.Math.Sqrt(_meshData.NumberOfElements));

        SubMeshes subMeshes = new SubMeshes();
        subMeshes.SubMeshInfos = new List<SubMeshInfo>();
        int subMeshId = 0;
        ElmtSubMesh = new int[_meshData.NumberOfElements];
        for (int i = 0; i < _meshData.NumberOfElements; i++)
        {
          if (ElmtSubMesh[i] == 0)
          {
            subMeshId++;
            int subMeshElmtCount = BFSVisit(i, subMeshId);
            subMeshes.SubMeshInfos.Add(new SubMeshInfo() { SubMeshId = subMeshId, NumberOfElments = subMeshElmtCount });
          }
        }

        subMeshes.ElmtSubMesh = ElmtSubMesh;
        return subMeshes;
      }

      /// <summary>
      /// Depth First Search visit method
      /// </summary>
      /// <param name="elementIndex">Start element</param>
      /// <param name="subMeshId">Sub mesh ID</param>
      /// <returns>Number of elements in sub mesh</returns>
      private int BFSVisit(int elementIndex, int subMeshId)
      {
        int numElmtsInSubMesh = 0;

        // Element has been discovered
        _stack.Push(elementIndex);
        ElmtSubMesh[elementIndex] = subMeshId;
        numElmtsInSubMesh++;

        while (_stack.Count > 0)
        {
          int elmt = _stack.Pop();

          List<CMeshFace> elmtFaces = _meshData.Elements[elmt].Faces;
          for (int i = 0; i < elmtFaces.Count; i++)
          {
            // Find element on the other side of the face
            CMeshFace elmtFace = elmtFaces[i];

            // Boundary faces never has an element on the other side
            if (elmtFace.IsBoundaryFace())
              continue;

            int otherElmt;
            if (elmtFace.LeftElement.Index == elmt)
              otherElmt = elmtFace.RightElement.Index;
            else
              otherElmt = elmtFace.LeftElement.Index;

            // Check if we have already visited otherElmt
            if (ElmtSubMesh[otherElmt] == 0)
            {
              // Element has been discovered
              _stack.Push(otherElmt);
              ElmtSubMesh[otherElmt] = subMeshId;
              numElmtsInSubMesh++;
            }
          }
        }

        return numElmtsInSubMesh;
      }
    }
  }
}