using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.ClientLib.Internal
{
    internal static class MathUtils
    {
        /// <summary>
        /// Geometric Average: (x1*x2*...*xn)^(1/n)
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static double GeometricMean(IEnumerable<double> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            double result = 1;
            var length = sources.Count();
            foreach (var num in sources)
            {
                result *= Math.Pow(num, 1.0 / length);
            }
            return result;
        }
        /// <summary>
        /// Standard Deviation
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static double StandardDeviation(IEnumerable<double> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            var length = sources.Count();
            if (length < 2) return 0.0;
            var sumSqrt = 0.0;
            var avg = sources.Average();
            foreach (var value in sources)
            {
                sumSqrt += Math.Pow((value - avg), 2);
            }
            return Math.Sqrt(sumSqrt / (length - 1));
        }

        public static double Median(IEnumerable<double> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            var length = sources.Count();
            var sort = sources.OrderBy(x => x).ToArray();
            int position = length / 2;
            var median = length % 2 == 0
                ? (sort[position] + sort[position + 1]) / 2
                : sort[position];

            return median;
        }
    }
}
