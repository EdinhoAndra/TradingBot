using System;
using System.Collections.Generic;

namespace Edison.Trading.Indicators
{
    public static class FracdiffHelper
    {
        public static Memory<double> GetWeights(double d, double thresh = 1e-5, int? maxSize = null)
        {
            var w = new List<double> { 1.0 };
            int k = 1;
            while (true)
            {
                double wk = -w[^1] * (d - k + 1) / k;
                if (Math.Abs(wk) < thresh)
                    break;
                w.Add(wk);
                k++;
                if (maxSize.HasValue && w.Count >= maxSize.Value)
                    break;
            }
            return w.ToArray().AsMemory();
        }

        public static Memory<double> FracdiffSeries(ReadOnlySpan<double> series, double d, double thresh = 1e-5, int? maxSize = null)
        {
            var w = GetWeights(d, thresh, maxSize);
            int width = w.Length;
            var output = new double[series.Length];
            for (int i = 0; i < series.Length; i++)
            {
                if (i >= width - 1)
                {
                    double val = 0.0;
                    for (int k = 0; k < width; k++)
                    {
                        val += w.Span[width - 1 - k] * series[i - k];
                    }
                    output[i] = val;
                }
                else
                {
                    output[i] = double.NaN;
                }
            }
            return output.AsMemory();
        }
    }
}
