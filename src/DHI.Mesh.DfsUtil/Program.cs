using System;

namespace DHI.Mesh.DfsUtil
{
  class Program
  {

    /// <summary> Static constructor </summary>
    static Program()
    {
      //// The setup method will make your application find the installed MIKE assemblies at runtime.
      //// The first call of the setup method takes precedence. Any subsequent calls will be ignored.
      //// It must be called BEFORE any method using MIKE libraries is called, i.e. it is not sufficient
      //// to call it as the first thing in that method using the MIKE libraries. Often this can be achieved
      //// by having this code in the static constructor.
      //// If MIKE Core is x-copy deployed with the application, this is not required.
      //if (!DHI.Mike.Install.MikeImport.Setup(18, DHI.Mike.Install.MikeProducts.MikeCore))
      //  throw new Exception("Cannot find a proper MIKE installation");
    }

    public static readonly string usage =
@"
Usage
    DHI.Mesh.DfsUtil -[tool] [arguments]

Tools:
"
        + DfsuInterp.InterpolateUsage
        + DfsuInterp.DfsuDiffUsage
;

    static void PrintUsage()
    {
      Console.Out.WriteLine(usage);
    }


    static void Main(string[] args)
    {

      if (args.Length == 0)
      {
        PrintUsage();
        return;
      }

      string arg0 = args[0].ToLower();

      // In honor of Microsoft Word and its super users, i.e. those using Word as their text-file-editor
      // Word dislikes the '-' or '\u002D' hyphen, and is more than happy to replace it with '–' ('\u2013')
      // '-' ('\u002D') is the standard hyphen character. These are other unicode hyphen characters
      string hyphenChars = "\u05be\u1806\u2010\u2011\u2012\u2013\u2014\u2015\u2053\u2212\uFE58\uFE63\uFF0D";
      // Check if first character is one of the special hyphen characters, replace with standard hyphen.
      if (hyphenChars.IndexOf(arg0[0]) >= 0)
        arg0 = "-" + arg0.Substring(1);

      if (arg0.StartsWith("-debug"))
      {
        arg0 = arg0.Substring(6);
        System.Diagnostics.Debugger.Launch();
      }

      if (arg0.StartsWith("-dfsuinterp"))
      {
        if (args.Length != 4)
        {
          Console.Out.WriteLine("-dfsuinterp requires 3 arguments.");
          PrintUsage();
        }
        DfsuInterp.Interpolate(args[1], args[2], args[3]);
      }
      else if (arg0.StartsWith("-dfsudiff"))
      {
        if (args.Length != 4)
        {
          Console.Out.WriteLine("-dfsudiff requires 3 arguments.");
          PrintUsage();
        }
        DfsuInterp.DfsuDiff(args[1], args[2], args[3]);
      }
      else
      {
        Console.Out.WriteLine("Tool "+arg0+" is not known.");
        PrintUsage();
      }
    }
  }
}
