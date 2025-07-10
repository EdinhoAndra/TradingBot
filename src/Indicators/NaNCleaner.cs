using System;
using System.Collections.Generic;
using System.Linq;

namespace Edison.Trading.Indicators
{
    public static class NaNCleaner
    {
        public static void DropNaRows(ref Memory<DateTime> timestamps, Dictionary<string, Memory<double>> featureMap)
        {
            int n = timestamps.Length;
            var keep = new List<int>(n);
            for (int i = 0; i < n; i++)
            {
                bool hasNa = false;
                foreach (var mem in featureMap.Values)
                {
                    if (double.IsNaN(mem.Span[i]))
                    {
                        hasNa = true;
                        break;
                    }
                }
                if (!hasNa)
                    keep.Add(i);
            }

            var tsArr = new DateTime[keep.Count];
            for (int i = 0; i < keep.Count; i++)
                tsArr[i] = timestamps.Span[keep[i]];
            timestamps = tsArr;

            foreach (var key in featureMap.Keys.ToList())
            {
                var src = featureMap[key].Span;
                var destArr = new double[keep.Count];
                for (int i = 0; i < keep.Count; i++)
                    destArr[i] = src[keep[i]];
                featureMap[key] = destArr;
            }
        }
    }
}
