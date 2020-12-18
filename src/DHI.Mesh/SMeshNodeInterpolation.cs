using System;
using System.Collections.Generic;

namespace DHI.Mesh
{
  public partial class MeshNodeInterpolation
  {
    /// <summary>
    /// Setup interpolation for all nodes in the mesh.
    /// </summary>
    public void Setup(SMeshData mesh)
    {
      var nodeInterpData = new Interpolator.InterPData[mesh.NumberOfNodes];
      for (int i = 0; i < mesh.NumberOfNodes; i++)
      {
        nodeInterpData[i] = SetupNodeInterpolation(mesh, i);
      }

      _nodeInterpolator = new Interpolator(nodeInterpData);
    }

    /// <summary>
    /// Calculate interpolation weights for interpolating a value at the node <paramref name="nodeIndex"/>
    /// from the surrounding element center values.
    /// </summary>
    /// <param name="mesh">Mesh object</param>
    /// <param name="nodeIndex">Index of node to setup interpolation for</param>
    public Interpolator.InterPData SetupNodeInterpolation(SMeshData mesh, int nodeIndex)
    {
      List<int> nodeElements = mesh.NodesElmts[nodeIndex];
      Interpolator.InterPData interpData = new Interpolator.InterPData()
      {
        Indices = new int[nodeElements.Count],
        Weights = new double[nodeElements.Count],
      };

      double omegaTot = 0;

      // Pseudo-Laplacian only works in case of more than 3 elements connected to the node.
      if (nodeElements.Count >= 3)
      {

        double Ixx = 0;
        double Iyy = 0;
        double Ixy = 0;
        double Rx  = 0;
        double Ry  = 0;

        for (int i = 0; i < nodeElements.Count; i++)
        {
          int elementIndex = nodeElements[i];
          double dx = mesh.ElementXCenter[elementIndex] - mesh.X[nodeIndex];
          double dy = mesh.ElementYCenter[elementIndex] - mesh.Y[nodeIndex];

          Ixx += dx * dx;
          Iyy += dy * dy;
          Ixy += dx * dy;
          Rx  += dx;
          Ry  += dy;

          interpData.Indices[i] = elementIndex;
        }

        double lambda = Ixx * Iyy - Ixy * Ixy;

        if (lambda > 1e-10 * (Ixx * Iyy))
        {
          // Standard case - Pseudo Laplacian

          double lambda_x = (Ixy * Ry - Iyy * Rx) / lambda;
          double lambda_y = (Ixy * Rx - Ixx * Ry) / lambda;

          for (int i = 0; i < nodeElements.Count; i++)
          {
            int elementIndex = nodeElements[i];
            double dx = mesh.ElementXCenter[elementIndex] - mesh.X[nodeIndex];
            double dy = mesh.ElementYCenter[elementIndex] - mesh.Y[nodeIndex];

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
        for (int i = 0; i < nodeElements.Count; i++)
        {
          int elementIndex = nodeElements[i];
          double dx = mesh.ElementXCenter[elementIndex] - mesh.X[nodeIndex];
          double dy = mesh.ElementYCenter[elementIndex] - mesh.Y[nodeIndex];

          // Inverse distance weighted interpolation weight
          double omega = 1 / Math.Sqrt(dx * dx + dy * dy);

          interpData.Indices[i] = elementIndex;
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
