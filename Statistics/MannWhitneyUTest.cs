using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CIlibProcessor.Common;

namespace CIlibProcessor.Statistics
{
    public static class MannWhitneyUTest
	{
		public static double Calculate(List<double> x, List<double> y, bool? exact = null, bool correct = true)
		{
			double pVal;

			//TODO: remove infinite/NaN x,y

			double nx = x.Count;
			double ny = y.Count;

		    if (nx < 1) throw new ArgumentException("Not enough finite x oberservations");
		    if (nx < 1) throw new ArgumentException("Not enough finite y oberservations");

			List<double> ranks = Helpers.Rank(x.Concat(y)).Ranks;

			if (!exact.HasValue)
			{
				exact = nx < 50 && ny < 50;
			}

		    //sum the ranks of the x samples
		    double sum = 0;
		    for (int i = 0; i < (int)nx; i++)
		    {
		        sum += ranks[i];
		    }

		    double statistic = sum - nx * (nx + 1) / 2;

			bool ties = ranks.Count != ranks.Distinct().Count(); //TODO: better check for ties?

			if (exact.Value && !ties)
			{
				double p = statistic > (nx * ny / 2) ? pwilcox(statistic - 1, nx, ny, 0, 0) : pwilcox(statistic, nx, ny, 1, 0);

				pVal = Math.Min(2 * p, 1);
			}
			else
			{
				Dictionary<double, int> nties = Helpers.NumTies(ranks);
				double z = statistic - nx * ny / 2;

			    double ntiesSum = 0;
			    foreach (var kvp in nties)
			    {
			        ntiesSum += Math.Pow(kvp.Value, 3) - kvp.Value;
			    }

				double sigma = Math.Sqrt((nx * ny / 12) * ((nx + ny + 1) - ntiesSum / ((nx + ny) * (nx + ny - 1))));

				double correction = 0;
				if (correct)
				{
					correction = Math.Sign(z) * 0.5;
				}

				z = (z - correction) / sigma;

				bool lower = z < 0;
				pVal = 2 * NormalDistribution.Pnorm(z, 0, 1, lower, false);
			}

			return pVal;
		}


	    /// <summary>
	    /// Pairwise tests with (optional) Holm correction.
	    /// </summary>
	    /// <returns>Array of pairwise p-values.</returns>
	    /// <param name="stats">Iteration statistics.</param>
	    /// <param name="correction">Use holm correction.</param>
	    public static double[,] PairwiseCalculate(IterationStats[] stats, bool correction = true)
		{
			double[,] results = new double[stats.Length - 1, stats.Length - 1];

			//TODO: parallel for?
			for (int i = 1; i < stats.Length; i++)
			{
				for (int j = 0; j < stats.Length - 1; j++)
				{
					if (i > j) results[i - 1, j] = Calculate(stats[i].Values, stats[j].Values, false);
					else results[i - 1, j] = double.NaN; //empty entry
				}
			}

			return correction ? HolmCorrection(results) : results;
		}

		private static double[,] HolmCorrection(double[,] pValues)
		{
			List<double> p = new List<double>();
			for (int i = 0; i < pValues.GetLength(1); i++) //columns
			{
				for (int j = 0; j < pValues.GetLength(0); j++)//rows
				{
					if (!double.IsNaN(pValues[j, i])) p.Add(pValues[j, i]);
				}
			}

			//ordered p-values
			var orderedP = Helpers.Rank(p);

			List<int> ro = Helpers.Order(orderedP.Indexes);
			List<double> pMin = new List<double>(p.Count);

			double cumMax = double.MinValue;//for cumulative max calc
			for(int i = 0; i < p.Count; i++)
			{
				double val = (p.Count - (i + 1) + 1) * p[orderedP.Indexes[i]];
				//stuff.Add(val);

				if (!double.IsNaN(val))
				{
					cumMax = Math.Max(cumMax, val);
				}
				pMin.Add(Math.Min(1, cumMax));
			}

			//order elements by values of r
			int index = 0; //reuse the index variable
			for (int i = 0; i < pValues.GetLength(1); i++) //columns
			{
				for (int j = 0; j < pValues.GetLength(0); j++) //rows
				{
					if (!double.IsNaN(pValues[j, i])) pValues[j, i] = pMin[ro[index++] - 1];
				}
			}

			return pValues;
		}

		[DllImport("Rmath", CallingConvention = CallingConvention.Cdecl)]
		private static extern double pwilcox(double q, double m, double n, int lowerTail, int logP);
	}
}