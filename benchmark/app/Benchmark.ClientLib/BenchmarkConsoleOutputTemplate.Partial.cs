using Benchmark.ClientLib.Reports;
using System;
using System.Linq;

namespace Benchmark.ClientLib
{
    public partial class BenchmarkConsoleOutputTemplate
    {
        private const char BarChar = '∎';

        public string Name { get; }
        public int Count { get; }
        public double Total { get; }
        public double Slowest { get; }
        public double Fastest { get; }
        public double Average { get; }
        public double StdErr { get; }
        public double StdDev { get; }
        public double Rps { get; }
        public string[] FormattedHistograms { get; }
        public LatencyDistribution[] Latencies { get; }
        public string[] FormattedStatusCodeDistributions { get; } = Array.Empty<string>();
        public string[] FormattedErrorCodeDistributions { get; } = Array.Empty<string>();

        public BenchmarkConsoleOutputTemplate(BenchReport report)
        {
            Name = report.ScenarioName;
            Count = report.Items.Sum(x => x.RequestCount);
            Total = report.Duration.TotalSeconds;
            Slowest = report.Items.Average(x => x.Slowest.TotalMilliseconds);
            Fastest = report.Items.Average(x => x.Fastest.TotalMilliseconds);
            Average = report.Items.Average(x => x.Average.TotalMilliseconds);
            StdDev = report.Items.Average(x => x.StandardDeviation.StdDev);
            StdErr = report.Items.Average(x => x.StandardDeviation.StdErr);
            Rps = report.Items.Average(x => x.Rps);
            FormattedHistograms = report.Items.SelectMany(x => x.Histogram)
                .GroupBy(x => x.Mark)
                .Select(x => new HistogramBucket
                {
                    Count = (int)x.Average(y => y.Count),
                    Mark = x.Key,
                    Frequency = x.Average(y => y.Frequency),
                })
                .OrderBy(x => x.Mark)
                .Select(x =>
                {
                    return $"";
                })
                .ToArray();
            Latencies = report.Items.SelectMany(x => x.Latencies)
                .GroupBy(x => x.Percentile)
                .Select(x => new LatencyDistribution
                {
                    Percentile = x.Key,
                    Latency = x.Select(y => y.Latency).Average(),
                })
                .ToArray();

            var histograms = report.Items.SelectMany(x => x.Histogram)
                .GroupBy(x => x.Mark)
                .Select(x => new HistogramBucket
                {
                    Mark = x.Key,
                    Count = x.Sum(y => y.Count),
                    Frequency = x.Average(y => y.Frequency),
                })
                .OrderBy(x => x.Mark)
                .ToArray();
            var maxHistogramMark = histograms.Max(x => FormatMs(x.Mark).Length);
            var maxHistogramCount = histograms.Max(x => x.Count);
            FormattedHistograms = histograms.Select(x =>
                {
                    var barLength = 0;
                    // Normalize
                    if (maxHistogramCount > 0)
                        barLength = (x.Count * 40 + maxHistogramCount / 2) / maxHistogramCount;

                    var markLength = maxHistogramMark - FormatMs(x.Mark).Length;
                    var countLength = maxHistogramCount.ToString().Length - x.Count.ToString().Length;
                    return $"{FormatMs(x.Mark)}{AddSpace(markLength)} [{x.Count}]{AddSpace(countLength)} |{new string(BarChar, barLength)}";
                })
                .ToArray();

            if (report.Items.SelectMany(x => x.StatusCodeDistributions).Any())
            {
                var statusCodeMax = report.Items.SelectMany(x => x.StatusCodeDistributions).Max(x => x.StatusCode.Length);
                FormattedStatusCodeDistributions = report.Items.SelectMany(x => x.StatusCodeDistributions)
                    .GroupBy(x => x.StatusCode)
                    .Select(x => new StatusCodeDistribution
                    {
                        StatusCode = x.Key,
                        Count = x.Sum(y => y.Count),
                    })
                    .OrderByDescending(x => x.Count)
                    .Select(x =>
                    {
                        var length = statusCodeMax - x.StatusCode.Length;
                        return $"[{x.StatusCode}]{AddSpace(length)}\t{x.Count} responses";
                    })
                    .ToArray();
            }

            if (report.Items.SelectMany(x => x.ErrorCodeDistribution).Any())
            {
                var errorCountMax = report.Items.SelectMany(x => x.ErrorCodeDistribution).Max(x => x.Count.ToString().Length);
                FormattedErrorCodeDistributions = report.Items.SelectMany(x => x.ErrorCodeDistribution)
                    .GroupBy(x => x.StatusCode)
                    .Select(x => new ErrorCodeDistribution
                    {
                        StatusCode = x.Key,
                        Count = x.Sum(y => y.Count),
                        Detail = x.Select(y => y.Detail).FirstOrDefault(y => !string.IsNullOrEmpty(y)),
                    })
                    .OrderByDescending(x => x.Count)
                    .Select(x =>
                    {
                        var length = errorCountMax - x.Count.ToString().Length;
                        return $"[{x.Count}]{AddSpace(length)}\t{x.Detail}\t";
                    })
                    .ToArray();
            }
        }

        public string FormatMs(double ms)
        {
            return ms < 1
                ? string.Format("{0:f4}", ms) 
                : string.Format("{0:f2}", ms);
        }

        private string AddSpace(int length) => string.Empty.PadRight(length, ' ');
    }
}
