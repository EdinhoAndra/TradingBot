using System.Collections.Generic;
using Edison.Trading.Core;
using System;

namespace Edison.Trading.Indicators
{
    public static class ReturnsCalculator
    {
        public static Memory<double> PctChange(IList<RenkoBrick> bricks, int lag = 1, bool logReturn = false)
        {
            var prices = new double[bricks.Count];
            for (int i = 0; i < bricks.Count; i++)
                prices[i] = bricks[i].Close;
            return PctChange(prices, lag, logReturn);
        }

        public static Memory<double> PctChange(ReadOnlySpan<double> series, int lag = 1, bool logReturn = false)
        {
            int n = series.Length;
            var result = new double[n];
            for (int i = 0; i < n; i++)
            {
                int j = i - lag;
                if (j >= 0)
                {
                    double ratio = series[i] / series[j];
                    result[i] = logReturn ? Math.Log(ratio) : ratio - 1.0;
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
