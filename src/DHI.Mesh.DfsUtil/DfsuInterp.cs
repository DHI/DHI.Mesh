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
      foreach (IDfsSimpleDynamicItemInfo itemInfo in sourceDfsu.ItemInfo)
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
        //AllowExtrapolation = true,
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

        DHI.Mesh.DfsUtil -dfsudiff [referenceFilename] [compareFilename] [diffFilename] <options>

        Compares the compare-file to the reference-file and writes differences
        to the diff-file. In case the compare-file and reference-file is not
        identical, the compare-data is interpolated to the reference-file mesh
        and then compared.

        Options:
            -deletevaluenodiff
                In case of either reference or compare file having delete value
                the result is a delete value. Default (not specified) is to treat
                such a delete value as zero.
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
    /// <param name="deleteValueDiff">If set to true, comparing delete value to non-delete value will return the non-delete value</param>
    public static void DfsuDiff(string referenceFilename, string compareFilename, string diffFilename, bool deleteValueDiff = true)
    {
      System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
      watch.Start();

      // Open reference file and comparison file
      DfsuFile refdfsu = DfsFileFactory.DfsuFileOpen(referenceFilename);
      DfsuFile comdfsu = DfsFileFactory.DfsuFileOpen(compareFilename);

      float refDeleteValueFloat = refdfsu.DeleteValueFloat;
      float comDeleteValueFloat = comdfsu.DeleteValueFloat;

      // Create diff file, matching reference file.
      DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);

      // Setup header and geometry, copy from source file
      builder.SetNodes(refdfsu.X, refdfsu.Y, refdfsu.Z, refdfsu.Code);
      builder.SetElements(refdfsu.ElementTable);
      builder.SetProjection(refdfsu.Projection);
      builder.SetZUnit(refdfsu.ZUnit);
      builder.SetTimeInfo(refdfsu.StartDateTime, refdfsu.TimeStepInSeconds);

      // Add dynamic items, copying from source
      foreach (IDfsSimpleDynamicItemInfo itemInfo in refdfsu.ItemInfo)
      {
        builder.AddDynamicItem(itemInfo.Name, itemInfo.Quantity);
      }

      DfsuFile diffDfsu = builder.CreateFile(diffFilename);

      watch.Stop();
      Console.Out.WriteLine("Create File : " + watch.Elapsed.TotalSeconds);
      watch.Reset();
      watch.Start();

      // Build up mesh structures for interpolation
      SMeshData refMesh = SCreate(refdfsu);
      SMeshData comMesh = SCreate(comdfsu);
      watch.Stop();
      Console.Out.WriteLine("Create mesh  : " + watch.Elapsed.TotalSeconds);
      watch.Reset();

      watch.Start();
      bool meshEquals = refMesh.EqualsGeometry(comMesh);
      if (!meshEquals)
        comMesh.BuildDerivedData();
      watch.Stop();
      Console.Out.WriteLine("Build Deriv : " + watch.Elapsed.TotalSeconds);
      watch.Reset();

      MeshInterpolator2D interpolator = null;
      float[]            targetData = null;

      // Do not interpolate if meshes equals
      if (!meshEquals)
      {
        watch.Start();
        // Build up interpolatin structures
        interpolator = new MeshInterpolator2D(comMesh)
        {
          DeleteValue      = comdfsu.DeleteValueFloat,
          DeleteValueFloat = comdfsu.DeleteValueFloat,
          //AllowExtrapolation = true,
        };
        interpolator.SetTarget(refMesh);
        // Temporary, interpolated compare-data
        targetData = new float[diffDfsu.NumberOfElements];
        watch.Stop();
        Console.Out.WriteLine("Interpolator: " + watch.Elapsed.TotalSeconds);
        watch.Reset();
      }

      watch.Start();

      // Loop over all time steps
      IDfsItemData<float> refData;
      IDfsItemData<float> comData;
      while (null != (refData = refdfsu.ReadItemTimeStepNext() as IDfsItemData<float>) &&
             null != (comData = comdfsu.ReadItemTimeStepNext() as IDfsItemData<float>))
      {

        if (interpolator != null)
          interpolator.InterpolateToTarget(comData.Data, targetData);
        else
        {
          targetData = comData.Data;
        }

        for (int i = 0; i < targetData.Length; i++)
        {
          // ReSharper disable CompareOfFloatsByEqualityOperator
          if      (refData.Data[i] != refDeleteValueFloat && 
                   targetData[i]   != comDeleteValueFloat)
            targetData[i] = refData.Data[i] - targetData[i];

          else if (refData.Data[i] == refDeleteValueFloat &&
                   targetData[i]   == comDeleteValueFloat)
            targetData[i] = refDeleteValueFloat;

          else if (deleteValueDiff)
          {
            if (refData.Data[i] != refDeleteValueFloat)
              targetData[i] = refData.Data[i];
            else // (targetData[i] != comDeleteValueFloat)
              targetData[i] = - targetData[i];
          }
          else
          {
            targetData[i] = refDeleteValueFloat;
          }
          // ReSharper restore CompareOfFloatsByEqualityOperator
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

    public static SMeshData SCreate(DfsuFile dfsu)
    {
      return SMeshData.CreateMesh(dfsu.Projection.WKTString, dfsu.NodeIds, dfsu.X, dfsu.Y, dfsu.Z.ToDoubleArray(), dfsu.Code, dfsu.ElementIds, dfsu.ElementType, dfsu.ElementTable.ToZeroBased());
    }
    public static SMeshData SCreate(MeshFile mesh)
    {
      return SMeshData.CreateMesh(mesh.Projection, mesh.NodeIds, mesh.X, mesh.Y, mesh.Z, mesh.Code, mesh.ElementIds, mesh.ElementType, mesh.ElementTable.ToZeroBased());
    }

  }
}
