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
  public class SMeshSearcher
  {
    public double Tolerance { get; set; } = 1e-3;

    private SMeshData _mesh;

#if NTS173
    private STRtree _elementSearchTree;
#else
    private STRtree<int> _elementSearchTree;
#endif

    /// <summary>
    /// Create searcher for provided <paramref name="mesh"/>
    /// </summary>
    public SMeshSearcher(SMeshData mesh)
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
      _elementSearchTree = new STRtree<int>();
#endif
      for (int i = 0; i < _mesh.NumberOfElements; i++)
      {
        int      element   = i;
        int[]    elmtNodes = _mesh.ElementTable[element];
        double   x = _mesh.X[elmtNodes[0]];
        double   y = _mesh.Y[elmtNodes[0]];
        Envelope extent  = new Envelope(x, x, y, y);
        for (int j = 1; j < elmtNodes.Length; j++)
        {
          x = _mesh.X[elmtNodes[j]];
          y = _mesh.Y[elmtNodes[j]];
          extent.ExpandToInclude(x, y);
        }
        _elementSearchTree.Insert(extent, element);
      }
    }

    /// <summary>
    /// Find element containing (x,y) coordinate. Returns -1 if no element found.
    /// <para>
    /// If (x,y) is exactly on the boundary between two elements, one of them will be returned.
    /// If (x,y) is matching exactly a node coordinate, one of the elements including the node will be returned.
    /// </para>
    /// </summary>
    public int FindElement(double x, double y)
    {
      // Find potential elements for (x,y) point
      Envelope targetEnvelope = new Envelope(x, x, y, y);
      targetEnvelope.ExpandBy(Tolerance);

#if NTS173
      IList potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);
#else
      IList<int> potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);
#endif

      // Loop over all potential elements
      for (int i = 0; i < potentialSourceElmts.Count; i++)
      {
#if NTS173
        int element = (int)potentialSourceElmts[i];
#else
        int element = potentialSourceElmts[i];
#endif

        // Check if element includes the (x,y) point
        if (_mesh.Includes(element, x, y))
          return element;
      }

      if (Tolerance <= 0)
        return -1;

      // Try again, now with tolerance
      for (int i = 0; i < potentialSourceElmts.Count; i++)
      {
#if NTS173
        int element = (int)potentialSourceElmts[i];
#else
        int element = potentialSourceElmts[i];
#endif

        // Check if element includes the (x,y) point
        if (_mesh.Includes(element, x, y, Tolerance))
          return element;
      }

      return -1;
    }
  }
}