using System;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfsu;

namespace DHI.Mesh.DfsUtil
{
  class DfsuInterp
  {

    public static readonly string InterpolateUsage =
@"
    -dfsuinterp: Interpolate dfsu file to another mesh:

        DHI.Mesh.DfsUtil -dfsuinterp [sourceFilename] [targetMeshFilename] [targetFilename]

        Interpolate values from 'sourceFilename' to mesh defined by 
        'targetMeshFilename', and store it in 'targetFilename'. 
        The 'targetMeshFilename' can be a mesh or dfsu file.
";

    /// <summary>
    /// Interpolate values from <paramref name="sourceFilename"/> to mesh
    /// defined by <paramref name="targetMeshFilename"/>, and store it in
    /// <paramref name="targetFilename"/>
    /// </summary>
    /// <param name="sourceFilename">Source data for interpolation</param>
    /// <param name="targetMeshFilename">Target mesh to interpolate to. Can be a mesh or dfsu file</param>
    /// <param name="targetFilename">File to store interpolated data to</param>
    public static void Interpolate(string sourceFilename, string targetMeshFilename, string targetFilename)
    {
      System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
      watch.Start();

      DfsuFile sourceDfsu = DfsFileFactory.DfsuFileOpen(sourceFilename);

      DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);

      DfsuBuildGeometry(targetMeshFilename, builder);
      builder.SetTimeInfo(sourceDfsu.StartDateTime, sourceDfsu.TimeStepInSeconds);

      // Add dynamic items, copying from source
      foreach (DfsuDynamicItemInfo itemInfo in sourceDfsu.ItemInfo)
      {
        builder.AddDynamicItem(itemInfo.Name, itemInfo.Quantity);
      }

      DfsuFile targetDfsu = builder.CreateFile(targetFilename);

      watch.Stop();
      Console.Out.WriteLine("Create File : " + watch.Elapsed.TotalSeconds);
      watch.Reset();
      watch.Start();


      MeshData sourceMesh = Create(sourceDfsu);
      MeshData targetMesh = Create(targetDfsu);
      sourceMesh.BuildDerivedData();


      watch.Stop();
      Console.Out.WriteLine("Build mesh  : " + watch.Elapsed.TotalSeconds);
      watch.Reset();
      watch.Start();

      MeshInterpolator2D interpolator = new MeshInterpolator2D(sourceMesh)
      {
        DeleteValue = sourceDfsu.DeleteValueFloat,
        DeleteValueFloat = sourceDfsu.DeleteValueFloat,
      };
      interpolator.SetTarget(targetMesh);

      watch.Stop();
      Console.Out.WriteLine("Interpolator: " + watch.Elapsed.TotalSeconds);
      watch.Reset();
      watch.Start();

      // Temporary, interpolated target-data
      float[] targetData = new float[targetDfsu.NumberOfElements];

      // Add data for all item-timesteps, copying from source, interpolating
      IDfsItemData<float> sourceData;
      while (null != (sourceData = sourceDfsu.ReadItemTimeStepNext() as IDfsItemData<float>))
      {
        interpolator.InterpolateToTarget(sourceData.Data, targetData);
        targetDfsu.WriteItemTimeStepNext(sourceData.Time, targetData);
      }
      watch.Stop();
      Console.Out.WriteLine("Interpolate : " + watch.Elapsed.TotalSeconds);
      watch.Reset();

      sourceDfsu.Close();
      targetDfsu.Close();
    }


    public static readonly string DfsuDiffUsage =
@"
    -dfsudiff: Create difference file between two dfsu files:

        DHI.Mesh.DfsUtil -dfsudiff [referenceFilename] [compareFilename] [diffFilename]

        Compares the compare-file to the reference-file and writes differences
        to the diff-file. In case the compare-file and reference-file is not
        identical, the compare-data is interpolated to the reference-file mesh
        and then compared.
";

    /// <summary>
    /// Create a difference file between <paramref name="referenceFilename"/> 
    /// and <paramref name="compareFilename"/>, and store it in
    /// <paramref name="diffFilename"/>.
    /// The compare-file data is interpolated to the reference-file mesh, if
    /// meshes does not match.
    /// </summary>
    /// <param name="referenceFilename">Reference data for comparison</param>
    /// <param name="compareFilename">Comparison data</param>
    /// <param name="diffFilename">File to store difference data to</param>
    public static void DfsuDiff(string referenceFilename, string compareFilename, string diffFilename)
    {
      System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
      watch.Start();

      // Open reference file and comparison file
      DfsuFile refdfsu = DfsFileFactory.DfsuFileOpen(referenceFilename);
      DfsuFile comdfsu = DfsFileFactory.DfsuFileOpen(compareFilename);

      float deleteValueFloat = refdfsu.DeleteValueFloat;

      // Create diff file, matching reference file.
      DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);

      // Setup header and geometry, copy from source file
      builder.SetNodes(refdfsu.X, refdfsu.Y, refdfsu.Z, refdfsu.Code);
      builder.SetElements(refdfsu.ElementTable);
      builder.SetProjection(refdfsu.Projection);
      builder.SetZUnit(refdfsu.ZUnit);
      builder.SetTimeInfo(refdfsu.StartDateTime, refdfsu.TimeStepInSeconds);

      // Add dynamic items, copying from source
      foreach (DfsuDynamicItemInfo itemInfo in refdfsu.ItemInfo)
      {
        builder.AddDynamicItem(itemInfo.Name, itemInfo.Quantity);
      }

      DfsuFile diffDfsu = builder.CreateFile(diffFilename);

      watch.Stop();
      Console.Out.WriteLine("Create File : " + watch.Elapsed.TotalSeconds);
      watch.Reset();
      watch.Start();

      // Build up mesh structures for interpolation
      MeshData sourceMesh = Create(comdfsu);
      MeshData targetMesh = Create(refdfsu);
      sourceMesh.BuildDerivedData();

      watch.Stop();
      Console.Out.WriteLine("Build mesh  : " + watch.Elapsed.TotalSeconds);
      watch.Reset();
      watch.Start();

      // Build up interpolatin structures
      MeshInterpolator2D interpolator = new MeshInterpolator2D(sourceMesh)
      {
        DeleteValue = refdfsu.DeleteValueFloat,
        DeleteValueFloat = refdfsu.DeleteValueFloat,
      };
      interpolator.SetTarget(targetMesh);

      watch.Stop();
      Console.Out.WriteLine("Interpolator: " + watch.Elapsed.TotalSeconds);
      watch.Reset();
      watch.Start();

      // Temporary, interpolated compare-data
      float[] targetData = new float[diffDfsu.NumberOfElements];

      // Loop over all time steps
      IDfsItemData<float> refData;
      IDfsItemData<float> comData;
      while (null != (refData = refdfsu.ReadItemTimeStepNext() as IDfsItemData<float>) &&
             null != (comData = comdfsu.ReadItemTimeStepNext() as IDfsItemData<float>))
      {

        interpolator.InterpolateToTarget(comData.Data, targetData);

        for (int i = 0; i < targetData.Length; i++)
        {
          if (refData.Data[i] != deleteValueFloat && 
              targetData[i]   != deleteValueFloat)
            targetData[i] = refData.Data[i] - targetData[i];
          else
            targetData[i] = deleteValueFloat;
        }
        diffDfsu.WriteItemTimeStepNext(refData.Time, targetData);
      }
      watch.Stop();
      Console.Out.WriteLine("Interpolate : " + watch.Elapsed.TotalSeconds);
      watch.Reset();

      refdfsu.Close();
      comdfsu.Close();
      diffDfsu.Close();
    }



    private static void DfsuBuildGeometry(string targetMeshFilename, DfsuBuilder builder)
    {
      DfsFactory factory = new DfsFactory();

      if (targetMeshFilename.EndsWith(".mesh", StringComparison.OrdinalIgnoreCase))
      {
        MeshFile target = MeshFile.ReadMesh(targetMeshFilename);

        // Setup header and geometry, copy from source file
        builder.SetNodes(target.X, target.Y, target.Z.ToFloatArray(), target.Code);
        builder.SetElements(target.ElementTable);
        builder.SetProjection(factory.CreateProjection(target.Projection));
        builder.SetZUnit(eumUnit.eumUmeter);
      }
      else
      {
        DfsuFile target = DfsFileFactory.DfsuFileOpen(targetMeshFilename);

        // Setup header and geometry, copy from source file
        builder.SetNodes(target.X, target.Y, target.Z, target.Code);
        builder.SetElements(target.ElementTable);
        builder.SetProjection(target.Projection);
        builder.SetZUnit(eumUnit.eumUmeter);

        target.Close();
      }
    }


    public static MeshData Create(DfsuFile dfsu)
    {
      return MeshData.CreateMesh(dfsu.Projection.WKTString, dfsu.NodeIds, dfsu.X, dfsu.Y, dfsu.Z.ToDoubleArray(), dfsu.Code, dfsu.ElementIds, dfsu.ElementType, dfsu.ElementTable);
    }
    public static MeshData Create(MeshFile mesh)
    {
      return MeshData.CreateMesh(mesh.Projection, mesh.NodeIds, mesh.X, mesh.Y, mesh.Z, mesh.Code, mesh.ElementIds, mesh.ElementType, mesh.ElementTable);
    }


  }
}
