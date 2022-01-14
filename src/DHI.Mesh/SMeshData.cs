using System;
using System.Collections.Generic;
using CMeshFace=DHI.Mesh.SMeshFace;

namespace DHI.Mesh
{

  /// <summary>
  /// A mesh, consisting of triangles and quadrilaterals elements.
  /// <para>
  /// This SMeshData class uses low-level structures to save on memory, compared to
  /// the <see cref="MeshData"/> class that uses a more object oriented approach
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
  public class SMeshData : SMeshDataBase, IMeshDataInfo
  {
    public SMeshData(string projection, int[] nodeIds, double[] x, double[] y, double[] z, int[] code, int[] elementIds, int[] elementType, int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
      : base(projection, nodeIds, x, y, z, code, elementIds, elementType, connectivity, zUnit)
    {
    }

    private double[] _elmtXCenter;
    /// <summary> For each element, a list of element center coordinates</summary>
    public double[] ElementXCenter
    {
      get { return _elmtXCenter; }
    }

    private double[] _elmtYCenter;
    /// <summary> For each element, a list of element center coordinates</summary>
    public double[] ElementYCenter
    {
      get { return _elmtYCenter; }
    }

    private double[] _elmtZCenter;
    /// <summary> For each element, a list of element center coordinates</summary>
    public double[] ElementZCenter
    {
      get { return _elmtZCenter; }
    }

    private List<int>[] _nodesElmts;
    /// <summary> For each node, a list of elements that is connected to this node </summary>
    public List<int>[] NodesElmts
    {
      get { return _nodesElmts; }
    }

    /// <summary>
    /// Element faces.
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="BuildFaces"/> or 
    /// <see cref="BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<CMeshFace> Faces { get; private set; }

    /// <summary>
    /// Faces that is connected to each node. 
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="BuildFaces"/> or 
    /// <see cref="BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<int>[] NodesFaces { get; private set; }
    /// <summary>
    /// Faces (boundary of element) that each element is defined by.
    /// <para>
    /// This is a derived feature. It is initially null, but can be created by calling
    /// <see cref="BuildFaces"/> or 
    /// <see cref="BuildDerivedData"/>.
    /// </para>
    /// </summary>
    public List<int>[] ElementsFaces { get; private set; }

    /// <summary>
    /// Build all derived data:
    /// <see cref="CalcElementCenters"/>,
    /// <see cref="BuildNodeElements"/>,
    /// <see cref="BuildFaces"/>
    /// </summary>
    /// <returns>A list of errors, or null</returns>
    public List<string> BuildDerivedData()
    {
      CalcElementCenters();
      BuildNodeElements();
      List<string> errors = BuildFaces(true);
      return errors;
    }

    /// <summary>
    /// Calculate element center values,
    /// <see cref="ElementXCenter"/>,
    /// <see cref="ElementYCenter"/> and
    /// <see cref="ElementZCenter"/>.
    /// </summary>
    public void CalcElementCenters()
    {
      if (_elmtXCenter != null)
        return;

      int numberOfElements = NumberOfElements;
      _elmtXCenter = new double[numberOfElements];
      _elmtYCenter = new double[numberOfElements];
      _elmtZCenter = new double[numberOfElements];
      for (int ielmt = 0; ielmt < numberOfElements; ielmt++)
      {
        int[] nodeInElmt     = _connectivity[ielmt];
        int   numNodesInElmt = nodeInElmt.Length;

        double xc = 0;
        double yc = 0;
        double zc = 0;

        for (int j = 0; j < numNodesInElmt; j++)
        {
          int      nodeIndex = nodeInElmt[j];
          xc += X[nodeIndex];
          yc += Y[nodeIndex];
          zc += Z[nodeIndex];
        }

        double inumNodesInElmt = 1.0 / numNodesInElmt;
        _elmtXCenter[ielmt] = xc * inumNodesInElmt; // / numNodesInElmt;
        _elmtYCenter[ielmt] = yc * inumNodesInElmt; // / numNodesInElmt;
        _elmtZCenter[ielmt] = zc * inumNodesInElmt; // / numNodesInElmt;
      }
    }

    /// <summary>
    /// Build the <see cref="NodesElmts"/> lists.
    /// </summary>
    public void BuildNodeElements()
    {
      if (NodesElmts != null)
        return;

      int numberOfNodes    = NumberOfNodes;
      int numberOfElements = NumberOfElements;
      _nodesElmts = new List<int>[numberOfNodes];

      // Build up element list in nodes
      for (int i = 0; i < numberOfNodes; i++)
      {
        _nodesElmts[i] = new List<int>();
      }

      for (int ielmt = 0; ielmt < numberOfElements; ielmt++)
      {
        int[]     elmtNodes     = _connectivity[ielmt];
        int       numNodesInElmt = elmtNodes.Length;
        for (int j = 0; j < numNodesInElmt; j++)
        {
          int       meshNode  = elmtNodes[j];
          List<int> nodeElmts = _nodesElmts[meshNode];
          nodeElmts.Add(ielmt);
        }
      }
    }

    /// <summary>
    /// Build faces of mesh. It will by default build <see cref="Faces"/> and the
    /// <see cref="NodesFaces"/>. It will only build the <see cref="ElementsFaces"/>
    /// if the <paramref name="elmtFaces"/> flag is set.
    /// </summary>
    /// <returns>String of errors. Null if no errors was found</returns>
    public List<string> BuildFaces(bool elmtFaces = false)
    {
      List<string> errors = new List<string>();

      int numberOfNodes    = NumberOfNodes;
      int numberOfElements = NumberOfElements;

      bool hasElementFaces = ElementsFaces != null;

      //System.Diagnostics.Stopwatch timer = MeshExtensions.StartTimer();

      if (Faces == null)
      {

        // Build up face lists
        // The exact number of faces is: NumberOfElements+NumberOfNodes + numberOfSubMeshes - numberOfHoles
        Faces = new List<CMeshFace>((int)((numberOfElements+numberOfNodes)*1.01));
        if (elmtFaces)
          ElementsFaces = new List<int>[numberOfElements];

        // Preallocate list of face on all nodes - used in next loop
        NodesFaces = new List<int>[numberOfNodes];
        for (int i = 0; i < numberOfNodes; i++)
          NodesFaces[i] = new List<int>();
        //timer.ReportAndRestart("Prealloc nodeface");

        //watch.Start();
        // Create all faces.
        for (int ielmt = 0; ielmt < numberOfElements; ielmt++)
        {
          int   element   = ielmt;
          int[] elmtNodes = _connectivity[element];
          if (elmtFaces)
            ElementsFaces[ielmt] = new List<int>(elmtNodes.Length);
          for (int j = 0; j < elmtNodes.Length; j++)
          {
            int fromNode = elmtNodes[j];
            int toNode   = elmtNodes[(j + 1) % elmtNodes.Length];
            AddFace(element, fromNode, toNode);
          }
        }
        //timer.ReportAndRestart("Create faces "+Faces.Count);

        // Figure out boundary code
        for (int i = 0; i < Faces.Count; i++)
        {
          CMeshFace face = Faces[i];
          face.SetBoundaryCode(this, errors);
        }
        //timer.ReportAndRestart("Set Boundary Code");

      }

      if (elmtFaces && !hasElementFaces)
      {
        // If not already created, create the lists
        if (ElementsFaces == null)
        {
          ElementsFaces = new List<int>[numberOfElements];
          for (int ielmt = 0; ielmt < numberOfElements; ielmt++)
          {
            ElementsFaces[ielmt] = new List<int>();
          }
        }

        // Add face to the elements list of faces
        for (int i = 0; i < Faces.Count; i++)
        {
          CMeshFace face = Faces[i];
          ElementsFaces[face.LeftElement].Add(i);
          if (face.RightElement >= 0)
            ElementsFaces[face.RightElement].Add(i);
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
    private void AddFace(int element, int fromNode, int toNode)
    {
      List<int> fromNodeFaces = NodesFaces[fromNode];
      List<int> toNodeFaces   = NodesFaces[toNode];

      // Try find "reverse face" going from to-node to from-node.
      // The FindIndex with delegate is 10+ times slower than the tight loop below.
      //int reverseFaceIndex = toNodeFaces.FindIndex(mf => mf.ToNode == fromNode);
      int reverseToNodeFaceIndex = -1;
      // Look in all faces starting from toNode
      for (int i = 0; i < toNodeFaces.Count; i++)
      {
        // Check if the face goes to fromNode
        if (Faces[toNodeFaces[i]].ToNode == fromNode)
        {
          reverseToNodeFaceIndex = i;
          break;
        }
      }

      if (reverseToNodeFaceIndex >= 0)
      {
        // Found reverse face, reuse it and add the element as the RightElement
        CMeshFace reverseFace = Faces[toNodeFaces[reverseToNodeFaceIndex]];
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
        fromNodeFaces.Add(Faces.Count-1);
        // Adding to toNodeFaces is not required for the algorithm to work,
        // however, it is required in order to get NodesFaces lists right
        toNodeFaces.Add(Faces.Count-1);
      }
    }

    /// <summary>
    /// Create and add a face - special version for <see cref="SMeshBoundaryExtensions.GetBoundaryFaces(SMeshData, bool)"/>
    /// <para>
    /// A face is only "added once", i.e. when two elements share the face, it is found twice,
    /// once defined as "toNode"-"fromNode" and once as "fromNode"-"toNode". The second time,
    /// the existing face is being reused, and the element is added as the <see cref="CMeshFace.RightElement"/>
    /// </para>
    /// <para>
    /// The <see cref="CMeshFace"/> is added to the global list of faces, and also to tne nodes list of faces.
    /// </para>
    /// </summary>
    internal static void AddFace(int element, int fromNode, int toNode, List<CMeshFace>[] facesFromNode)
    {
      List<CMeshFace> facesFromToNode = facesFromNode[toNode];

      // Try find "reverse face" going from from-node to to-node.
      // The FindIndex with delegate is 10+ times slower than the tight loop below.
      //int reverseFaceIndex = toNodeFaces.FindIndex(mf => mf.ToNode == fromNode);
      int reverseFaceIndex = -1;
      for (int i = 0; i < facesFromToNode.Count; i++)
      {
        if (facesFromToNode[i].ToNode == fromNode)
        {
          reverseFaceIndex = i;
          break;
        }
      }

      if (reverseFaceIndex >= 0)
      {
        // Found reverse face, reuse it and add the element as the RightElement
        CMeshFace reverseFace = facesFromToNode[reverseFaceIndex];
        reverseFace.RightElement = element;
      }
      else
      {
        // Found new face, set element as LeftElement and add it to both from-node and to-node
        CMeshFace meshFace = new CMeshFace(fromNode, toNode)
        {
          LeftElement = element,
        };

        facesFromNode[fromNode].Add(meshFace);
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
    public bool EqualsGeometry(SMeshData other, double tolerance = 1e-3)
    {
      if (this.NumberOfNodes != other.NumberOfNodes ||
          this.NumberOfElements != other.NumberOfElements)
        return false;

      for (int i = 0; i < this.NumberOfNodes; i++)
      {
        if (Math.Abs(this.X[i] - other.X[i]) > tolerance ||
            Math.Abs(this.Y[i] - other.Y[i]) > tolerance ||
            this.Code[i] != other.Code[i])
          return false;
      }

      for (int i = 0; i < this.NumberOfElements; i++)
      {
        if (this.ElementTable[i].Length != other.ElementTable[i].Length)
          return false;
        for (int j = 0; j < this.ElementTable[i].Length; j++)
        {
          if (this.ElementTable[i][j] != other.ElementTable[i][j])
            return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Create mesh from arrays.
    /// <para>
    /// The arrays <see cref="NodeIds"/>, <see cref="ElementIds"/>, <see cref="ElementType"/>
    /// are created with default values.
    /// </para>
    /// <para>
    /// Note that the <paramref name="connectivity"/> array is using zero-based indices
    /// (as compared to the <see cref="MeshFile.ElementTable"/>, which is using one-based indices)
    /// </para>
    /// </summary>
    public static SMeshData CreateMesh(
      string projection, double[] x, double[] y, double[] z, int[] code,
      int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
    {
      return new SMeshData(projection, null, x, y, z, code, null, null, connectivity, zUnit);
    }

    /// <summary>
    /// Create mesh from arrays.
    /// <para>
    /// Note that the <paramref name="connectivity"/> array is using zero-based indices
    /// (as compared to the <see cref="MeshFile.ElementTable"/>, which is using one-based indices)
    /// </para>
    /// </summary>
    public static SMeshData CreateMesh(SMeshDataBase database, int[] elementTypes, int[][] connectivity, MeshUnit zUnit = MeshUnit.Meter)
    {
      return new SMeshData(database.Projection, database.NodeIds, database.X, database.Y, database.Z, database.Code, database.ElementIds, elementTypes, connectivity, zUnit);
    }
  }
}
