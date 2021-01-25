using System.Collections.Generic;
using CMeshData = DHI.Mesh.SMeshData;
using CMeshFace = DHI.Mesh.SMeshFace;

namespace DHI.Mesh
{

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

      BFSSubCMesher subMesher = new BFSSubCMesher(meshData);
      SubMeshes subMeshes = subMesher.Process();
      // Sort such that the sub mesh with most element is first in the list
      subMeshes.SubMeshInfos.Sort((smi1, smi2) => -smi1.NumberOfElments.CompareTo(smi2.NumberOfElments));
      return subMeshes;
    }

    /// <summary>
    /// Class performing simple Breadth First Search to
    /// identify connected component sub-meshes.
    /// </summary>
    private class BFSSubCMesher
    {
      private CMeshData _meshData;
      public  int[]     ElmtSubMesh;

      /// <summary> stack to reuse between Visits </summary>
      private Stack<int> _stack;

      public BFSSubCMesher(CMeshData meshData)
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

          List<int> elmtFaces = _meshData.ElementsFaces[elmt];
          for (int i = 0; i < elmtFaces.Count; i++)
          {
            // Find element on the other side of the face
            CMeshFace elmtFace = _meshData.Faces[elmtFaces[i]];

            // Boundary faces never has an element on the other side
            if (elmtFace.IsBoundaryFace())
              continue;

            int otherElmt;
            if (elmtFace.LeftElement == elmt)
              otherElmt = elmtFace.RightElement;
            else
              otherElmt = elmtFace.LeftElement;

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