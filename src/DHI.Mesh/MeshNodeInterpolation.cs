using System;

namespace DHI.Mesh
{
  /// <summary>
  /// Class for setting up an <see cref="Interpolator"/> for interpolating
  /// element center values to node values.
  /// <para>
  /// Based on a Pseudo Laplace procedure by [Holmes, Connell 1989]. Uses an inverse distance
  /// average if the pseudo laplace procedure fails. 
  /// </para>
  /// <para>
  ///   Holmes, D. G. and Connell, S. D. (1989), Solution of the
  ///       2D Navier-Stokes on unstructured adaptive grids, AIAA Pap.
  ///       89-1932 in Proc.AIAA 9th CFD Conference.
  /// </para>
  /// </summary>
  public partial class MeshNodeInterpolation
  {
    private bool         _allowExtrapolation;
    private Interpolator _nodeInterpolator;
    
    public MeshNodeInterpolation()
    {
    }

    /// <summary>
    /// Allow extrapolation, by disabling clipping of the omega weights
    /// <para>
    /// Default is false.
    /// </para>
    /// </summary>
    public bool AllowExtrapolation
    {
      get { return _allowExtrapolation; }
      set { _allowExtrapolation = value; }
    }

    /// <summary>
    /// Interpolator
    /// </summary>
    public Interpolator NodeInterpolator
    {
      get { return _nodeInterpolator; }
      set { _nodeInterpolator = value; }
    }

    /// <summary>
    /// Setup interpolation for all nodes in the mesh.
    /// </summary>
    public void Setup(MeshData mesh)
    {
      var nodeInterpData = new Interpolator.InterPData[mesh.Nodes.Count];
      for (int i = 0; i < mesh.Nodes.Count; i++)
      {
        MeshNode node = mesh.Nodes[i];
        nodeInterpData[i] = SetupNodeInterpolation(node);
      }

      _nodeInterpolator = new Interpolator(nodeInterpData);
    }

    /// <summary>
    /// Calculate interpolation weights for interpolating a value at the provided <paramref name="node"/>
    /// from the surrounding element center values.
    /// </summary>
    /// <param name="node">Node to setup interpolation for</param>
    public Interpolator.InterPData SetupNodeInterpolation(MeshNode node)
    {

      Interpolator.InterPData interpData = new Interpolator.InterPData()
      {
        Indices = new int[node.Elements.Count],
        Weights = new double[node.Elements.Count],
      };

      double omegaTot = 0;

      if (node.Elements.Count >= 3)
      {

        double Ixx      = 0;
        double Iyy      = 0;
        double Ixy      = 0;
        double Rx       = 0;
        double Ry       = 0;

        for (int i = 0; i < node.Elements.Count; i++)
        {
          MeshElement element = node.Elements[i];
          double      dx      = element.XCenter - node.X;
          double      dy      = element.YCenter - node.Y;

          Ixx += dx * dx;
          Iyy += dy * dy;
          Ixy += dx * dy;
          Rx  += dx;
          Ry  += dy;

          interpData.Indices[i] = element.Index;
        }

        double lambda = Ixx * Iyy - Ixy * Ixy;

        if (lambda > 1e-10 * (Ixx * Iyy))
        {
          // Standard case - Pseudo Laplacian

          for (int i = 0; i < node.Elements.Count; i++)
          {
            MeshElement element = node.Elements[i];
            double      dx      = element.XCenter - node.X;
            double      dy      = element.YCenter - node.Y;

            double lambda_x = (Ixy * Ry - Iyy * Rx) / lambda;
            double lambda_y = (Ixy * Rx - Ixx * Ry) / lambda;

            double omega = 1.0 + lambda_x * dx + lambda_y * dy;
            if (!_allowExtrapolation)
            {
              if (omega < 0)
                omega = 0;
              else if (omega > 2)
                omega = 2;
            }

            interpData.Weights[i] =  omega;
            omegaTot              += omega;
          }
        }
      }

      if (omegaTot <= 1e-10)
      {
        // We did not succeed using pseudo laplace procedure, 
        // use inverse distance instead
        omegaTot = 0;
        for (int i = 0; i < node.Elements.Count; i++)
        {
          MeshElement element = node.Elements[i];
          double      dx      = element.XCenter - node.X;
          double      dy      = element.YCenter - node.Y;

          // Inverse distance weighted interpolation weight
          double omega = 1 / Math.Sqrt(dx * dx + dy * dy);

          interpData.Weights[i] =  omega;
          omegaTot              += omega;
        }
      }

      // Scale to 1
      if (omegaTot != 0)
      {
        for (int i = 0; i < interpData.Weights.Length; i++)
        {
          interpData.Weights[i] /= omegaTot;
        }
      }
      else
      {
        for (int i = 0; i < interpData.Weights.Length; i++)
        {
          interpData.Weights[i] = 0;
        }
      }

      //double sum = 0;
      //for (int i = 0; i < interpData.Weights.Length; i++)
      //{
      //  sum += interpData.Weights[i];
      //}
      //if (Math.Abs(sum - 1) > 1e-12)
      //  Console.Out.WriteLine("Duh!!!: "+node.Index);

      return interpData;
    }


  }
}