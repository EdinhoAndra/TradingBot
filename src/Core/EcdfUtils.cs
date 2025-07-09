using System;
using System.Collections.Generic;
using System.Linq;

namespace Edison.Trading.Core
{
    /// <summary>
    /// Represents the sizing decision for a single input row.
    /// </summary>
    public record EcdfDecisionRecord(
        DateTime? Date,
        double ProbClass0,
        double ProbClass1,
        double Percentile,
        int Sizing,
        int? DirectionSignal);

    /// <summary>
    /// Determines how sizing decisions should be computed.
    /// </summary>
    public enum DecisionMode
    {
        /// <summary>Return a binary execution signal filtered by percentile thresholds.</summary>
        Filter,
        /// <summary>Scale the execution position directly by the percentile value.</summary>
        Scale
    }

    public static class EcdfUtils
    {
        /// <summary>
        /// Computes ECDF percentiles of <paramref name="values"/> against the <paramref name="sample"/> distribution.
        /// </summary>
        /// <param name="sample">Reference sample used to build the ECDF.</param>
        /// <param name="values">Values to evaluate.</param>
        private static double[] ComputeEcdfPercentiles(double[] sample, double[] values)
        {
            var sorted = (double[])sample.Clone();
            Array.Sort(sorted);
            int n = sorted.Length;
            var result = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                int idx = Array.BinarySearch(sorted, v);
                if (idx < 0)
                {
                    idx = ~idx;
                }
                else
                {
                    while (idx < n && sorted[idx] <= v) idx++;
                }
                result[i] = idx / (double)n;
            }
            return result;
        }

        /// <summary>
        /// Generates sizing decisions based on the percentile of calibrated probabilities.
        /// </summary>
        /// <param name="calibrator">Calibrator used to predict probabilities.</param>
        /// <param name="xRef">Reference feature matrix.</param>
        /// <param name="xNew">New feature matrix to evaluate.</param>
        /// <param name="mode">Determines how positions are derived from percentiles.</param>
        /// <param name="minLimit">Minimum percentile threshold for execution.</param>
        /// <param name="maxLimit">Optional maximum percentile threshold for execution.</param>
        /// <param name="maxPosition">Maximum position size.</param>
        /// <param name="dates">Optional list of dates associated with rows in <paramref name="xNew"/>.</param>
        /// <param name="includeDirectionSignal">When true, adds a simple direction signal based on probability 0.5.</param>
        public static List<EcdfDecisionRecord> GenerateEcdfDecisions(
            IsotonicCalibrator calibrator,
            float[][] xRef,
            float[][] xNew,
            DecisionMode mode = DecisionMode.Filter,
            double minLimit = 0.7,
            double? maxLimit = null,
            double maxPosition = 1.0,
            IList<DateTime?>? dates = null,
            bool includeDirectionSignal = false)
        {
            if (dates != null && dates.Count != xNew.Length)
                throw new ArgumentException("Number of dates must match rows of xNew.");

            var probsRef = calibrator.PredictCalibratedBatch(xRef).Select(p => (double)p).ToArray();
            var probs1 = calibrator.PredictCalibratedBatch(xNew).Select(p => (double)p).ToArray();
            var probs0 = probs1.Select(p => 1.0 - p).ToArray();

            var percentiles = ComputeEcdfPercentiles(probsRef, probs1);

            int[] executionSignal;
            double[] position;

            if (mode == DecisionMode.Filter)
            {
                if (maxLimit.HasValue)
                {
                    executionSignal = percentiles.Select(p => p >= minLimit && p <= maxLimit.Value ? 1 : 0).ToArray();
                }
                else
                {
                    executionSignal = percentiles.Select(p => p >= minLimit ? 1 : 0).ToArray();
                }
                position = executionSignal.Select(s => s * maxPosition).ToArray();
            }
            else if (mode == DecisionMode.Scale)
            {
                position = percentiles.Select(p => p * maxPosition).ToArray();
                executionSignal = position.Select(p => p > 0 ? 1 : 0).ToArray();
            }
            else
            {
                throw new ArgumentException("Invalid mode. Use Filter or Scale.", nameof(mode));
            }

            int?[] direction = includeDirectionSignal
                ? probs1.Select(p => p >= 0.5 ? 1 : 0).Cast<int?>().ToArray()
                : Enumerable.Repeat<int?>(null, probs1.Length).ToArray();

            var result = new List<EcdfDecisionRecord>(probs1.Length);
            for (int i = 0; i < probs1.Length; i++)
            {
                DateTime? dt = dates != null ? dates[i] : (DateTime?)null;
                result.Add(new EcdfDecisionRecord(
                    dt,
                    probs0[i],
                    probs1[i],
                    percentiles[i],
                    Convert.ToInt32(position[i]),
                    direction[i]));
            }

            return result;
        }
    }
}
