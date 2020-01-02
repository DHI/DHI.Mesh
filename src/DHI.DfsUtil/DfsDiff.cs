using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS;

namespace DHI.DfsUtil
{
  /// <summary>
  /// Class for making a diff-file from two other files that have the same structure
  /// (same items and time steps), but actual numbers vary. 
  /// </summary>
  class DfsDiff
  {
    public static readonly string CreateDiffFileUsage =
@"
    -diff: Find the difference between two, in structure, identical files,
        and create new file with difference values.

        DHI.DfsUtil -diff [file1] [file2]
        DHI.DfsUtil -diff [file1] [file2] [diffFile]

        The two input files must be equal in structure, e.g. coming
        from the same simulation but giving different results.
        Header and static data must be identical, only difference
        must be in values of the dynamic data.

        If not providing a diffFile, the file is not created, but
        difference statistics is written to console.

        In case of one file having delete value, and the other not 
        having delete value, the difference may turn out big, even
        if in reality the results differ only little, i.e. for 
        water level results from a flooding simulation.
";

    /// <summary>
    /// Create a new file, being the difference of two files.
    /// <para>
    /// The two input files must be equal in structure, e.g. coming
    /// from the same simulation but giving different results.
    /// Header and static data must be identical, only difference
    /// must be in values of the dynamic data.
    /// </para>
    /// </summary>
    public static void CreateDiffFile(string file1, string file2, string filediff = null)
    {
      IDfsFile dfs1 = DfsFileFactory.DfsGenericOpen(file1);
      IDfsFile dfs2 = DfsFileFactory.DfsGenericOpen(file2);

      // Validate that it has the same number of items.
      if (dfs1.ItemInfo.Count != dfs2.ItemInfo.Count)
        throw new Exception("Number of dynamic items does not match");
      int numItems = dfs1.ItemInfo.Count;

      // In case number of time steps does not match, take the smallest.
      int numTimes = dfs1.FileInfo.TimeAxis.NumberOfTimeSteps;
      if (numTimes > dfs2.FileInfo.TimeAxis.NumberOfTimeSteps)
      {
        numTimes = dfs2.FileInfo.TimeAxis.NumberOfTimeSteps;
        Console.Out.WriteLine("Number of time steps does not match, using the smallest number");
      }

      // For recording max difference for every item
      double[] maxDiff = new double[dfs1.ItemInfo.Count];
      // Index in time (index) of maximum and first difference. -1 if no difference
      int[] maxDiffTime = new int[dfs1.ItemInfo.Count];
      int[] firstDiffTime = new int[dfs1.ItemInfo.Count];
      for (int i = 0; i < dfs1.ItemInfo.Count; i++)
      {
        maxDiffTime[i] = -1;
        firstDiffTime[i] = -1;
      }

      // Copy over info from the first file, assuming the second file contains the same data.
      IDfsFileInfo fileInfo = dfs1.FileInfo;

      DfsBuilder builder = null;

      if (!string.IsNullOrEmpty(filediff))
      {
        builder = DfsBuilder.Create(fileInfo.FileTitle, fileInfo.ApplicationTitle, fileInfo.ApplicationVersion);

        // Set up the header
        builder.SetDataType(fileInfo.DataType);
        builder.SetGeographicalProjection(fileInfo.Projection);
        builder.SetTemporalAxis(fileInfo.TimeAxis);
        builder.SetItemStatisticsType(fileInfo.StatsType);
        builder.DeleteValueByte        = fileInfo.DeleteValueByte;
        builder.DeleteValueDouble      = fileInfo.DeleteValueDouble;
        builder.DeleteValueFloat       = fileInfo.DeleteValueFloat;
        builder.DeleteValueInt         = fileInfo.DeleteValueInt;
        builder.DeleteValueUnsignedInt = fileInfo.DeleteValueUnsignedInt;

        // Transfer compression keys.
        if (fileInfo.IsFileCompressed)
        {
          int[] xkey;
          int[] ykey;
          int[] zkey;
          fileInfo.GetEncodeKey(out xkey, out ykey, out zkey);
          builder.SetEncodingKey(xkey, ykey, zkey);
        }

        // Copy custom blocks
        foreach (IDfsCustomBlock customBlock in fileInfo.CustomBlocks)
        {
          builder.AddCustomBlock(customBlock);
        }
      }

      // Copy dynamic item definitions
      bool[] floatItems = new bool[dfs1.ItemInfo.Count];
      for (int i = 0; i < dfs1.ItemInfo.Count; i++)
      {
        var itemInfo = dfs1.ItemInfo[i];

        // Validate item sizes
        var itemInfo2 = dfs2.ItemInfo[i];
        if (itemInfo.ElementCount != itemInfo2.ElementCount)
          throw new Exception("Dynamic items must have same size, item number " + (i + 1) +
                              " has different sizes in the two files");
        // Validate the data type, only supporting floats and doubles.
        if (itemInfo.DataType == DfsSimpleType.Float)
          floatItems[i] = true;
        else if (itemInfo.DataType != DfsSimpleType.Double)
          throw new Exception("Dynamic item must be double or float, item number " + (i + 1) + " is of type " +
                              (itemInfo.DataType));

        builder?.AddDynamicItem(itemInfo);
      }

      // Create file
      builder?.CreateFile(filediff);

      if (builder != null)
      {
        // Copy over static items from file 1, assuming the static items of file 2 are identical
        IDfsStaticItem si1;
        while (null != (si1 = dfs1.ReadStaticItemNext()))
        {
          builder.AddStaticItem(si1);
        }
      }

      // Get the file
      DfsFile diff = builder?.GetFile();

      float deleteValueFloat = dfs1.FileInfo.DeleteValueFloat;
      double deleteValueDouble     = dfs1.FileInfo.DeleteValueDouble;

      int[] deleteValueDiffCount = new int[numItems];
      // Write dynamic data to the file, being the difference between the two
      for (int i = 0; i < numTimes; i++)
      {
        for (int j = 0; j < numItems; j++)
        {
          if (floatItems[j])
          {
            float deleteValue = deleteValueFloat;
            IDfsItemData<float> data1 = dfs1.ReadItemTimeStepNext() as IDfsItemData<float>;
            IDfsItemData<float> data2 = dfs2.ReadItemTimeStepNext() as IDfsItemData<float>;
            for (int k = 0; k < data1.Data.Length; k++)
            {
              if ((data1.Data[k] == deleteValue) && (data2.Data[k] == deleteValue))
              {
                // Both has delete values
                data1.Data[k] = deleteValue;
              }
              else if (data1.Data[k] == deleteValue)
              {
                // One is delete value, the other is not
                deleteValueDiffCount[j]++;
                data1.Data[k] = -data2.Data[k];
                if (firstDiffTime[j] == -1)
                  firstDiffTime[j] = i;
              }
              else if (data2.Data[k] == deleteValue)
              {
                // One is delete value, the other is not
                deleteValueDiffCount[j]++;
                data1.Data[k] = data1.Data[k];
                if (firstDiffTime[j] == -1)
                  firstDiffTime[j] = i;
              }
              else
              {
                // Both has values
                float valuediff = data1.Data[k] - data2.Data[k];
                data1.Data[k] = valuediff;
                float absValueDiff = Math.Abs(valuediff);
                if (absValueDiff > maxDiff[j])
                {
                  maxDiff[j]     = absValueDiff;
                  maxDiffTime[j] = i;
                  if (firstDiffTime[j] == -1)
                    firstDiffTime[j] = i;
                }
              }
            }
            diff?.WriteItemTimeStepNext(data1.Time, data1.Data);
          }
          else
          {
            double deleteValue = deleteValueDouble;
            IDfsItemData<double> data1 = dfs1.ReadItemTimeStepNext() as IDfsItemData<double>;
            IDfsItemData<double> data2 = dfs2.ReadItemTimeStepNext() as IDfsItemData<double>;
            for (int k = 0; k < data1.Data.Length; k++)
            {
              if ((data1.Data[k] == deleteValue) && (data2.Data[k] == deleteValue))
              {
                // Both has delete values
                data1.Data[k] = deleteValue;
              }
              else if (data1.Data[k] == deleteValue)
              {
                // One is delete value, the other is not
                deleteValueDiffCount[j]++;
                data1.Data[k] = -data2.Data[k];
                if (firstDiffTime[j] == -1)
                  firstDiffTime[j] = i;
              }
              else if (data2.Data[k] == deleteValue)
              {
                // One is delete value, the other is not
                deleteValueDiffCount[j]++;
                data1.Data[k] = data1.Data[k];
                if (firstDiffTime[j] == -1)
                  firstDiffTime[j] = i;
              }
              else
              {
                // Both has values
                double valuediff = data1.Data[k] - data2.Data[k];
                data1.Data[k] = valuediff;
                double absValueDiff = Math.Abs(valuediff);
                if (absValueDiff > maxDiff[j])
                {
                  maxDiff[j]     = absValueDiff;
                  maxDiffTime[j] = i;
                  if (firstDiffTime[j] == -1)
                    firstDiffTime[j] = i;
                }
              }
            }
            diff?.WriteItemTimeStepNext(data1.Time, data1.Data);
          }
        }
      }

      System.Console.WriteLine("Difference statistics:");
      for (int i = 0; i < maxDiffTime.Length; i++)
      {
        if (firstDiffTime[i] < 0)
        {
          Console.WriteLine("{0,-30}: no difference", dfs1.ItemInfo[i].Name);
        }
        else
        {
          if (deleteValueDiffCount[i] == 0)
            Console.WriteLine("{0,-30}: Max difference at timestep {1,3}: {2}. First difference at timestep {3}.", dfs1.ItemInfo[i].Name, maxDiffTime[i], maxDiff[i], firstDiffTime[i]);
          else
            Console.WriteLine("{0,-30}: Max difference at timestep {1,3}: {2}. First difference at timestep {3}. DeleteValue differences: {4}", dfs1.ItemInfo[i].Name, maxDiffTime[i], maxDiff[i], firstDiffTime[i], deleteValueDiffCount[i]);
          Environment.ExitCode = -5;
        }
      }

      dfs1.Close();
      dfs2.Close();
      diff?.Close();

    }
  }
}
