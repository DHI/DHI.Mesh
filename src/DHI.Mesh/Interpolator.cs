using System;

namespace DHI.Mesh
{
  /// <summary>
  /// Does interpolation from source data to target data.
  /// <para>
  /// It basically does a sparse matrix-vector product, though taking
  /// delete (undefined) values in source data into account, and handling
  /// degree-type data.
  /// </para>
  /// </summary>
  public partial class Interpolator
  {

    /// <summary>
    /// For a given target element/node, this defines the indices and
    /// weights for interpolating from source data.
    /// </summary>
    public struct InterPData
    {
      /// <summary> Indices in source </summary>
      public int[] Indices;
      /// <summary> Weights on source values </summary>
      public double[] Weights;
    }

    private double      _deleteValue;
    private CircularValueTypes _circularType = CircularValueTypes.Normal;

    private InterPData[] _interpData;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="interpData">Interpolation definition</param>
    public Interpolator(InterPData[] interpData)
    {
      _interpData = interpData;
    }

    /// <summary>
    /// Delete value/undefined value. Values in source data will not be
    /// used in the interpolation, if they equal this value.
    /// </summary>
    public double DeleteValue
    {
      get { return _deleteValue; }
      set { _deleteValue = value; }
    }

    /// <summary>
    /// Type of value, for interpolation of radians and degrees
    /// </summary>
    public CircularValueTypes CircularType
    {
      get { return _circularType; }
      set { _circularType = value; }
    }
  }
}