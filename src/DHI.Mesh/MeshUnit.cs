using System;

namespace DHI.Mesh
{
  /// <summary>
  /// Unit for the z-coordinate in mesh files.
  /// </summary>
  public enum MeshUnit
  {
    /// <summary> Meter </summary>
    Meter,
    /// <summary> MilliMeter </summary>
    Millimeter,
    /// <summary> CentiMeter </summary>
    Centimeter,
    /// <summary> KiloMeter </summary>
    Kilometer,
    /// <summary> International inch (2.54 cm) </summary>
    Inch,
    /// <summary> US Survey inch (100/39.37 ~ 2.5400051 cm </summary>
    InchUS,
    /// <summary> International foot (0.3048 meters) </summary>
    Feet,
    /// <summary> US Survey foot (1200/3937 ~ 0.30480061 meters) </summary>
    FeetUS,
    /// <summary> International yard (0.9144 meters) </summary>
    Yard,
    /// <summary> US Survey yard (3600/3937 ~ 0.914402 meters) </summary>
    YardUS,
    /// <summary> International mile (1.609.344 meters) </summary>
    Mile,
    /// <summary> US Survey mile (6336/3937 ~ 1609.347 meters) </summary>
    MileUS,
  }

  /// <summary>
  /// Utility class for <see cref="MeshUnit"/>.
  /// <para>
  /// Its main task is to match a <see cref="MeshUnit"/> to the corresponding EUM unit.
  /// </para>
  /// </summary>
  // The EUM assembly is deliberately not used here, to keep the two assemblies independent.
  // This class must be kept in sync with the DHI EUM system, especially the allowed units 
  // for the eumIBathymetry item.
  public static class MeshUnitUtil
  {
    /// <summary>
    /// Convert EUM unit integer into a <see cref="MeshUnit"/>
    /// </summary>
    public static MeshUnit FromEum(int eumUnitInt)
    {
      switch (eumUnitInt)
      {
        case 1000: return (MeshUnit.Meter);
        case 1002: return (MeshUnit.Millimeter);
        case 1007: return (MeshUnit.Centimeter);
        case 1001: return (MeshUnit.Kilometer);
        case 1004: return (MeshUnit.Inch);
        case 1013: return (MeshUnit.InchUS);
        case 1003: return (MeshUnit.Feet);
        case 1014: return (MeshUnit.FeetUS);
        case 1006: return (MeshUnit.Yard);
        case 1015: return (MeshUnit.YardUS);
        case 1005: return (MeshUnit.Mile);
        case 1016: return (MeshUnit.MileUS);
        default:
          throw new ArgumentOutOfRangeException(nameof(eumUnitInt), eumUnitInt, null);
      }
    }

    /// <summary>
    /// Convert <see cref="MeshUnit"/> into an EUM unit integer
    /// </summary>
    public static int ToEum(this MeshUnit meshUnit)
    {
      switch (meshUnit)
      {
        case MeshUnit.Meter: return (1000);
        case MeshUnit.Millimeter: return (1002);
        case MeshUnit.Centimeter: return (1007);
        case MeshUnit.Kilometer: return (1001);
        case MeshUnit.Inch: return (1004);
        case MeshUnit.InchUS: return (1013);
        case MeshUnit.Feet: return (1003);
        case MeshUnit.FeetUS: return (1014);
        case MeshUnit.Yard: return (1006);
        case MeshUnit.YardUS: return (1015);
        case MeshUnit.Mile: return (1005);
        case MeshUnit.MileUS: return (1016);
        default:
          throw new ArgumentOutOfRangeException(nameof(meshUnit), meshUnit, null);
      }
    }

  }

}