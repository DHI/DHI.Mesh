using System;
using System.Collections.Generic;
using System.Diagnostics;
using NetTopologySuite.Geometries;

namespace DHI.Mesh
{
  /// <summary>
  /// Calculating intersection weights for a polygon intersecting a mesh.
  /// <para>
  /// If it is required to calculate weights for more than a few polygons,
  /// do call the <see cref="InitSearcher"/> method. This will initialize
  /// a search tree. Initializing the search tree will take a while, and
  /// then calculation of weights will be a lot faster. If a <see cref="MeshSearcher"/>
  /// is provided in the constructor, calling <see cref="InitSearcher"/> has no effect.
  /// </para>
  /// <para>
  /// If calculating weights for only one (or a few) polygons, it is faster
  /// not to set up the search tree; setting up the search tree takes longer
  /// than checking and calculating weights for all elements a single time.
  /// </para>
  /// </summary>
  public class SMeshIntersectionCalculator : IMeshIntersectionCalculator
  {
    private readonly SMeshData _mesh;
    private SMeshSearcher _searcher;

    /// <summary>
    /// Types of weights to calculate, default is <see cref="Mesh.WeightType.Weight"/>
    /// </summary>
    public WeightType WeightType { get; set; } = WeightType.Weight;

    /// <summary>
    /// The area of the mesh and the polygon intersection, i.e. included in the weights.
    /// <para>
    /// This will equal the polygon area when the polygon is fully covered by the mesh.
    /// </para>
    /// </summary>
    public double IntersectionArea { get; private set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public SMeshIntersectionCalculator(SMeshData mesh)
    {
      _mesh = mesh;
    }

    /// <summary>
    /// Constructor to use if a <see cref="SMeshSearcher"/> is already available.
    /// </summary>
    public SMeshIntersectionCalculator(SMeshData mesh, SMeshSearcher searcher)
    {
      _mesh = mesh;
      _searcher = searcher;
    }


    /// <summary>
    /// Initialize the search data structures. 
    /// </summary>
    public void InitSearcher()
    {
      if (_searcher == null)
      {
        _searcher = new SMeshSearcher(_mesh);
        _searcher.SetupElementSearch();
      }
    }

    /// <summary>
    /// Find elements either contained, containing or intersecting the polygon.
    /// <para>
    /// If polygon is totally contained within one mesh element, then 1 element is returned.
    /// If polygon partially falls outside of the grid, only elements within grid are returned.
    /// </para>
    /// </summary>
    /// <param name="polygon">Polygon or multi-polygon</param>
    public List<ElementWeight> CalculateWeights(Polygon polygon)
    {
      // Find potential elements for polygon point
      Envelope targetEnvelope = polygon.EnvelopeInternal;

      IList<int> potentialElmts;
      if (_searcher != null)
      {
        // Find potential elements for polygon overlap
        potentialElmts = _searcher.QueryElements(targetEnvelope);
      }
      else
        potentialElmts = null;

      return CalculateWeights(polygon, potentialElmts);
    }

    /// <summary>
    /// Find elements either contained, containing or intersecting the polygon.
    /// <para>
    /// This method can be used if only some elements of the mesh is to be included
    /// in the weight calculations.
    /// </para>
    /// <para>
    /// If polygon is totally contained within one mesh element, then 1 element is returned.
    /// If polygon partially falls outside of the grid, only elements within grid are returned.
    /// </para>
    /// <param name="polygon">Polygon or multi-polygon</param>
    /// <param name="elements">List of elements</param>
    /// </summary>
    public List<ElementWeight> CalculateWeights(Geometry polygon, IList<int> elements)
    {
      if (!(polygon is MultiPolygon) && !(polygon is Polygon))
        throw new Exception("Cannot calculate weights for geometry of type: " + polygon.GeometryType);

      Envelope targetEnvelope = polygon.EnvelopeInternal;

      //// It should be faster to use than the polygon directly?
      //PreparedPolygon prepolygon = new PreparedPolygon(polygon);

      List<ElementWeight> result = new List<ElementWeight>();

      // Total intersecting area
      double totalArea = 0;
      // Loop over all potential elements
      int elementsCount = elements?.Count ?? _mesh.NumberOfElements;
      for (int i = 0; i < elementsCount; i++)
      {
        int element = elements != null ? elements[i] : i;

        // Fast-lane check: When there is no overlap even by the envelopes
        if (!targetEnvelope.Intersects(_mesh.ElementEnvelopeInternal(element)))
          continue;

        Polygon elementPolygon = _mesh.ElementToPolygon(element);

        Geometry intersection = elementPolygon.Intersection(polygon);
        if (!intersection.IsEmpty)
        {
          // Target polygon and element polygon has an overlap. 
          // If target  polygon fully contains the element polygon, this is the element area
          // If element polygon fully contains the target  polygon, this is the polygon area
          double intersectingArea = intersection.Area;
          totalArea += intersectingArea;
          if (WeightType == WeightType.Fraction)
            result.Add(new ElementWeight(element, intersectingArea / elementPolygon.Area));
          else
            result.Add(new ElementWeight(element, intersectingArea));
        }
      }

      IntersectionArea = totalArea;

      if (result.Count == 0 || totalArea == 0)
      {
        return null;
      }

      // When Weight-based calculations, weight with the total intersecting area
      if (WeightType == WeightType.Weight)
      {
        for (int i = 0; i < result.Count; i++)
        {
          ElementWeight elmtWeight = result[i];
          elmtWeight.Weight /= totalArea;
          result[i] = elmtWeight;
        }
      }

      return result;
    }

  }
}
