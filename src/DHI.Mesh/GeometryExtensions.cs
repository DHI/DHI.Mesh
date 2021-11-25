using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace DHI.Mesh
{
  /// <summary>
  /// Extension methods for converting between mesh structures and geometry structures.
  /// </summary>
  public static class GeometryExtensions
  {
    /// <summary>
    /// Default Geometry Factory
    /// </summary>
    public static GeometryFactory GeomFactory = new GeometryFactory();

    /// <summary>
    /// Create a polygon from a mesh element
    /// </summary>
    public static IPolygon ToPolygon(this MeshElement element)
    {
      return element.ToPolygon(GeomFactory);
    }

    /// <summary>
    /// Create a polygon from a mesh element.
    /// </summary>
    public static IPolygon ToPolygon(this MeshElement element, GeometryFactory geomFactory)
    {
      List<Coordinate> coordinates = new List<Coordinate>(element.Nodes.Count);
      
      MeshNode node;
      for (int i = 0; i < element.Nodes.Count; i++)
      {
        node = element.Nodes[i];
        coordinates.Add(new Coordinate(node.X, node.Y, node.Z));
      }
      // Add the first node again, to close the polygon
      node = element.Nodes[0];
      coordinates.Add(new Coordinate(node.X, node.Y, node.Z));

      IPolygon elementPolygon = geomFactory.CreatePolygon(coordinates.ToArray());

      return elementPolygon;
    }

    /// <summary>
    /// Create a polygon from a mesh element.
    /// </summary>
    public static IPolygon ElementToPolygon(this SMeshData mesh, int element)
    {
      return ElementToPolygon(mesh, element, GeomFactory);
    }

    /// <summary>
    /// Create a polygon from a mesh element.
    /// </summary>
    public static IPolygon ElementToPolygon(this SMeshData mesh, int element, GeometryFactory geomFactory)
    {
      int[] elementNodes = mesh.ElementTable[element];
      List<Coordinate> coordinates = new List<Coordinate>(elementNodes.Length);
      
      int node;
      for (int i = 0; i < elementNodes.Length; i++)
      {
        node = elementNodes[i];
        coordinates.Add(new Coordinate(mesh.X[node], mesh.Y[node], mesh.Z[node]));
      }
      // Add the first node again, to close the polygon
      node = elementNodes[0];
      coordinates.Add(new Coordinate(mesh.X[node], mesh.Y[node], mesh.Z[node]));

      IPolygon elementPolygon = geomFactory.CreatePolygon(coordinates.ToArray());

      return elementPolygon;
    }

    /// <summary>
    /// Create an envelop around from a mesh element.
    /// </summary>
    public static Envelope EnvelopeInternal(this MeshElement element)
    {
      List<MeshNode> elementNodes = element.Nodes;
      double minx = elementNodes[0].X;
      double miny = elementNodes[0].Y;
      double maxx = elementNodes[0].X;
      double maxy = elementNodes[0].Y;
      for (int i = 1; i < elementNodes.Count; i++)
      {
        minx = minx < elementNodes[i].X ? minx : elementNodes[i].X;
        maxx = maxx > elementNodes[i].X ? maxx : elementNodes[i].X;
        miny = miny < elementNodes[i].Y ? miny : elementNodes[i].Y;
        maxy = maxy > elementNodes[i].Y ? maxy : elementNodes[i].Y;
      }
      return new Envelope(minx, maxx, miny, maxy);
    }

    /// <summary>
    /// Create an envelop around from a mesh element.
    /// </summary>
    public static Envelope ElementEnvelopeInternal(this SMeshData mesh, int element)
    {

      int[] elementNodes = mesh.ElementTable[element];
      double minx = mesh.X[elementNodes[0]];
      double miny = mesh.Y[elementNodes[0]];
      double maxx = mesh.X[elementNodes[0]];
      double maxy = mesh.Y[elementNodes[0]];
      for (int i = 1; i < elementNodes.Length; i++)
      {
        minx = minx < mesh.X[elementNodes[i]] ? minx : mesh.X[elementNodes[i]];
        maxx = maxx > mesh.X[elementNodes[i]] ? maxx : mesh.X[elementNodes[i]];
        miny = miny < mesh.Y[elementNodes[i]] ? miny : mesh.Y[elementNodes[i]];
        maxy = maxy > mesh.Y[elementNodes[i]] ? maxy : mesh.Y[elementNodes[i]];
      }
      return new Envelope(minx, maxx, miny, maxy);
    }
  }
}
