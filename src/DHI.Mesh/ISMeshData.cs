using System;
using System.Collections.Generic;
using System.Text;

namespace DHI.Mesh
{
  public interface ISMeshData : IMeshDataInfo
  {
    /// <summary>
    /// Node Id's. Can be null, then default value is assumed.
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
    int[] NodeIds { get; set; }

    /// <summary>
    /// Node X coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    double[] X { get; set; }

    /// <summary>
    /// Node Y coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    double[] Y { get; set; }

    /// <summary>
    /// Node Z coordinates.
    /// <para>
    /// You can modify each coordinate individually directly in the list, 
    /// or provide a new array of coordinates, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    double[] Z { get; set; }

    /// <summary>
    /// Node boundary code.
    /// <para>
    /// You can modify each value individually directly in the list, 
    /// or provide a new array of values, which must have the same
    /// length as the original one.
    /// </para>
    /// </summary>
    int[] Code { get; set; }

    /// <summary>
    /// Element Id's. Can be null, then default value is assumed.
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
    int[] ElementIds { get; set; }
  }
}
