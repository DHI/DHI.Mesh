namespace DHI.Mesh
{
  public partial class InterpElmtNode
  {

    /// <summary>
    /// Calculate interpolation weights for the point (x,y) inside the <see cref="element"/>"/>
    /// <para>
    /// The weights returned can be used to calculate a value v at the point (x,y) using the
    /// <code>GetValue</code> methods
    /// </para>
    /// <para>
    /// if the point (x,y) is not inside the element, results are undefined.
    /// </para>
    /// </summary>
    /// <returns>Interpolation weights</returns>

    public static Weights InterpolationWeights(double x, double y, int element, SMeshData smesh)
    {
      Weights weights = new Weights();
      // Setting "out-of-bounds" index
      weights.Element1Index = -1;

      bool found = false;
      weights.Element1Index = element;

      // Check which face the point belongs to, and which "side" of the face
      bool isQuad = smesh.IsQuadrilateral(element);
      int numFaces = isQuad ? 4 : 3;
      for (int j = 0; j < numFaces; j++)
      {
        SMeshFace elementFace = smesh.Faces[smesh.ElementsFaces[element][j]];
        // From the element (x,y), looking towards the face, 
        // figure out wich node is right and which is left.
        int rightNode, leftNode;
        if (elementFace.LeftElement == element)
        {
          rightNode = elementFace.FromNode;
          leftNode = elementFace.ToNode;
        }
        else
        {
          rightNode = elementFace.ToNode;
          leftNode = elementFace.FromNode;
        }

        double elementXCenter = smesh.ElementXCenter[element];
        double elementYCenter = smesh.ElementYCenter[element];
        double rightNodeX     = smesh.X[rightNode];
        double rightNodeY     = smesh.Y[rightNode];
        double leftNodeX      = smesh.X[leftNode];
        double leftNodeY      = smesh.Y[leftNode];

        // Find also the element on the other side of the face
        double otherElementX, otherElementY;
        int otherElement = elementFace.OtherElement(element);
        if (otherElement >= 0)
        {
          otherElementX = smesh.ElementXCenter[otherElement];
          otherElementY = smesh.ElementYCenter[otherElement];
          weights.Element2Index = otherElement;
        }
        else
        {
          // No other element - boundary face, use center of face.
          otherElementX = 0.5 * (rightNodeX + leftNodeX);
          otherElementY = 0.5 * (rightNodeY + leftNodeY);
          // Use "itself" as element-2
          weights.Element2Index = element;
        }


        // Check if point is on the right side of the line between element and other-element
        if (MeshExtensions.IsPointInsideLines(x, y, 
                                              elementXCenter, elementYCenter, 
                                              rightNodeX, rightNodeY, 
                                              otherElementX, otherElementY))
        {
          (double w1, double w2, double w3) = 
            MeshExtensions.InterpolationWeights(
              x, y, elementXCenter, elementYCenter,
              rightNodeX, rightNodeY, otherElementX, otherElementY);
          weights.NodeIndex      = rightNode;
          weights.Element1Weight = w1;
          weights.NodeWeight     = w2;
          weights.Element2Weight = w3;
          found = true;
          break;
        }

        // Check if point is on the left side of the line between element and other-element
        if (MeshExtensions.IsPointInsideLines(x, y, 
                                              elementXCenter, elementYCenter, 
                                              otherElementX, otherElementY,
                                              leftNodeX, leftNodeY))
        {
          (double w1, double w2, double w3) = 
            MeshExtensions.InterpolationWeights(
              x, y, elementXCenter, elementYCenter,
              otherElementX, otherElementY, leftNodeX, leftNodeY);
          weights.NodeIndex = leftNode;
          weights.Element1Weight = w1;
          weights.Element2Weight = w2;
          weights.NodeWeight     = w3;
          found = true;
          break;
        }
      }

      if (!found) // Should never happen, but just in case
      {
        weights.Element1Weight = 1;
        weights.Element2Weight = 0;
        weights.NodeWeight     = 0;
        weights.Element2Index  = element;
        weights.NodeIndex      = smesh.ElementTable[element][0];
      }

      return weights;
    }

  }
}
