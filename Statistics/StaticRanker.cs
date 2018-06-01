using System.Collections.Generic;
using System.IO;
using System.Linq;
using CIlibProcessor.Common;

namespace CIlibProcessor.Statistics
{
    public static class StaticRanker
    {
        public static List<RankOutput> Rank(List<Algorithm> algorithms, string measure, int? iteration = null,
            double alpha = 0.05, bool maximize = false)
        {
            List<RankOutput> output = algorithms.Select(alg => new RankOutput(alg.Name, 0, 0, measure)).ToList();


            IterationStats[] stats;
            if (iteration.HasValue)
            {
                stats = algorithms.Select(x => x.GetMeasurement(measure)[iteration.Value]).ToArray();
            }
            else
            {
                stats = algorithms.Select(x => x.GetMeasurement(measure).FinalIteration).ToArray();
            }

            //perform a pairwise Mann-Whitney U test with Holm correction to assess individual differences
            double[,] mwp = MannWhitneyUTest.PairwiseCalculate(stats);

            for (int j = 0; j < mwp.GetLength(0); j++) //row
            {
                for (int k = 0; k < mwp.GetLength(1); k++) //col
                {
                    //if no significant difference, move to next column
                    if (double.IsNaN(mwp[j, k]) || !(mwp[j, k] < alpha)) continue;
                    
                    //else, compare medians to determine which algorithm was better
                    int rowIndex = j + 1; //note: j index starts at second algorithm (i.e., j[0] = alg[1])

                    int best;
                    int worst;
                    if (stats[rowIndex].Median > stats[k].Median) //alg 1 is better (assuming max)
                    {
                        best = rowIndex;
                        worst = k;
                    }
                    else //alg 2 is better (assuming max)
                    {
                        best = k;
                        worst = rowIndex;
                    }

                    if (!maximize) //swap best and worse for minimization
                    {
                        int temp = best;
                        best = worst;
                        worst = temp;
                    }

                    output[best].Wins++;
                    output[worst].Losses++;
                }
            }

            foreach (RankOutput rankOutput in output.OrderBy(x => x.Algorithm))
            {
                //TODO: this is horrible runtime, but correctly ranks the solutions
                rankOutput.Rank = output.Count(x => x.Difference > rankOutput.Difference) +1;
            }

            return output;
        }

        
        /// <summary>
        /// Write the results of Mann-Whitney U ranking to a file.
        /// </summary>
        /// <param name="ranks">A list of rank information.</param>
        /// <param name="directory">The directory to write the file.</param>
        public static void WriteToFile(List<RankOutput> ranks, string directory, string suffix = "")
        {
            if (!Directory.Exists(directory))         //create the output directory if it doesn't exist
                Directory.CreateDirectory(directory);
            
            string filename = Path.Combine(directory, string.Format("{0}_diff-{1}.csv", ranks.First().MeasurementName, suffix));
            
            using (TextWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine("Algorithm,Wins,Losses,Difference,Rank");
                foreach (RankOutput rankOutput in ranks.OrderBy(x => x.Algorithm))
                {
                    int rank = ranks.Count(x => x.Difference > rankOutput.Difference) + 1; //this is horrible runtime, but correctly ranks the solutions
                    writer.WriteLine($"{rankOutput.Algorithm},{rankOutput.Wins:F0},{rankOutput.Losses:F0},{rankOutput.Difference:F0},{rank}");
                }
            }
        }
    }
}