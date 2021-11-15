using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
#if NTS173
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.Index.Strtree;
#else
using NetTopologySuite.Index.Strtree;
#endif

namespace DHI.Mesh
{
  /// <summary>
  /// Search class for searching for elements (and eventually also nearest nodes)
  /// <para>
  /// 
  /// </para>
  /// </summary>
  public class MeshSearcher
  {
    private MeshData _mesh;

#if NTS173
    private STRtree _elementSearchTree;
#else
    private STRtree<MeshElement> _elementSearchTree;
#endif

    /// <summary>
    /// Create searcher for provided <paramref name="mesh"/>
    /// </summary>
    public MeshSearcher(MeshData mesh)
    {
      _mesh = mesh;
    }

    /// <summary>
    /// Setup for element-search
    /// </summary>
    public void SetupElementSearch()
    {
#if NTS173
      _elementSearchTree = new STRtree();
#else
      _elementSearchTree = new STRtree<MeshElement>();
#endif
      for (int i = 0; i < _mesh.Elements.Count; i++)
      {
        MeshElement element = _mesh.Elements[i];
        double      x       = element.Nodes[0].X;
        double      y       = element.Nodes[0].Y;
        Envelope    extent  = new Envelope(x, x, y, y);
        for (int j = 1; j < element.Nodes.Count; j++)
        {
          extent.ExpandToInclude(element.Nodes[j].X, element.Nodes[j].Y);
        }
        _elementSearchTree.Insert(extent, element);
      }
    }

    /// <summary>
    /// Find element containing (x,y) coordinate. Returns null if no element found.
    /// <para>
    /// If (x,y) is exactly on the boundary between two elements, one of them will be returned.
    /// If (x,y) is matching exacly a node coordinate, one of the elements including the node will be returned.
    /// </para>
    /// </summary>
    public MeshElement FindElement(double x, double y)
    {
      // Find potential elements for (x,y) point
      Envelope targetEnvelope       = new Envelope(x, x, y, y);

#if NTS173
      IList potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);
#else
      IList<MeshElement> potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);
#endif

      // Loop over all potential elements
      for (int i = 0; i < potentialSourceElmts.Count; i++)
      {
#if NTS173
        MeshElement element = (MeshElement)potentialSourceElmts[i];
#else
        MeshElement element = potentialSourceElmts[i];
#endif

        // Check if element includes the (x,y) point
        if (element.Includes(x, y))
          return element;
      }

      return null;
    }

        /// <summary>
        /// Find elements either contained, containing or intersecting polygon. The Weight is equal to the proportinal 
        /// area each element intersects with the polygon.
        /// <para>
        /// If polygon is totally contained within one mesh element, then 1 element is returned with weight 1.
        /// If polygon partially falls outside of the grid, only elements within grid are returned.
        /// </para>
        /// </summary>
        public IList<(MeshElement,double)> FindElementsAndWeight(IPolygon polygon)
        {
            // Find potential elements for (x,y) point
            Envelope targetEnvelope = polygon.Boundary.EnvelopeInternal;
            GeometryFactory gm = new GeometryFactory();
            IList<(MeshElement, double)> result = new List<(MeshElement, double)>();

#if NTS173
      IList potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);
#else
            IList<MeshElement> potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);
#endif

            // Loop over all potential elements
            for (int i = 0; i < potentialSourceElmts.Count; i++)
            {
#if NTS173
        MeshElement element = (MeshElement)potentialSourceElmts[i];
#else
                MeshElement element = potentialSourceElmts[i];
#endif
                Coordinate[] coordinates = element.Nodes
                    .Select(node => new Coordinate(node.X, node.Y, node.Z))
                    .ToArray();
                IPolygon elementPolygon = gm.CreatePolygon(coordinates.ToArray());

                if (elementPolygon.Contains(polygon))
                {
                    result.Add((element, 1.0));
                    continue;
                }

                if (elementPolygon.Intersects(polygon))
                {
                    result.Add((element, elementPolygon.Intersection(polygon).Area));
                    continue;
                }

                if (polygon.Contains(elementPolygon))
                {
                    result.Add((element, elementPolygon.Area));
                }
            }
            if (result.Count == 0)
            {
                return null;
            }

            var area = result.Sum(e => e.Item2);
            for (int i = 0; i < result.Count; i++)
            {
                result[i] = (result[i].Item1, result[i].Item2 / area);
            }
            return result.Count == 0 ? null : result;
        }
    }
}