using System;
using System.Collections.Generic;
using Edison.Trading.Core;

namespace Edison.Trading.Indicators
{
    public static class RslCalculator
    {
        public static Memory<double> CalculateRsl(IList<RenkoBrick> bricks, int timePeriod = 14)
        {
            var closes = new double[bricks.Count];
            for (int i = 0; i < bricks.Count; i++)
                closes[i] = bricks[i].Close;
            return CalculateRsl(closes, timePeriod);
        }

        public static Memory<double> CalculateRsl(ReadOnlySpan<double> prices, int timePeriod = 14)
        {
            int n = prices.Length;
            var result = new double[n];
            double sum = 0.0;

            for (int i = 0; i < n; i++)
            {
                sum += prices[i];
                if (i >= timePeriod)
                    sum -= prices[i - timePeriod];

                if (i >= timePeriod - 1)
                {
                    double mean = sum / timePeriod;
                    result[i] = mean != 0.0 ? prices[i] / mean - 1.0 : double.NaN;
                }
                else
                {
                    result[i] = double.NaN;
                }
            }
            return result.AsMemory();
        }
    }
}
