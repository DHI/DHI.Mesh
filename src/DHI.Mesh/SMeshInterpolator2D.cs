using System.Collections.Generic;

namespace DHI.Mesh
{
  public partial class MeshInterpolator2D
  {
    /// <summary> Source mesh </summary>
    private SMeshData _smesh;
    /// <summary> Searcher, for finding elements for a given coordinate </summary>
    private SMeshSearcher _ssearcher;

    /// <summary>
    /// Create interpolator based on <paramref name="sourceMesh"/>
    /// </summary>
    public MeshInterpolator2D(SMeshData sourceMesh)
    {
      _smesh = sourceMesh;
    }

    /// <summary>
    /// Set a target being all elements of the <paramref name="targetMesh"/>
    /// </summary>
    public void SetTarget(SMeshData targetMesh)
    {
      if (targetMesh.ElementXCenter == null)
        targetMesh.CalcElementCenters();

      SetTargetSize(targetMesh.NumberOfElements);
      for (int i = 0; i < targetMesh.NumberOfElements; i++)
      {
        AddTarget(targetMesh.ElementXCenter[i], targetMesh.ElementYCenter[i]);
      }
    }

    /// <summary>
    /// Add a target, by specifying its (x,y) coordinate.
    /// </summary>
    private void AddSTarget(double x, double y)
    {
      if (_targets == null)
        _targets = new List<InterPData>();
      if (_ssearcher == null)
      {
        _ssearcher = new SMeshSearcher(_smesh);
        _ssearcher.SetupElementSearch();
      }

      InterPData interpData = new InterPData();
      // Setting "out-of-bounds" index
      interpData.Element1Index = -1;

      // Find element that includes the (x,y) coordinate
      int element = _ssearcher.FindElement(x, y);

      // Check if element has been found, i.e. includes the (x,y) point
      if (element >= 0)
      {
        bool found = false;
        interpData.Element1Index = element;

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
            interpData.Element2Index = otherElement;
          }
          else
          {
            // No other element - boundary face, use center of face.
            otherElementX = 0.5 * (rightNodeX + leftNodeX);
            otherElementY = 0.5 * (rightNodeY + leftNodeY);
            // Use "itself" as element-2
            interpData.Element2Index = element;
          }


          // Check if point is on the right side of the line between element and other-element
          if (MeshExtensions.IsPointInsideLines(x, y, elementXCenter, elementYCenter, rightNodeX, rightNodeY, otherElementX, otherElementY))
          {
            (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, elementXCenter, elementYCenter, rightNodeX, rightNodeY, otherElementX, otherElementY);
            interpData.NodeIndex = rightNode;
            interpData.Element1Weight = w1;
            interpData.NodeWeight     = w2;
            interpData.Element2Weight = w3;
            found = true;
            break;
          }
          // Check if point is on the left side of the line between element and other-element
          if (MeshExtensions.IsPointInsideLines(x, y, elementXCenter, elementYCenter, otherElementX, otherElementY, leftNodeX, leftNodeY))
          {
            (double w1, double w2, double w3) = MeshExtensions.InterpolationWeights(x, y, elementXCenter, elementYCenter, otherElementX, otherElementY, leftNodeX, leftNodeY);
            interpData.NodeIndex = leftNode;
            interpData.Element1Weight = w1;
            interpData.Element2Weight = w2;
            interpData.NodeWeight = w3;
            found = true;
            break;
          }
        }

        if (!found) // Should never happen, but just in case
        {
          interpData.Element1Weight = 1;
          interpData.Element2Weight = 0;
          interpData.NodeWeight     = 0;
          interpData.Element2Index  = element;
          interpData.NodeIndex      = _smesh.ElementTable[element][0];
        }
      }
      _targets.Add(interpData);
    }

  }
}