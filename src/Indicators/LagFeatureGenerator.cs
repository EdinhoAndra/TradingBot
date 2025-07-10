using System;
using System.Collections.Generic;
using System.Linq;

namespace Edison.Trading.Indicators
{
    public static class LagFeatureGenerator
    {
        public static void AddLaggedFeatures(Dictionary<string, Memory<double>> featureMap, IEnumerable<int> lags, IEnumerable<string>? featureNames = null)
        {
            var baseFeatures = featureNames != null ? featureNames.ToArray() : featureMap.Keys.ToArray();

            foreach (int lag in lags)
            {
                foreach (string name in baseFeatures)
                {
                    var src = featureMap[name].Span;
                    var destArr = new double[src.Length];
                    var dest = destArr.AsSpan();
                    for (int i = 0; i < src.Length; i++)
                        dest[i] = i >= lag ? src[i - lag] : double.NaN;

                    featureMap[$"{name}_lag_{lag}"] = destArr;
                }
            }
        }
    }
}
