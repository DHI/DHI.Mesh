
using System;
using System.IO;
using System.Reflection;

namespace DHI.Mesh.Test
{
  public static class UnitTestHelper
  {
    //public static string _testDataDir = @"C:\Work\GitHub\DHI.Mesh\TestData\";
    public static string _testDataDir;

    /// <summary>
    /// Full path to TestData folder
    /// </summary>
    public static string TestDataDir
    {
      get
      {
        if (!string.IsNullOrEmpty(_testDataDir))
          return _testDataDir;
        string exeLocation = Assembly.GetExecutingAssembly().Location;
        int indexOf = exeLocation.IndexOf("\\src\\DHI.Mesh.Test\\", StringComparison.OrdinalIgnoreCase);
        _testDataDir = exeLocation.Substring(0, indexOf) + "\\TestData\\";
        return _testDataDir;
      }
    }

    /// <summary>
    /// Create <see cref="IMeshDataInfo"/>, depending on <paramref name="smesh"/> flag.
    /// </summary>
    public static IMeshDataInfo ToMeshData(this MeshFile file, bool smesh)
    {
      if (smesh)
        return file.ToSMeshData();
      return file.ToMeshData();
    }
  }

  public static class MeshFactory
  {
    public static IMeshIntersectionCalculator CreateIntersectionCalculator(IMeshDataInfo mesh)
    {
      if (mesh is MeshData md)
        return new MeshIntersectionCalculator(md);
      if (mesh is SMeshData smd)
        return new SMeshIntersectionCalculator(smd);
      throw new ArgumentException("Unsupported mesh: " + mesh.GetType());
    }
  }

}
