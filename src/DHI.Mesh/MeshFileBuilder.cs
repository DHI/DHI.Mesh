using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Mesh
{
  /// <summary>
  /// Builder for creating a mesh file.
  /// <para>
  /// The following must be set:
  /// <see cref="SetProjection(string)"/>,
  /// <see cref="SetNodes(double[],double[],double[],int[])"/>,
  /// <see cref="SetElements"/>.
  /// Other setters are optional and if not set, default values are used.
  /// </para>
  /// <para>
  /// Be aware; setting the node and element id's to anything but the default
  /// values can cause some tools to fail.
  /// </para>
  /// </summary>
  public class MeshFileBuilder
  {
    private string _projection;
    private MeshUnit _zUnit;

    private bool _isSetProjection;
    private bool _isSetNodes;
    private bool _isSetConnectivity;

    // Node variables
    private int[] _nodeIds; // this can be null, then set default id's, starting from 1
    private double[] _x;
    private double[] _y;
    private double[] _z;
    private int[] _code;

    // Element variables
    private int[] _elementIds; // this can be null, then set default id's, starting from 1
    private int[][] _connectivity;

    /// <summary>
    /// Set the projection to use for the mesh
    /// </summary>
    public void SetProjection(string projection)
    {
      if (projection == null)
        throw new ArgumentNullException("projection");
      _projection = projection;
      _isSetProjection = true;
    }

    /// <summary>
    /// Set the quantity to use for the mesh Z variable. If not set, 
    /// it will use a Bathymetry item type (eumIBathymetry) 
    /// with meter unit (eumUmeter).
    /// </summary>
    public void SetZUnit(MeshUnit zUnit)
    {
      _zUnit = zUnit;
    }

    /// <summary>
    /// Set node coordinates and code.
    /// <para>
    /// Coordinates are converted to doubles and stored.
    /// </para>
    /// </summary>
    public void SetNodes(float[] x, float[] y, float[] z, int[] code)
    {
      if (x == null)
        throw new ArgumentNullException("x");
      if (y == null)
        throw new ArgumentNullException("y");
      if (z == null)
        throw new ArgumentNullException("z");

      SetNodes(Convert(x), Convert(y), Convert(z), code);
    }

    /// <summary>
    /// Set node coordinates and code.
    /// </summary>
    public void SetNodes(double[] x, double[] y, double[] z, int[] code)
    {
      if (x == null)
        throw new ArgumentNullException("x");
      if (y == null)
        throw new ArgumentNullException("y");
      if (z == null)
        throw new ArgumentNullException("z");
      if (code == null)
        throw new ArgumentNullException("code");

      int numberOfNodes = x.Length;

      if (numberOfNodes != y.Length || numberOfNodes != z.Length || numberOfNodes != code.Length)
      {
        throw new ArgumentException(
          string.Format("All arguments must have same length. Lengths are: x={0}, y={1}, z={2}, code={3}",
                        x.Length, y.Length, z.Length, code.Length));
      }

      if (_nodeIds != null && numberOfNodes != _nodeIds.Length)
        throw new ArgumentException("Arguments does not have same length as the number of node ids. These must match");

      _x = x;
      _y = y;
      _z = z;
      _code = code;

      _isSetNodes = true;
    }

    /// <summary>
    /// Set the node id's. Optional. If not set, default values are used (1,2,3,...)
    /// </summary>
    public void SetNodeIds(int[] nodeIds)
    {
      if (nodeIds == null)
      {
        _nodeIds = null;
        return;
      }
      if (_x != null && _x.Length != nodeIds.Length)
        throw new ArgumentException("Number of node id's does not match number of nodes", "nodeIds");
      _nodeIds = nodeIds;
    }

    /// <summary>
    /// Set element connectivity: For each element is specified which nodes
    /// the element consist of. The node is specified by its index into the list of nodes.
    /// </summary>
    public void SetElements(int[][] connectivity)
    {
      if (connectivity == null)
        throw new ArgumentNullException("connectivity");
      if (connectivity.Length == 0)
        throw new ArgumentException("Element table has no rows. There must be at least one row");

      if (_elementIds != null && _elementIds.Length != connectivity.Length)
        throw new ArgumentException("Number of elements is not the same as number of element ids. They must match");

      // Check number of elements
      for (int i = 0; i < connectivity.Length; i++)
      {
        int[] elmnt = connectivity[i];
        if (3 > elmnt.Length || elmnt.Length > 4)
        {
          throw new ArgumentException(
            string.Format("All elements must have 3 or 4 nodes. Element number {0} has {1} nodes", i + 1,
                          elmnt.Length));
        }
      }

      _connectivity = connectivity;

      _isSetConnectivity = true;
    }

    /// <summary>
    /// Set the element id's. Optional. If not set, default values are used (1,2,3,...)
    /// </summary>
    public void SetElementIds(int[] elementIds)
    {
      if (_connectivity != null && _connectivity.Length != elementIds.Length)
      {
        throw new ArgumentException("Number of element id's does not match number of elements", "elementIds");
      }

    }

    /// <summary>
    /// Validate will return a string of issues from the item builder.
    /// When this returns an empty list, the item has been properly build.
    /// </summary>
    public string[] Validate()
    {
      return (Validate(false));
    }

    private string[] Validate(bool dieOnError)
    {
      List<string> errors = new List<string>();

      if (!_isSetProjection)
        errors.Add("Projection has not been set");
      if (!_isSetNodes)
        errors.Add("Nodes have not been set");
      if (!_isSetConnectivity)
        errors.Add("Elements have not been set");

      // Check that all nodenumbers are within the range of
      // number of nodes.
      if (_isSetNodes && _isSetConnectivity)
      {
        bool check = true;
        foreach (int[] elmt in _connectivity)
        {
          foreach (int nodeNumber in elmt)
          {
            if (0 >= nodeNumber || nodeNumber > _x.Length)
            {
              check = false;
              break;
            }
          }
          if (!check)
            break;
        }
        if (!check)
          errors.Add("At least one element has an invalid node number. Node numbers must be within [1,numberOfNodes]");
      }

      if (dieOnError && errors.Count > 0)
      {
        string msgs = ErrorMessage(errors);
        throw new Exception(msgs);
      }

      return (errors.ToArray());
    }

    internal static string ErrorMessage(List<string> errors)
    {
      if (errors.Count == 1)
        return (errors[0]);
      string msgs = "Several issues:";
      foreach (string err in errors)
      {
        msgs += "\n  " + err;
      }
      return (msgs);
    }


    /// <summary>
    /// Create and return a new <see cref="MeshFile"/> object
    /// </summary>
    public MeshFile CreateMesh()
    {

      Validate(true);

      // Creating default eumQuantity in meters
      _zUnit = MeshUnit.Meter;

      // Creating default node id's, if empty
      if (_nodeIds == null)
      {
        // Setting node ids 1,2,3,...
        _nodeIds = new int[_x.Length];
        for (int i = 0; i < _x.Length; i++)
        {
          _nodeIds[i] = i + 1;
        }
      }
      // Creating default element id's, if empty
      if (_elementIds == null)
      {
        // Setting element ids 1,2,3,...
        _elementIds = new int[_connectivity.Length];
        for (int i = 0; i < _connectivity.Length; i++)
        {
          _elementIds[i] = i + 1;
        }
      }

      // Creating additional element information
      int[] elementType = new int[_connectivity.Length];
      int[] nodesPerElmt = new int[_connectivity.Length];
      int nodeElmtCount = 0;  // total number of nodes listed in the connectivity table
      for (int i = 0; i < elementType.Length; i++)
      {
        int elmtTypeNumber;
        int[] elmt = _connectivity[i];
        switch (elmt.Length)
        {
          case 3:
            elmtTypeNumber = 21;
            break;
          case 4:
            elmtTypeNumber = 25;
            break;
          case 6:
            elmtTypeNumber = 32;
            break;
          case 8:
            elmtTypeNumber = 33;
            break;
          default:
            // this should have been caught in the validate phase, but just in case:
            throw new Exception("Element with invalid number of nodes encountered");
        }
        elementType[i] = elmtTypeNumber;
        nodesPerElmt[i] = elmt.Length;
        nodeElmtCount += elmt.Length;
      }

      int[] connectivityArray = new int[nodeElmtCount];
      int k = 0;
      for (int i = 0; i < elementType.Length; i++)
      {
        int[] elmt = _connectivity[i];
        for (int j = 0; j < elmt.Length; j++)
        {
          connectivityArray[k++] = elmt[j];
        }
      }

      MeshFile res = MeshFile.Create(_zUnit, _projection, _nodeIds, _x, _y, _z, _code, _elementIds, elementType, _connectivity);

      return (res);
    }



    internal static double[] Convert(float[] arr)
    {
      double[] res = new double[arr.Length];
      for (int i = 0; i < arr.Length; i++)
      {
        res[i] = arr[i];
      }
      return (res);
    }

    internal static float[] Convert(double[] arr)
    {
      float[] res = new float[arr.Length];
      for (int i = 0; i < arr.Length; i++)
      {
        res[i] = (float)arr[i];
      }
      return (res);
    }
  }
}
