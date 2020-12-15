using System.Collections.Generic;
using System.Diagnostics;

namespace DHI.Mesh
{
  /// <summary>
  /// A face is a boundary between two elements. The face has a direction, defined from <see cref="FromNode"/> to <see cref="ToNode"/>,
  /// and in that direction it has a <see cref="LeftElement"/> and a <see cref="RightElement"/>
  /// <para>
  /// For boundary faces, the <see cref="RightElement"/> does not exist and is null.
  /// </para>
  /// </summary>
  [DebuggerDisplay("MeshFace: {FromNode}-{ToNode} ({LeftElement},{RightElement})")]
  public class SMeshFace
  {
    public SMeshFace(int fromNode, int toNode)
    {
      FromNode     = fromNode;
      ToNode       = toNode;
      LeftElement  = -1;
      RightElement = -1;
      Code         = 0;
    }
    /// <summary> From node - start point of face </summary>
    public int FromNode;
    /// <summary> to node - end point of face </summary>
    public int ToNode;
    /// <summary> Left element </summary>
    public int LeftElement;
    /// <summary> Right element. For boundary faces this is not defined (-1). </summary>
    public int RightElement;
    public int Code;


    /// <summary>
    /// Evaluate its boundary code based on the code of the nodes.
    /// </summary>
    internal void SetBoundaryCode(SMeshData mesh, List<string> errors = null)
    {
      // If the RightElement exists, this face is an internal face
      if (RightElement >= 0)
      {
        return;
      }

      // RightElement does not exist, so it is a boundary face.
      int fromCode = mesh.Code[FromNode];
      int toCode   = mesh.Code[ToNode];

      // True if "invalid" boundary face, then set it as land face.
      bool landFace = false;

      if (fromCode == 0)
      {
        landFace = true;
        errors?.Add(string.Format(
          "Invalid mesh: Boundary face, from node {0} to node {1} is missing a boundary code on node {0}. " +
          "Hint: Modify boundary code for node {0}",
          FromNode + 1, ToNode + 1));
      }

      if (toCode == 0)
      {
        landFace = true;
        errors?.Add(string.Format(
          "Invalid mesh: Boundary face, from node {0} to node {1} is missing a boundary code on node {1}. " +
          "Hint: Modify boundary code for node {1}",
          FromNode + 1, ToNode + 1));
      }

      int faceCode;

      // Find face code:
      // 1) In case any of the nodes is a land node (code value 1) then the
      //    boundary face is a land face, given boundary code value 1.
      // 2) For boundary faces (if both fromNode and toNode have code values larger than 1), 
      //    the face code is the boundary code value of toNode.
      if (fromCode == 1 || toCode == 1)
        faceCode = 1;
      else
        faceCode = toCode;

      if (!landFace)
        Code = faceCode;
      else
        Code = 1;
    }

  }
}