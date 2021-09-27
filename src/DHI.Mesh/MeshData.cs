using System;
using System.Collections.Generic;
using CMeshFace=DHI.Mesh.MeshFace;

namespace DHI.Mesh
{
  /// <summary>
  /// A mesh, consisting of triangles and quadrilaterals elements.
  /// <para>
  /// This MeshData class models the mesh using an objected oriented data model, easing access and navigation in the mesh.
  /// If memory overhead becomes too big, the <see cref="SMeshData"/> uses memory optimized data structures.
  /// </para>
  /// <para>
  /// A mesh consist of a number of nodes an elements. The node defines the coordinates.
  /// Each element is defined by a number of nodes. The number of nodes depends on the
  /// type of the element, triangular or quadrilateral, 2D or 3D, horizontal or vertical.
  /// </para>
  /// <para>
  /// For details, check the "DHI Flexible File Format" document, in the
  /// "MIKE SDK documentation index".
  /// </para>
  /// </summary>
  public class MeshData
  {
    #region Geometry region

    /// <summary>
    /// Projection string, in WKT format
    /// </summary>
    public string Projection { get; set; }

    /// <summary>
    /// Unit of the z variables in the nodes and elements.
    /// </summary>
    public MeshUnit ZUnit { get; set; }

    /// <summary>
    /// Number of nodes in the mesh.
    /// </summary>
    public int NumberOfNodes { get { return (Nodes.Count); } }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    public int NumberOfElements { get { return (Elements.Count); } }


    /// <summary>
    /// Nodes in the mesh.
    /// </summary>
    public List<MeshNode> Nodes { get; set; }
    /// <summary>
    /// Elements in the mesh.
    /// </summary>
    public List<MeshElement> Elements { get; set; }

    /// <summary>
    /// Element faces.
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="BuildFaces"/> or 
    /// <see cref="BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<CMeshFace> Faces { get; private set; }

    #endregion

    /// <summary>
    /// Create mesh from arrays.
    /// <para>
    /// Note that the <paramref name="connectivity"/> array is using zero-based indices
    /// (as compared to the <see cref="MeshFile.ElementTable"/>, which is using one-based indices)
    /// </para>
    /// </summary>
    public static MeshData CreateMesh(string projection, int[] nodeIds, double[] x, double[] y, double[] z, int[] code, int[] elementIds, int[] elementTypes, int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
    {
      MeshData meshData = new MeshData();
      meshData.Projection = projection;
      meshData.ZUnit = zUnit;
      meshData.Nodes = new List<MeshNode>(nodeIds.Length);
      meshData.Elements = new List<MeshElement>(elementIds.Length);

      for (int i = 0; i < nodeIds.Length; i++)
      {
        var node = new MeshNode()
        {
          Index = i,
          Id = nodeIds[i],
          X = x[i],
          Y = y[i],
          Z = z[i],
          Code = code[i],
        };
        meshData.Nodes.Add(node);
      }

      for (int ielmt = 0; ielmt < elementIds.Length; ielmt++)
      {
        int[] nodeInElmt = connectivity[ielmt];
        int numNodesInElmt = nodeInElmt.Length;

        var element = new MeshElement()
        {
          Index = ielmt,
          Id = elementIds[ielmt],
          ElementType = elementTypes[ielmt],
          Nodes = new List<MeshNode>(numNodesInElmt),
        };
        double xc = 0;
        double yc = 0;
        double zc = 0;

        for (int j = 0; j < numNodesInElmt; j++)
        {
          int nodeIndex = nodeInElmt[j];
          MeshNode meshNode = meshData.Nodes[nodeIndex];
          element.Nodes.Add(meshNode);
          xc += meshNode.X;
          yc += meshNode.Y;
          zc += meshNode.Z;
        }

        double inumNodesInElmt = 1.0 / numNodesInElmt;
        element.XCenter = xc * inumNodesInElmt; // / numNodesInElmt;
        element.YCenter = yc * inumNodesInElmt; // / numNodesInElmt;
        element.ZCenter = zc * inumNodesInElmt; // / numNodesInElmt;

        meshData.Elements.Add(element);
      }

      return meshData;

    }

    /// <summary>
    /// Build derived mesh data, the <see cref="MeshNode.Elements"/> and <see cref="MeshFace"/> lists.
    /// </summary>
    public List<string> BuildDerivedData()
    {
      BuildNodeElements();
      List<string> errors = BuildFaces(true);
      return errors;
    }

    public void BuildNodeElements()
    {
      if (Nodes[0].Elements != null)
        return;

      // Build up element list in nodes
      for (int i = 0; i < Nodes.Count; i++)
      {
        Nodes[i].Elements = new List<MeshElement>();
      }

      for (int ielmt = 0; ielmt < Elements.Count; ielmt++)
      {
        MeshElement    element        = Elements[ielmt];
        List<MeshNode> nodeInElmt     = element.Nodes;
        int            numNodesInElmt = nodeInElmt.Count;

        for (int j = 0; j < numNodesInElmt; j++)
        {
          MeshNode meshNode = nodeInElmt[j];
          meshNode.Elements.Add(element);
        }
      }
    }

    /// <summary>
    /// Build up the list of <see cref="Faces"/>
    /// </summary>
    /// <param name="elmtFaces">Also build up <see cref="MeshElement.Faces"/></param>
    public List<string> BuildFaces(bool elmtFaces = false)
    {
      List<string> errors = new List<string>();

      int numberOfNodes    = NumberOfNodes;
      int numberOfElements = NumberOfElements;

      bool hasElementFaces = Elements[0].Faces != null;

      //System.Diagnostics.Stopwatch timer = MeshExtensions.StartTimer();

      if (Faces == null)
      {

        // Build up face lists
        // The exact number of faces is: NumberOfElements+NumberOfNodes + numberOfSubMeshes - numberOfHoles
        Faces = new List<CMeshFace>((int)((numberOfElements+numberOfNodes)*1.01));

        // Preallocate list of face on all nodes - used in next loop
        for (int i = 0; i < Nodes.Count; i++)
          Nodes[i].Faces = new List<CMeshFace>();
        //timer.ReportAndRestart("Prealloc nodeface");

        //watch.Start();
        // Create all faces.
        for (int ielmt = 0; ielmt < Elements.Count; ielmt++)
        {
          MeshElement element = Elements[ielmt];
          List<MeshNode> elmtNodes = element.Nodes;
          if (elmtFaces)
            element.Faces = new List<CMeshFace>(elmtNodes.Count);
          for (int j = 0; j < elmtNodes.Count; j++)
          {
            MeshNode fromNode = elmtNodes[j];
            MeshNode toNode   = elmtNodes[(j + 1) % elmtNodes.Count];
            AddFace(element, fromNode, toNode);
          }
        }
        //timer.ReportAndRestart("Create faces "+Faces.Count);

        // Figure out boundary code
        for (int i = 0; i < Faces.Count; i++)
        {
          CMeshFace face = Faces[i];
          face.SetBoundaryCode(errors);
        }
        //timer.ReportAndRestart("Set Boundary Code");

      }

      if (elmtFaces && !hasElementFaces)
      {
        // If not already created, create the lists
        if (Elements[0].Faces == null)
        {
          for (int ielmt = 0; ielmt < numberOfElements; ielmt++)
          {
            Elements[ielmt].Faces = new List<CMeshFace>();
          }
        }

        // Add face to the elements list of faces
        for (int i = 0; i < Faces.Count; i++)
        {
          CMeshFace face = Faces[i];
          face.LeftElement.Faces.Add(face);
          if (face.RightElement != null)
            face.RightElement.Faces.Add(face);
        }
      }
      //timer.ReportAndRestart("Create element faces");

      return errors;
    }

    /// <summary>
    /// Create and add a face.
    /// <para>
    /// A face is only "added once", i.e. when two elements share the face, it is found twice,
    /// once defined as "toNode"-"fromNode" and once as "fromNode"-"toNode". The second time,
    /// the existing face is being reused, and the element is added as the <see cref="CMeshFace.RightElement"/>
    /// </para>
    /// <para>
    /// The <see cref="CMeshFace"/> is added to the global list of faces, and also to tne nodes list of faces.
    /// </para>
    /// </summary>
    private void AddFace(MeshElement element, MeshNode fromNode, MeshNode toNode)
    {
      List<CMeshFace> fromNodeFaces = fromNode.Faces;
      List<CMeshFace> toNodeFaces = toNode.Faces;

      // Try find "reverse face" going from to-node to from-node.
      // The FindIndex with delegate is 10+ times slower than the tight loop below.
      //int reverseFaceIndex = toNodeFaces.FindIndex(mf => mf.ToNode == fromNode);
      int reverseToNodeFaceIndex = -1;
      // Look in all faces starting from toNode
      for (int i = 0; i < toNodeFaces.Count; i++)
      {
        // Check if the face goes to fromNode
        if (toNodeFaces[i].ToNode == fromNode)
        {
          reverseToNodeFaceIndex = i;
          break;
        }
      }

      if (reverseToNodeFaceIndex >= 0)
      {
        // Found reverse face, reuse it and add the element as the RightElement
        CMeshFace reverseFace = toNodeFaces[reverseToNodeFaceIndex];
        reverseFace.RightElement = element;
      }
      else
      {
        // Found new face, set element as LeftElement and add it to both from-node and to-node
        CMeshFace meshFace = new CMeshFace(fromNode, toNode)
        {
          LeftElement = element,
        };

        Faces.Add(meshFace);
        fromNodeFaces.Add(meshFace);
        // Adding to toNodeFaces is not required for the algorithm to work,
        // however, it is required in order to get NodesFaces lists right
        toNodeFaces.Add(meshFace);
      }
    }

    /// <summary>
    /// Create and add a face - special version for <see cref="MeshBoundaryExtensions.GetBoundaryFaces"/>
    /// <para>
    /// A face is only "added once", i.e. when two elements share the face, it is found twice,
    /// once defined as "toNode"-"fromNode" and once as "fromNode"-"toNode". The second time,
    /// the existing face is being reused, and the element is added as the <see cref="CMeshFace.RightElement"/>
    /// </para>
    /// <para>
    /// The <see cref="CMeshFace"/> is added to the global list of faces, and also to tne nodes list of faces.
    /// </para>
    /// </summary>
    internal static void AddFace(MeshElement element, MeshNode fromNode, MeshNode toNode, List<CMeshFace>[] nodeFaces)
    {
      List<CMeshFace> fromNodeFaces = nodeFaces[fromNode.Index];
      List<CMeshFace> toNodeFaces = nodeFaces[toNode.Index];

      // Try find "reverse face" going from from-node to to-node.
      // The FindIndex with delegate is 10+ times slower than the tight loop below.
      //int reverseFaceIndex = toNodeFaces.FindIndex(mf => mf.ToNode == fromNode);
      int reverseFaceIndex = -1;
      for (int i = 0; i < toNodeFaces.Count; i++)
      {
        if (toNodeFaces[i].ToNode == fromNode)
        {
          reverseFaceIndex = i;
          break;
        }
      }

      if (reverseFaceIndex >= 0)
      {
        // Found reverse face, reuse it and add the element as the RightElement
        CMeshFace reverseFace = toNodeFaces[reverseFaceIndex];
        reverseFace.RightElement = element;
      }
      else
      {
        // Found new face, set element as LeftElement and add it to both from-node and to-node
        CMeshFace meshFace = new CMeshFace(fromNode, toNode)
        {
          LeftElement = element,
        };

        fromNodeFaces.Add(meshFace);
      }
    }

    /// <summary>
    /// Determines whether the specified mesh geometry is equal to the current mesh's geometry.
    /// The order of the nodes and elements in the mesh must equal.
    /// <para>
    /// This only checks the geometry, i.e. node coordinates, node boundary codes and element connectivity.
    /// The Id's in the mesh may differ. 
    /// </para>
    /// </summary>
    /// <param name="other"></param>
    /// <param name="tolerance"></param>
    public bool EqualsGeometry(MeshData other, double tolerance = 1e-3)
    {
      if (this.Nodes.Count != other.Nodes.Count ||
          this.Elements.Count != other.Elements.Count)
        return false;

      for (int i = 0; i < this.Nodes.Count; i++)
      {
        MeshNode nt = this.Nodes[i];
        MeshNode no = other.Nodes[i];
        if (Math.Abs(nt.X - no.X) > tolerance ||
            Math.Abs(nt.Y - no.Y) > tolerance ||
            nt.Code != no.Code)
          return false;
      }

      for (int i = 0; i < this.Elements.Count; i++)
      {
        MeshElement et = this.Elements[i];
        MeshElement eo = other.Elements[i];
        if (et.Nodes.Count != eo.Nodes.Count)
          return false;
        for (int j = 0; j < et.Nodes.Count; j++)
        {
          if (et.Nodes[j].Index != eo.Nodes[j].Index)
            return false;
        }
      }
      return true;
    }
  }
}
