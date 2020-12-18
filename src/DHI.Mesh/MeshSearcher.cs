using System.Collections.Generic;
using GeoAPI.Geometries;
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
  }
}