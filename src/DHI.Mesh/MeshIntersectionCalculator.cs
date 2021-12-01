using System;
using System.Collections.Generic;
using System.Diagnostics;
using GeoAPI.Geometries;

namespace DHI.Mesh
{

  /// <summary>
  /// Types of weights
  /// </summary>

  public enum WeightType
  {
    /// <summary>
    /// Weights, all weights summing to 1, the Weight equal to the proportional 
    /// area of the element intersection with the polygon.
    /// </summary>
    Weight,
    /// <summary>
    /// Area of element covered by polygon
    /// </summary>
    Area,
    /// <summary>
    /// Fraction of element covered by polygon
    /// </summary>
    Fraction
  }

  /// <summary>
  /// Represents a weight of an element, being either fractional or area based.
  /// </summary>
  public struct ElementWeight
  {
    public ElementWeight(int elementIndex, double weight)
    {
      ElementIndex = elementIndex;
      Weight = weight;
    }

    public ElementWeight(MeshElement element, double weight)
    {
      ElementIndex = element.Index;
      Weight = weight;
    }

    /// <summary>
    /// Index of element in list of elements
    /// </summary>
    public int ElementIndex { get; set; }

    /// <summary>
    /// Weight value
    /// </summary>
    public double Weight { get; set; }
  }

  /// <summary>
  /// Common interface for the two MeshData classes: 
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
  public interface IMeshIntersectionCalculator
  {
    /// <summary>
    /// Types of weights to calculate, default is <see cref="Mesh.WeightType.Weight"/>
    /// </summary>
    WeightType WeightType { get; set; }

    /// <summary>
    /// The area of the mesh and the polygon intersection, i.e. included in the weights.
    /// <para>
    /// This will equal the polygon area when the polygon is fully covered by the mesh.
    /// </para>
    /// </summary>
    double IntersectionArea { get; }

    /// <summary>
    /// Initialize the search data structures. 
    /// </summary>
    void InitSearcher();

    /// <summary>
    /// Find elements either contained, containing or intersecting the polygon.
    /// <para>
    /// If polygon is totally contained within one mesh element, then 1 element is returned.
    /// If polygon partially falls outside of the grid, only elements within grid are returned.
    /// </para>
    /// </summary>
    /// <param name="polygon">Polygon or multi-polygon</param>
    List<ElementWeight> CalculateWeights(IGeometry polygon);
  }

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
  public class MeshIntersectionCalculator : IMeshIntersectionCalculator
  {
    private readonly MeshData _mesh;
    private MeshSearcher _searcher;

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
    public MeshIntersectionCalculator(MeshData mesh)
    {
      _mesh = mesh;
    }

    /// <summary>
    /// Constructor to use if a <see cref="MeshSearcher"/> is already available.
    /// </summary>
    public MeshIntersectionCalculator(MeshData mesh, MeshSearcher searcher)
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
        _searcher = new MeshSearcher(_mesh);
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
    public List<ElementWeight> CalculateWeights(IGeometry polygon)
    {
      if (!(polygon is IMultiPolygon) && !(polygon is IPolygon))
        throw new Exception("Cannot calculate weights for geometry of type: " + polygon.GeometryType);

      // Find potential elements for polygon point
      Envelope targetEnvelope = polygon.EnvelopeInternal;

#if NTS173
      IList potentialElmts;
#else
      IList<MeshElement> potentialElmts;
#endif
      if (_searcher != null)
      {
        // Find potential elements for polygon overlap
#if NTS173
        potentialSourceElmts = _searcher.Query(targetEnvelope);
#else
        potentialElmts = _searcher.QueryElements(targetEnvelope);
#endif
      }
      else
        potentialElmts = _mesh.Elements;

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
    /// </summary>
#if NTS173
    public IList<ElementWeight> CalculateWeights(IPolygon polygon, IList potentialSourceElmts)
#else
    public List<ElementWeight> CalculateWeights(IGeometry polygon, IList<MeshElement> elements)
#endif
    {
      if (!(polygon is IMultiPolygon) && !(polygon is IPolygon))
        throw new Exception("Cannot calculate weights for geometry of type: " + polygon.GeometryType);

      Envelope targetEnvelope = polygon.EnvelopeInternal;

      //// It should be faster to use than the polygon directly?
      //PreparedPolygon prepolygon = new PreparedPolygon(polygon);

      List<ElementWeight> result = new List<ElementWeight>();

      // Total intersecting area
      double totalArea = 0;
      // Loop over all potential elements
      for (int i = 0; i < elements.Count; i++)
      {
#if NTS173
        MeshElement element = (MeshElement)potentialSourceElmts[i];
#else
        MeshElement element = elements[i];
#endif

        // Fast-lane check: When there is no overlap even by the envelopes
        if (!targetEnvelope.Intersects(element.EnvelopeInternal()))
          continue;

        IPolygon elementPolygon = element.ToPolygon();

        IGeometry intersection = elementPolygon.Intersection(polygon);
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
