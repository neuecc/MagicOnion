using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark.ClientLib
{
    internal static class TimeSpanExtensions
    {
        public static TimeSpan Sum(this IEnumerable<TimeSpan> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (!sources.Any())
                return TimeSpan.Zero;

            var sum = TimeSpan.Zero;
            foreach (var source in sources)
            {
                sum += source;
            }
            return sum;
        }
        public static TimeSpan Average(this IEnumerable<TimeSpan> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (!sources.Any())
                return TimeSpan.Zero;

            var sum = sources.Sum();
            var avg = sum / sources.Count();
            return avg;
        }
    }
}
