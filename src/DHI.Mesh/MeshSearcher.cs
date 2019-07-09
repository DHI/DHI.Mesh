using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Strtree;

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

    private STRtree<MeshElement> _elementSearchTree;

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
      _elementSearchTree = new STRtree<MeshElement>();
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
    /// If (x,y) is exacly on the boundary between two elements, one of them will be returned.
    /// If (x,y) is matching exacly a node coordinate, one of the elements including the node will be returned.
    /// </para>
    /// </summary>
    public MeshElement FindElement(double x, double y)
    {
      // Find potential elements for (x,y) point
      Envelope targetEnvelope       = new Envelope(x, x, y, y);
      IList<MeshElement> potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);

      // Loop over all potential elements
      for (int i = 0; i < potentialSourceElmts.Count; i++)
      {
        MeshElement element = potentialSourceElmts[i];

        // Check if element includes the (x,y) point
        if (element.Includes(x, y))
          return element;
      }

      return null;
    }
  }
}