using System;
using System.Collections.Generic;
using System.Linq;
using Edison.Trading.Core;

namespace Edison.Trading.Indicators
{
    public static class VolatilityCalculators
    {
        public static Memory<double> ParkinsonVolatility(IList<RenkoBrick> bricks, int window = 20)
        {
            int n = bricks.Count;
            var high = new double[n];
            var low = new double[n];
            for (int i = 0; i < n; i++)
            {
                high[i] = bricks[i].High;
                low[i] = bricks[i].Low;
            }
            return ParkinsonVolatility(high, low, window);
        }

        public static Memory<double> ParkinsonVolatility(ReadOnlySpan<double> high, ReadOnlySpan<double> low, int window = 20)
        {
            int n = high.Length;
            var logSq = new double[n];
            for (int i = 0; i < n; i++)
            {
                double ratio = high[i] / low[i];
                double ln = Math.Log(ratio);
                logSq[i] = ln * ln;
            }

            var result = new double[n];
            double sum = 0.0;
            double factor = 1.0 / (4.0 * window * Math.Log(2.0));

            for (int i = 0; i < n; i++)
            {
                sum += logSq[i];
                if (i >= window)
                    sum -= logSq[i - window];

                if (i >= window - 1)
                    result[i] = Math.Sqrt(sum * factor);
                else
                    result[i] = double.NaN;
            }
            return result.AsMemory();
        }

        public static Memory<double> GarmanKlassVolatility(IList<RenkoBrick> bricks, int window = 20)
        {
            int n = bricks.Count;
            var open = new double[n];
            var high = new double[n];
            var low = new double[n];
            var close = new double[n];
            for (int i = 0; i < n; i++)
            {
                open[i] = bricks[i].Open;
                high[i] = bricks[i].High;
                low[i] = bricks[i].Low;
                close[i] = bricks[i].Close;
            }
            return GarmanKlassVolatility(open, high, low, close, window);
        }

        public static Memory<double> GarmanKlassVolatility(ReadOnlySpan<double> open, ReadOnlySpan<double> high, ReadOnlySpan<double> low, ReadOnlySpan<double> close, int window = 20)
        {
            int n = open.Length;
            var rs = new double[n];
            double constFactor = 2.0 * Math.Log(2.0) - 1.0;
            for (int i = 0; i < n; i++)
            {
                double logHL = Math.Log(high[i] / low[i]);
                double logCO = Math.Log(close[i] / open[i]);
                rs[i] = 0.5 * logHL * logHL - constFactor * logCO * logCO;
            }

            var result = new double[n];
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                sum += rs[i];
                if (i >= window)
                    sum -= rs[i - window];

                if (i >= window - 1)
                    result[i] = Math.Sqrt(sum / window);
                else
                    result[i] = double.NaN;
            }
            return result.AsMemory();
        }

        public static Memory<double> RealizedVolatility(ReadOnlySpan<double> returns, int window = 20)
        {
            int n = returns.Length;
            var squared = new double[n];
            for (int i = 0; i < n; i++)
                squared[i] = returns[i] * returns[i];

            var result = new double[n];
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                sum += squared[i];
                if (i >= window)
                    sum -= squared[i - window];

                if (i >= window - 1)
                    result[i] = Math.Sqrt(sum);
                else
                    result[i] = double.NaN;
            }
            return result.AsMemory();
        }
    }
}
