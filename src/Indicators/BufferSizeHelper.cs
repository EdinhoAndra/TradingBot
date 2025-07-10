using System;
using System.Collections.Generic;
using System.Linq;

namespace Edison.Trading.Indicators
{
    public static class BufferSizeHelper
    {
        public static int DetermineBufferSize(params IEnumerable<int>[] windows)
        {
            if (windows == null || windows.Length == 0)
                throw new ArgumentException("No window collections provided", nameof(windows));

            int max = 0;
            foreach (var set in windows)
            {
                if (set == null) continue;
                foreach (int w in set)
                    if (w > max) max = w;
            }
            return max;
        }
    }
}
