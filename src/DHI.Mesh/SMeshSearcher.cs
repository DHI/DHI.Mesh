﻿using NetTopologySuite.Geometries;
using System.Collections.Generic;
using SearchTreeType = NetTopologySuite.Index.Quadtree.Quadtree<int>;

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
    /// <summary>
    /// Tolerance, when searching for elements 
    /// </summary>
    public double Tolerance { get; set; } = 1e-3;

    private SMeshData _mesh;
    private SearchTreeType _elementSearchTree;

    /// <summary>
    /// Create searcher for provided <paramref name="mesh"/>
    /// </summary>
    public SMeshSearcher(SMeshData mesh)
    {
      _mesh = mesh;
    }

    /// <summary>
    /// Create searcher for provided <paramref name="mbase"/>
    /// </summary>
    public SMeshSearcher(ISMeshData mbase)
    {
      var mesh = new SMeshData(mbase.Projection, mbase.NodeIds, mbase.X, mbase.Y, mbase.Z, mbase.Code, mbase.ElementIds, mbase.ElementType, mbase.ElementTable, mbase.ZUnit);
      mesh.BuildDerivedData();
      _mesh = mesh;
    }


    /// <summary>
    /// Setup for element-search
    /// </summary>
    public void SetupElementSearch()
    {
      _elementSearchTree = new SearchTreeType();

      for (int i = 0; i < _mesh.NumberOfElements; i++)
      {
        int element = i;
        Envelope extent  = _mesh.ElementEnvelopeInternal(element); ;
        _elementSearchTree.Insert(extent, element);
      }
      // When using STRtree, call this method here
      //_elementSearchTree.Build();
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

      IList<int> potentialSourceElmts = _elementSearchTree.Query(targetEnvelope);

      // Loop over all potential elements
      for (int i = 0; i < potentialSourceElmts.Count; i++)
      {
        int element = potentialSourceElmts[i];

        // Check if element includes the (x,y) point
        if (_mesh.Includes(element, x, y))
          return element;
      }

      if (Tolerance <= 0)
        return -1;

      // Try again, now with tolerance
      for (int i = 0; i < potentialSourceElmts.Count; i++)
      {
        int element = potentialSourceElmts[i];

        // Check if element includes the (x,y) point
        if (_mesh.Includes(element, x, y, Tolerance))
          return element;
      }

      return -1;
    }


    /// <summary>
    /// Queries and returns elements which are (fully or partly) inside or close to the search envelope.
    /// <para>
    /// Note: There is no guarantee that the element lies inside the search envelope;
    /// this is a fast first filtering of elements, providing all "nearby" elements.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The elements returned are elements whose envelope <b>may</b> intersect the search Envelope.
    /// Elements with non-intersecting envelopes may be returned as well.
    /// In most situations there will be many elements which are not returned,
    /// thus providing improved performance over a simple linear search over all elements.
    /// </remarks>
    /// <param name="envelope">The search envelope, the desired query area.</param>
    /// <returns>A List of elements which may intersect the search envelope</returns>
    public IList<int> QueryElements(Envelope envelope)
    {
      return _elementSearchTree.Query(envelope);
    }

    /// <summary>
    /// Find elements either contained, containing or intersecting polygon.
    /// <para>
    /// If no elements are found, an empty list is returned.
    /// </para>
    /// </summary>
    public IList<int> FindElements(Polygon polygon)
    {
      if (_elementSearchTree == null)
      {
        SetupElementSearch();
      }

      Envelope targetEnvelope = polygon.EnvelopeInternal;
      
      IList<int> potentialElmts = _elementSearchTree.Query(targetEnvelope);

      List<int> result = new List<int>();

      // Loop over all potential elements
      for (int i = 0; i < potentialElmts.Count; i++)
      {
        int element = potentialElmts[i];

        // Fast-lane check: When there is no overlap even by the envelopes
        if (!targetEnvelope.Intersects(_mesh.ElementEnvelopeInternal(element)))
          continue;

        // More detailed check for actual overlap
        Polygon elementPolygon = _mesh.ElementToPolygon(element);
        if (elementPolygon.Intersects(polygon))
        {
          result.Add(element);
        }
      }

      return result;
    }

  }
}
