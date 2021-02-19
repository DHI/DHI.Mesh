using System.Collections.Generic;

namespace DHI.Mesh
{
  public partial class MeshInterpolator2D
  {
    /// <summary>
    /// Create interpolator based on <paramref name="sourceMesh"/>
    /// </summary>
    public MeshInterpolator2D(SMeshData sourceMesh, MeshValueType sourceType)
    {
      _smesh      = sourceMesh;
      _sourceType = sourceType;
    }

    /// <summary> Source mesh </summary>
    private SMeshData _smesh;
    /// <summary> Searcher, for finding elements for a given coordinate </summary>
    private SMeshSearcher _ssearcher;

    /// <summary>
    /// Set a target being all elements of the <paramref name="targetMesh"/>
    /// </summary>
    public void SetTarget(SMeshData targetMesh, MeshValueType targetType)
    {
      if (targetType == MeshValueType.Elements)
      {
        if (targetMesh.ElementXCenter == null)
          targetMesh.CalcElementCenters();

        SetTargetSize(targetMesh.NumberOfElements);
        for (int i = 0; i < targetMesh.NumberOfElements; i++)
        {
          AddTarget(targetMesh.ElementXCenter[i], targetMesh.ElementYCenter[i]);
        }
      }
      else
      {
        SetTargetSize(targetMesh.NumberOfNodes);
        for (int i = 0; i < targetMesh.NumberOfNodes; i++)
        {
          AddTarget(targetMesh.X[i], targetMesh.Y[i]);
        }
      }
    }

    /// <summary>
    /// Add a target, by specifying its (x,y) coordinate.
    /// </summary>
    private void AddSTarget(double x, double y)
    {
      if (_ssearcher == null)
      {
        _ssearcher = new SMeshSearcher(_smesh);
        _ssearcher.SetupElementSearch();
      }

      // Find element that includes the (x,y) coordinate
      int element = _ssearcher.FindElement(x, y);

      // Setup interpolation from node values
      if (NodeValueInterpolation)
      {
        InterPNodeData interp;
        if (element >= 0)
        {
          int[] nodes = _smesh.ElementTable[element];
          if (nodes.Length == 3)
          {
            double x1 = _smesh.X[nodes[0]];
            double x2 = _smesh.X[nodes[1]];
            double x3 = _smesh.X[nodes[2]];
            double y1 = _smesh.Y[nodes[0]];
            double y2 = _smesh.Y[nodes[1]];
            double y3 = _smesh.Y[nodes[2]];

            var weights = InterpTriangle.InterpolationWeights(x, y, x1, y1, x2, y2, x3, y3);
            interp = new InterPNodeData(element, weights.w1, weights.w2, weights.w3);
          }
          else if (nodes.Length == 4)
          {
            double x1 = _smesh.X[nodes[0]];
            double x2 = _smesh.X[nodes[1]];
            double x3 = _smesh.X[nodes[2]];
            double x4 = _smesh.X[nodes[3]];
            double y1 = _smesh.Y[nodes[0]];
            double y2 = _smesh.Y[nodes[1]];
            double y3 = _smesh.Y[nodes[2]];
            double y4 = _smesh.Y[nodes[3]];

            var weights = InterpQuadrangle.InterpolationWeights(x, y, x1, y1, x2, y2, x3, y3, x4, y4);
            interp = new InterPNodeData(element, weights.dx, weights.dy);
          }
          else
          {
            interp = InterPNodeData.Undefined();
          }
        }
        else
        {
          interp = InterPNodeData.Undefined();
        }
        if (_targetsNode == null)
          _targetsNode = new List<InterPNodeData>();
        _targetsNode.Add(interp);
      }

      // Setup interpolation from element+node values
      if (ElmtNodeValueInterpolation)
      {
        InterPElmtNodeData interpElmtNodeData = new InterPElmtNodeData();
        // Setting "out-of-bounds" index
        interpElmtNodeData.Element1Index = -1;

        // Check if element has been found, i.e. includes the (x,y) point
        if (element >= 0)
        {
          bool found = false;
          interpElmtNodeData.Element1Index = element;

          // Check which face the point belongs to, and which "side" of the face
          bool isQuad   = _smesh.IsQuadrilateral(element);
          int  numFaces = isQuad ? 4 : 3;
          for (int j = 0; j < numFaces; j++)
          {
            SMeshFace elementFace = _smesh.Faces[_smesh.ElementsFaces[element][j]];
            // From the element (x,y), looking towards the face, 
            // figure out wich node is right and which is left.
            int rightNode, leftNode;
            if (elementFace.LeftElement == element)
            {
              rightNode = elementFace.FromNode;
              leftNode  = elementFace.ToNode;
            }
            else
            {
              rightNode = elementFace.ToNode;
              leftNode  = elementFace.FromNode;
            }

            double elementXCenter = _smesh.ElementXCenter[element];
            double elementYCenter = _smesh.ElementYCenter[element];
            double rightNodeX     = _smesh.X[rightNode];
            double rightNodeY     = _smesh.Y[rightNode];
            double leftNodeX      = _smesh.X[leftNode];
            double leftNodeY      = _smesh.Y[leftNode];

            // Find also the element on the other side of the face
            double otherElementX, otherElementY;
            int otherElement = elementFace.OtherElement(element);
            if (otherElement >= 0)
            {
              otherElementX = _smesh.ElementXCenter[otherElement];
              otherElementY = _smesh.ElementYCenter[otherElement];
              interpElmtNodeData.Element2Index = otherElement;
            }
            else
            {
              // No other element - boundary face, use center of face.
              otherElementX = 0.5 * (rightNodeX + leftNodeX);
              otherElementY = 0.5 * (rightNodeY + leftNodeY);
              // Use "itself" as element-2
              interpElmtNodeData.Element2Index = element;
            }


            // Check if point is on the right side of the line between element and other-element
            if (MeshExtensions.IsPointInsideLines(x, y, elementXCenter, elementYCenter, rightNodeX, rightNodeY, otherElementX, otherElementY))
            {
              (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, elementXCenter, elementYCenter, rightNodeX, rightNodeY, otherElementX, otherElementY);
              interpElmtNodeData.NodeIndex = rightNode;
              interpElmtNodeData.Element1Weight = w1;
              interpElmtNodeData.NodeWeight     = w2;
              interpElmtNodeData.Element2Weight = w3;
              found = true;
              break;
            }
            // Check if point is on the left side of the line between element and other-element
            if (MeshExtensions.IsPointInsideLines(x, y, elementXCenter, elementYCenter, otherElementX, otherElementY, leftNodeX, leftNodeY))
            {
              (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, elementXCenter, elementYCenter, otherElementX, otherElementY, leftNodeX, leftNodeY);
              interpElmtNodeData.NodeIndex = leftNode;
              interpElmtNodeData.Element1Weight = w1;
              interpElmtNodeData.Element2Weight = w2;
              interpElmtNodeData.NodeWeight = w3;
              found = true;
              break;
            }
          }

          if (!found) // Should never happen, but just in case
          {
            interpElmtNodeData.Element1Weight = 1;
            interpElmtNodeData.Element2Weight = 0;
            interpElmtNodeData.NodeWeight     = 0;
            interpElmtNodeData.Element2Index  = element;
            interpElmtNodeData.NodeIndex      = _smesh.ElementTable[element][0];
          }
        }
        if (_targetsElmtNode == null)
          _targetsElmtNode = new List<InterPElmtNodeData>();
        _targetsElmtNode.Add(interpElmtNodeData);
      }
    }

  }
}