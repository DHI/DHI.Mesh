using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DHI.Mesh
{

  /// <summary>
  /// A mesh, consisting of triangles and quadrilaterals elements.
  /// <para>
  /// Compared to the <see cref="MeshData"/>, the <see cref="MeshFile"/>
  /// stores node and element data in arrays. 
  /// </para>
  /// <para>
  /// This class also supports reading and writing of the DHI mesh files.
  /// </para>
  /// </summary>
  public class MeshFile
  {
    // projection as wkt-string
    private string _wktString;

    // quantity of data in the mesh file
    private MeshUnit _zUnit;

    // Node variables
    private int[] _nodeIds; // this can be null, then set default id's, starting from 1
    private double[] _x;
    private double[] _y;
    private double[] _z;
    private int[] _code;

    // Element variables
    private int[] _elementIds; // this can be null, then set default id's, starting from 1
    private int[] _elementType;
    private int[][] _connectivity;

    private bool _hasQuads;

    /// <summary>
    /// Unit of the <see cref="Z"/> variable.
    /// </summary>
    public MeshUnit ZUnit { get { return _zUnit; } }

    #region Geometry region

    /// <summary>
    /// The projection string, in WKT format.
    /// </summary>
    public string Projection
    {
      get { return (_wktString); }
      set { _wktString = value; }
    }

    /// <summary>
    /// Number of nodes in the mesh.
    /// </summary>
    public int NumberOfNodes { get { return (_nodeIds.Length); } }

    /// <summary>
    /// Number of elements in the mesh
    /// </summary>
    public int NumberOfElements { get { return (_elementIds.Length); } }


    /// <summary>
    /// Node Id's
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// <para>
    /// Be aware that changing this to anything but the default values (1,2,3,...)
    /// can make some tools stop working.
    /// </para>
    /// </summary>
    public int[] NodeIds
    {
      get { return _nodeIds; }
      set
      {
        if (_nodeIds.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _nodeIds = value;
      }
    }

    /// <summary>
    /// Node X coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public double[] X
    {
      get { return _x; }
      set
      {
        if (_x.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _x = value;
      }
    }

    /// <summary>
    /// Node Y coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public double[] Y
    {
      get { return _y; }
      set
      {
        if (_y.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _y = value;
      }
    }

    /// <summary>
    /// Node Z coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public double[] Z
    {
      get { return _z; }
      set
      {
        if (_z.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _z = value;
      }
    }

    /// <summary>
    /// Node boundary code.
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public int[] Code
    {
      get { return _code; }
      set
      {
        if (_code.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of nodes");
        _code = value;
      }
    }

    /// <summary>
    /// Element Id's
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// <para>
    /// Be aware that changing this to anything but the default values (1,2,3,...)
    /// can make some tools stop working.
    /// </para>
    /// </summary>
    public int[] ElementIds
    {
      get { return _elementIds; }
      set
      {
        if (_elementIds.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of elements");
        _elementIds = value;
      }
    }


    /// <summary>
    /// Array of element types. See documentation for each type.
    /// </summary>
    // TODO: Make into a enum
    public int[] ElementType
    { get { return _elementType; } }

    /// <summary>
    /// The <see cref="ElementTable"/> defines for each element which 
    /// nodes that defines the element. 
    /// <para>
    /// The numbers in the <see cref="ElementTable"/> are node numbers, not indices!
    /// Each value in the table must be between 1 and number-of-nodes.
    /// </para>
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    public int[][] ElementTable
    {
      get { return _connectivity; }
      set
      {
        if (_connectivity.Length != value.Length)
          throw new ArgumentException("Length of input does not match number of elements");
        _connectivity = value;
      }
    }

    #endregion

    private static readonly Regex _header2012 = new Regex(@"(\d+)\s+(\d+)\s+(\d+)\s+(.+)", RegexOptions.Compiled);
    private static readonly Regex _header2011 = new Regex(@"(\d+)\s+(.+)", RegexOptions.Compiled);

    /// <summary>
    /// Read .mesh file from stream and load all data.
    /// <para>
    /// If an element specifies a node number of zero, that node number is ignored, and
    /// does not become a part of the mesh data structure. That is the case for e.g.
    /// mixed triangular/quadrilateral meshes, where all elements specify 4 nodes, 
    /// and triangular elements specifies the last node as zero.
    /// </para>
    /// </summary>
    public void Read(Stream stream, string streamId = "")
    {
      TextReader tr = new StreamReader(stream, System.Text.Encoding.Default);
      Read(tr, streamId);
    }

    /// <summary>
    /// Read .mesh file and load all data.
    /// <para>
    /// If an element specifies a node number of zero, that node number is ignored, and
    /// does not become a part of the mesh data structure. That is the case for e.g.
    /// mixed triangular/quadrilateral meshes, where all elements specify 4 nodes, 
    /// and triangular elements specifies the last node as zero.
    /// </para>
    /// </summary>
    public void Read(string filename)
    {
      TextReader tr = new StreamReader(filename, System.Text.Encoding.Default);
      Read(tr, filename);
    }

    /// <summary>
    /// Read .mesh file from reader and load all data.
    /// <para>
    /// If an element specifies a node number of zero, that node number is ignored, and
    /// does not become a part of the mesh data structure. That is the case for e.g.
    /// mixed triangular/quadrilateral meshes, where all elements specify 4 nodes, 
    /// and triangular elements specifies the last node as zero.
    /// </para>
    /// </summary>
    public void Read(TextReader tr, string filename)
    {
      string line;
      try
      {

        char[] separator = new char[] { ' ', '\t' };

        // Read header line
        line = tr.ReadLine();
        if (line == null)
          throw new IOException("Can not load mesh file. File is empty");
        // Remove any leading spaces if present
        line = line.Trim();
        int noNodes = 0;
        string proj = null;
        // First try match the 2012 header line format
        Match match = _header2012.Match(line);
        if (match.Success)
        {
          // We just ignore the itemType integer, assuming it has the EUM value of eumIBathymetry (100079)
          int itemType = Int32.Parse(match.Groups[1].Value);
          int itemUnit = Int32.Parse(match.Groups[2].Value);
          _zUnit = MeshUnitUtil.FromEum(itemUnit);
          noNodes = Int32.Parse(match.Groups[3].Value);
          proj = match.Groups[4].Value;
        }
        // If not successfull, try match the 2011 header line format
        if (proj == null)
        {
          match = _header2011.Match(line);
          if (match.Success)
          {
            _zUnit = MeshUnit.Meter;
            noNodes = Int32.Parse(match.Groups[1].Value);
            proj = match.Groups[2].Value;
          }
        }
        if (proj == null)
          throw new IOException(string.Format("Can not load mesh file (failed reading mesh file header line): {0}", filename));
        _wktString = proj.Trim();

        string[] strings;

        // Allocate memory for nodes
        _nodeIds = new int[noNodes];
        _x = new double[noNodes];
        _y = new double[noNodes];
        _z = new double[noNodes];
        _code = new int[noNodes];

        // Read nodes
        try
        {
          for (int i = 0; i < noNodes; i++)
          {
            line = tr.ReadLine();
            if (line == null)
              throw new IOException("Unexpected end of file"); // used as inner exception

            line = line.Trim();
            strings = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            _nodeIds[i] = int.Parse(strings[0]);
            _x[i] = double.Parse(strings[1], NumberFormatInfo.InvariantInfo);
            _y[i] = double.Parse(strings[2], NumberFormatInfo.InvariantInfo);
            _z[i] = double.Parse(strings[3], NumberFormatInfo.InvariantInfo);
            _code[i] = int.Parse(strings[4]);
          }

        }
        catch (Exception inner)
        {
          throw new Exception(string.Format("Can not load mesh file (failed reading nodes): {0}", filename), inner);
        }

        // Reading element header line
        int noElements;
        int maxNoNodesPerElement;
        int elmtCode;
        line = tr.ReadLine();
        if (line == null)
          throw new IOException(string.Format("Can not load mesh file (unexpected end of file): {0}", filename));
        line = line.Trim();
        strings = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (strings.Length != 3)
          throw new IOException(string.Format("Can not load mesh file (failed reading element header line): {0}", filename));
        try
        {
          noElements = int.Parse(strings[0]);
          maxNoNodesPerElement = int.Parse(strings[1]);
          elmtCode = int.Parse(strings[2]);
        }
        catch (Exception ex)
        {
          throw new Exception(string.Format("Can not load mesh file (failed reading element header line): {0}", filename), ex);
        }

        // Element code must be 21 or 25 (21 for triangular meshes, 25 for mixed meshes)
        if (elmtCode != 21 || elmtCode != 25)
        {
          // TODO: Do we care?
        }

        // Allocate memory for elements
        _elementIds = new int[noElements];
        _elementType = new int[noElements];
        _connectivity = new int[noElements][];

        // Temporary (reused) list of nodes in one element
        List<int> nodesInElement = new List<int>(maxNoNodesPerElement);

        // Read all elements
        try
        {
          for (int i = 0; i < noElements; i++)
          {
            nodesInElement.Clear();

            // Read element header line
            line = tr.ReadLine();
            if (line == null)
              throw new IOException("Unexpected end of file"); // used as inner exception

            line = line.Trim();
            strings = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            // Read element id
            _elementIds[i] = int.Parse(strings[0]);
            // figure out number of nodes
            int noNodesInElmt = strings.Length - 1;
            for (int j = 0; j < noNodesInElmt; j++)
            {
              int nodeNumber = int.Parse(strings[j + 1]);
              // Check that the node number exists
              if (nodeNumber < 0 || nodeNumber > noNodes) // used as inner exception:
                throw new IOException("Node number in element table is negative or larger than number of nodes");
              // It is only a node in the element if the node number is positive
              if (nodeNumber > 0)
              {
                nodesInElement.Add(nodeNumber);
              }
            }
            _connectivity[i] = nodesInElement.ToArray();

            // Get element type from number of nodes
            if (_connectivity[i].Length == 3)
              _elementType[i] = 21;
            else if (_connectivity[i].Length == 4)
            {
              _elementType[i] = 25;
              _hasQuads = true;
            }
            else
            {
              _elementType[i] = 0;
              // TODO: Throw an exception?
            }
          }
        }
        catch (Exception inner)
        {
          throw new Exception(string.Format("Can not load mesh file (failed reading elements): {0}", filename), inner);
        }

      }
      finally
      {
        try
        {
          tr.Close();
        }
        catch { }
      }

    }

    /// <summary>
    /// Write <see cref="MeshFile"/> to filename
    /// </summary>
    public void Write(string filename)
    {
      // All double values are written using the "r" format string in order to assure correct
      // round-tripping (not loosing any decimals when reading again)

      TextWriter tw = new StreamWriter(filename);

      // Mesh file header line
      // EUM Item integer value for eumIBathymetry
      tw.Write(100079);
      tw.Write(" ");
      tw.Write(_zUnit.ToEum());
      tw.Write(" ");
      tw.Write(_nodeIds.Length);
      tw.Write(" ");
      tw.WriteLine(_wktString);

      // Node information
      for (int i = 0; i < _nodeIds.Length; i++)
      {
        tw.Write(_nodeIds[i]);
        tw.Write(" ");
        tw.Write(_x[i].ToString("r", NumberFormatInfo.InvariantInfo));
        tw.Write(" ");
        tw.Write(_y[i].ToString("r", NumberFormatInfo.InvariantInfo));
        tw.Write(" ");
        tw.Write(_z[i].ToString("r", NumberFormatInfo.InvariantInfo));
        tw.Write(" ");
        tw.WriteLine(_code[i]);
      }

      int maxNoNodesPerElmt;
      tw.Write(_elementIds.Length);
      tw.Write(" ");
      if (!_hasQuads)
      {
        maxNoNodesPerElmt = 3;
        tw.Write("3");
        tw.Write(" ");
        tw.WriteLine("21");
      }
      else
      {
        maxNoNodesPerElmt = 4;
        tw.Write("4");
        tw.Write(" ");
        tw.WriteLine("25");
      }

      // Element information
      for (int i = 0; i < _elementIds.Length; i++)
      {
        tw.Write(_elementIds[i]);
        int[] nodes = _connectivity[i];
        for (int j = 0; j < nodes.Length; j++)
        {
          tw.Write(" ");
          tw.Write(nodes[j]);

        }
        // Fill up with zeros
        for (int j = nodes.Length; j < maxNoNodesPerElmt; j++)
        {
          tw.Write(" ");
          tw.Write(0);
        }
        tw.WriteLine();
      }

      tw.Close();
    }

    internal static MeshFile Create(MeshUnit zUnit, string wktString, int[] nodeIds, double[] x, double[] y, double[] z, int[] nodeCode, int[] elmtIds, int[] elmtTypes, int[][] connectivity)
    {
      MeshFile res = new MeshFile();
      res._zUnit = zUnit;
      res._wktString = wktString;
      res._nodeIds = nodeIds;
      res._x = x;
      res._y = y;
      res._z = z;
      res._code = nodeCode;
      res._elementIds = elmtIds;
      res._elementType = elmtTypes;
      res._connectivity = connectivity;
      for (int i = 0; i < connectivity.Length; i++)
      {
        if (connectivity[i].Length == 4)
        {
          res._hasQuads = true;
          break;
        }
      }
      return (res);
    }

    /// <summary>
    /// Read the mesh from the provided mesh file
    /// </summary>
    public static MeshFile ReadMesh(string filename)
    {
      if (!File.Exists(filename))
        throw new FileNotFoundException("File not found", filename);
      var file = new MeshFile();
      file.Read(filename);
      return (file);
    }

  }
}
