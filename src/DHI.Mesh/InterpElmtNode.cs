namespace DHI.Mesh
{
  /// <summary>
  /// Data for interpolating value in the triangle of two elements and a node
  /// </summary>
  public partial class InterpElmtNode
  {

    /// <summary>
    /// Delete/undefined value
    /// </summary>
    public double DelVal { get; set; }

    /// <summary>
    /// Type of value, for interpolation of radians and degrees
    /// </summary>
    public CircularValueTypes CircularType { get; set; } = CircularValueTypes.Normal;


    public struct Weights
    {
      /// <summary> Source element. -1 if not available in source data </summary>
      public int Element1Index;
      /// <summary> Other element, on the other side of the face. For boundary faces, source element value is used. </summary>
      public int Element2Index;
      /// <summary> Node with interpolated value </summary>
      public int NodeIndex;
      /// <summary> Source element value weight </summary>
      public double Element1Weight;
      /// <summary> other element value weight </summary>
      public double Element2Weight;
      /// <summary> node value weight </summary>
      public double NodeWeight;

      /// <summary>
      /// Returns true if the interpolation is defined.
      /// </summary>
      public bool IsDefined
      {
        get { return Element1Index >= 0; }
      }

    }


    /// <summary>
    /// Return a <see cref="Weights"/> structure with undefined weights.
    /// </summary>
    public static Weights Undefined()
    {
      return new Weights() { Element1Index = -1 };
    }

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

    public static Weights InterpolationWeights(double x, double y, MeshElement element)
    {
      Weights weights = new Weights();
      // Setting "out-of-bounds" index
      weights.Element1Index = -1;

      bool found = false;
      weights.Element1Index = element.Index;

      // Check which face the point belongs to, and which "side" of the face
      bool isQuad   = element.IsQuadrilateral();
      int  numFaces = isQuad ? 4 : 3;
      for (int j = 0; j < numFaces; j++)
      {
        MeshFace elementFace = element.Faces[j];
        // From the element (x,y), looking towards the face, 
        // figure out wich node is right and which is left.
        MeshNode rightNode, leftNode;
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

        double elementXCenter = element.XCenter;
        double elementYCenter = element.YCenter;
        double rightNodeX     = rightNode.X;
        double rightNodeY     = rightNode.Y;
        double leftNodeX      = leftNode.X;
        double leftNodeY      = leftNode.Y;

        // Find also the element on the other side of the face
        double      otherElementX, otherElementY;
        MeshElement otherElement = elementFace.OtherElement(element);
        if (otherElement != null)
        {
          otherElementX = otherElement.XCenter;
          otherElementY = otherElement.YCenter;
          weights.Element2Index = otherElement.Index;
        }
        else
        {
          // No other element - boundary face, use center of face.
          otherElementX = 0.5 * (rightNodeX + leftNodeX);
          otherElementY = 0.5 * (rightNodeY + leftNodeY);
          // Use "itself" as element-2
          weights.Element2Index = element.Index;
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
          weights.NodeIndex      = rightNode.Index;
          weights.Element1Weight = w1;
          weights.NodeWeight     = w2;
          weights.Element2Weight = w3;
          found                         = true;
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
          weights.NodeIndex      = leftNode.Index;
          weights.Element1Weight = w1;
          weights.Element2Weight = w2;
          weights.NodeWeight     = w3;
          found                         = true;
          break;
        }
      }

      if (!found) // Should never happen, but just in case
      {
        weights.Element1Weight = 1;
        weights.Element2Weight = 0;
        weights.NodeWeight     = 0;
        weights.Element2Index  = element.Index;
        weights.NodeIndex      = element.Nodes[0].Index;
      }

      return weights;
    }

    /// <summary>
    /// Returns interpolated value based on the <paramref name="weights"/>
    /// </summary>
    /// <param name="weights">Triangular interpolation weights</param>
    /// <param name="elmtValues">Values at element centers</param>
    /// <param name="nodeValues">Values at nodes</param>
    /// <returns>Interpolated value</returns>
    public double GetValue(Weights weights, float[] elmtValues, float[] nodeValues)
    {

      // Do interpolation inside (element-element-node) triangle, 
      // disregarding any delete values.
      double sourceElementValue = elmtValues[weights.Element1Index];
      if (sourceElementValue != DelVal)
      {
        double value  = sourceElementValue * weights.Element1Weight;
        double weight = weights.Element1Weight;

        {
          double otherElmentValue = elmtValues[weights.Element2Index];
          if (otherElmentValue != DelVal)
          {
            CircularValueHandler.ToReference(CircularType, ref otherElmentValue, sourceElementValue);
            value  += otherElmentValue * weights.Element2Weight;
            weight += weights.Element2Weight;
          }
        }

        {
          double nodeValue = nodeValues[weights.NodeIndex];
          if (nodeValue != DelVal)
          {
            CircularValueHandler.ToReference(CircularType, ref nodeValue, sourceElementValue);
            value  += nodeValue * weights.NodeWeight;
            weight += weights.NodeWeight;
          }
        }

        value /= weight;
        CircularValueHandler.ToCircular(CircularType, ref value);
        return value;
      }

      return DelVal;

    }

  }
}