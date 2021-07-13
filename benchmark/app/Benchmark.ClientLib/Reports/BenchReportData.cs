using Benchmark.ClientLib.Internal;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Benchmark.ClientLib.Reports
{
    public record BenchReport
    {
        [JsonPropertyName("report_id")]
        public string ReportId { get; set; }
        [JsonPropertyName("execute_id")]
        public string ExecuteId { get; set; }
        /// <summary>
        /// Client Identifier for Same Machine Execution but treat as different client.
        /// </summary>
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }
        [JsonPropertyName("host_name")]
        public string HostName { get; set; }
        [JsonPropertyName("os")]
        public string OS { get; set; }
        [JsonPropertyName("os_architecture")]
        public string OsArchitecture { get; set; }
        [JsonPropertyName("process_architecture")]
        public string ProcessArchitecture { get; set; }
        [JsonPropertyName("cpu_number")]
        public int CpuNumber { get; set; }
        [JsonPropertyName("system_memory")]
        public long SystemMemory { get; set; }
        [JsonPropertyName("framework")]
        public string Framework { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("begin")]
        public DateTime Begin { get; set; }
        [JsonPropertyName("end")]
        public DateTime End { get; set; }
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        [JsonPropertyName("scenario_name")]
        public string ScenarioName { get; set; }
        [JsonPropertyName("concurrency")]
        public int Concurrency { get; set; }
        [JsonPropertyName("connections")]
        public int Connections { get; set; }
        [JsonPropertyName("items")]
        public BenchReportItem[] Items { get; set; }
    }

    public record BenchReportItem
    {
        [JsonPropertyName("execute_id")]
        public string ExecuteId { get; set; }
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("test_name")]
        public string TestName { get; set; }
        [JsonPropertyName("begin")]
        public DateTime Begin { get; set; }
        [JsonPropertyName("end")]
        public DateTime End { get; set; }
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        [JsonPropertyName("request_count")]
        public int RequestCount { get; set; }
        [JsonPropertyName("slowest")]
        public TimeSpan Slowest { get; set; }
        [JsonPropertyName("fastest")]
        public TimeSpan Fastest { get; set; }
        [JsonPropertyName("average")]
        public TimeSpan Average { get; set; }
        [JsonPropertyName("request_per_sec")]
        public Double Rps { get; set; }
        [JsonPropertyName("error_count")]
        public int Errors { get; set; }
        [JsonPropertyName("stddev")]
        public StandardDeviation StandardDeviation { get; set; }
        [JsonPropertyName("statuscodes")]
        public StatusCodeDistribution[] StatusCodeDistributions { get; set; }
        [JsonPropertyName("errorcodes")]
        public ErrorCodeDistribution[] ErrorCodeDistribution { get; set; }
        [JsonPropertyName("latencies")]
        public LatencyDistribution[] Latencies { get; set; }
        [JsonPropertyName("histogram")]
        public HistogramBucket[] Histogram { get; set; }
    }

    public struct StatusCodeDistribution
    {
        [JsonPropertyName("statuscode")]
        public string StatusCode { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }

        public static StatusCodeDistribution[] FromCallResults(IEnumerable<CallResult> callResults)
        {
            return callResults.Select(x => x.Status)
                .GroupBy(x => x.StatusCode)
                .Select(x => new StatusCodeDistribution
                {
                    Count = x.Count(),
                    StatusCode = x.Key.ToString(),
                })
                .ToArray();
        }
    }

    public struct ErrorCodeDistribution
    {
        [JsonPropertyName("statuscode")]
        public string StatusCode { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("detail")]
        public string Detail { get; set; }

        public static ErrorCodeDistribution[] FromCallResults(IEnumerable<CallResult> callResults)
        {
            return callResults
                .Where(x => x.Status.StatusCode != Grpc.Core.StatusCode.OK)
                .GroupBy(x => x.Error?.Message)
                .Select(x => new ErrorCodeDistribution
                {
                    Count = x.Count(),
                    StatusCode = x.Select(x => x.Status.StatusCode).First().ToString(),
                    Detail = x.Key,
                })
                .ToArray()
                ?? Array.Empty<ErrorCodeDistribution>();
        }
    }

    public struct CallResult
    {
        [JsonPropertyName("error")]
        public Exception Error { get; set; }
        [JsonPropertyName("status")]
        public Status Status { get; set; }
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }
    }

    public struct StandardDeviation
    {
        [JsonPropertyName("stddev")]
        public double StdDev { get; set; }
        [JsonPropertyName("stderr")]
        public double StdErr { get; set; }

        public static StandardDeviation Calculate(TimeSpan[] latencies, TimeSpan mean)
        {
            var variance = Variance(latencies, mean);
            var standardDeviation = Math.Sqrt(variance);
            var standardError = latencies.Length != 0
                ? standardDeviation / Math.Sqrt(latencies.Length - 1)
                : 0;
            return new StandardDeviation()
            {
                StdDev = standardDeviation,
                StdErr = standardError,
            };
        }

        private static double Variance(TimeSpan[] latencies, TimeSpan mean)
        {
            if (latencies.Length == 0 || latencies.Length == 1)
                return 0;

            double variance = 0;
            foreach (var l in latencies)
            {
                variance += (l.TotalMilliseconds - mean.TotalMilliseconds) * (l.TotalMilliseconds - mean.TotalMilliseconds) / (latencies.Length - 1);
            }
            return variance;
        }
    }

    public struct LatencyDistribution
    {
        private static readonly int[] percentiles = new int[] { 10, 25, 50, 75, 90, 95, 99 };

        [JsonPropertyName("percentile")]
        public int Percentile { get; set; }
        [JsonPropertyName("latency")]
        public TimeSpan Latency { get; set; }

        public static LatencyDistribution[] Calculate(TimeSpan[] latencies)
        {
            if (!latencies.Any())
                return Array.Empty<LatencyDistribution>();

            var data = new double[percentiles.Length];
            var length = latencies.Length;

            // get percentile latency value
            for (var i = 0; i < percentiles.Length; i++)
            {
                var percentile = percentiles[i];
                var ip = (percentile / 100.0) * length;
                var di = (int)ip;

                // since we're dealing with 0th based ranks we need to
                // check if ordinal is a whole number that lands on the percentile
                // if so adjust accordingly
                if (ip == (double)di)
                    di = di - 1;

                if (di < 0)
                {
                    di = 0;
                }
                data[i] = latencies[di].TotalMilliseconds;
            }

            var res = new LatencyDistribution[percentiles.Length];
            for (var i = 0; i < percentiles.Length; i++)
            {
                if (data[i] > 0)
                {
                    res[i] = new LatencyDistribution
                    {
                        Percentile = percentiles[i],
                        Latency = TimeSpan.FromMilliseconds(data[i]),
                    };
                }
            }
            return res;
        }
    }

    public struct HistogramBucket
    {
        [JsonPropertyName("mark")]
        public double Mark { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("frequency")]
        public double Frequency { get; set; }

        public static HistogramBucket[] Calculate(TimeSpan[] latencies, double slowest, double fastest)
        {
            var bc = 10;
            var buckets = new double[bc + 1];
            var counts = new int[bc + 1];
            var bs = (slowest - fastest);
            for (var i = 0; i < bc; i++)
            {
                // ghz is using linear, but it's mark interval is too wide for Benchmark.
                // let's change interval to shorter in, longer out.
                //buckets[i] = Easing.Linear(i, fastest, bs, bc); // ghz
                //buckets[i] = Easing.InQuadratic(i, fastest, bs, bc);
                //buckets[i] = Easing.InCubic(i, fastest, bs, bc);
                //buckets[i] = Easing.InQuintic(i, fastest, bs, bc);
                //buckets[i] = Easing.InSinusoidal(i, fastest, bs, bc);
                //buckets[i] = Easing.InExponential(i, fastest, bs, bc);
                buckets[i] = Easing.InCircular(i, fastest, bs, bc);
            }
            buckets[bc] = slowest;
            int bi = 0;
            int max = 0;
            for (var i = 0; i < latencies.Length;)
            {
                if (latencies[i].TotalMilliseconds <= buckets[bi])
                {
                    i++;
                    counts[bi]++;
                    if (max < counts[bi])
                    {
                        max = counts[bi];
                    }
                }
                else if (bi < buckets.Length - 1)
                {
                    bi++;
                }
            }
            var res = new HistogramBucket[buckets.Length];
            for (var i = 0; i < buckets.Length; i++)
            {
                res[i] = new HistogramBucket
                {
                    Mark = buckets[i],
                    Count = counts[i],
                    Frequency = counts[i] != 0 ? (double)counts[i] / latencies.Length : 0,
                };
            }
            return res;
        }
    }
}
