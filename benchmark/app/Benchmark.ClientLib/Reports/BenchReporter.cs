using Benchmark.ClientLib.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Benchmark.ClientLib.Reports
{
    public enum Framework
    {
        MagicOnion,
        GrpcDotnet,
        GrpcCcore,
        AspnetCore,
    }
    public class BenchReporter
    {
        private readonly BenchReport _report;
        private readonly List<BenchReportItem> _items;

        public string ReportId { get; }
        public string ClientId { get; }
        public string ExecuteId { get; }
        public BenchReport Report => _report;

        public BenchReporter(string reportId, string clientId, string executeId, Framework framework, string scenarioName, BenchmarkerConfig config)
        {
            ReportId = reportId;
            ClientId = clientId;
            ExecuteId = executeId;

            _report = new BenchReport
            {
                ReportId = ReportId,
                ClientId = ClientId,
                ExecuteId = ExecuteId,
                HostName = Dns.GetHostName(),
                OS = ShortOs(RuntimeInformation.OSDescription),
                OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                CpuNumber = Environment.ProcessorCount,
                SystemMemory = GetSystemMemory(),
                Framework = framework.ToString(),
                Version = GetFrameworkVersion(framework),
                ScenarioName = scenarioName,
                Concurrency = config.ClientConcurrency,
                Connections = config.ClientConnections,
            };
            _items = new List<BenchReportItem>();

            _report.Begin = DateTime.UtcNow;
        }

        private string GetFrameworkVersion(Framework framework)
        {
            if (framework == Framework.AspnetCore)
                return "5.0.0";

            var type = framework switch
            {
                Framework.MagicOnion => typeof(MagicOnion.IServiceMarker),
                Framework.GrpcDotnet => typeof(Grpc.Net.Client.GrpcChannel),
                Framework.GrpcCcore => typeof(Grpc.Core.Channel),
                _ => throw new NotImplementedException(),
            };
            var version = type.Assembly.GetName().Version?.ToString();
            return version;
        }

        /// <summary>
        /// Begin Reporter
        /// </summary>
        public void Begin()
        {
            _report.Begin = DateTime.UtcNow;
        }

        /// <summary>
        /// End Reporter and Finalize
        /// </summary>
        public void End()
        {
            _report.End = DateTime.UtcNow;
            _report.Duration = _report.End - _report.Begin;
            _report.Items = _items.ToArray();
        }

        /// <summary>
        /// Add Detail Report
        /// </summary>
        /// <param name="item"></param>
        public void AddDetail(BenchReportItem item)
        {
            _items.Add(item);
        }
        /// <summary>
        /// Add Detail Report
        /// </summary>
        /// <param name="testName"></param>
        /// <param name="testType"></param>
        /// <param name="reporter"></param>
        /// <param name="statistics"></param>
        /// <param name="results"></param>
        public void AddDetail(string testName, string testType, BenchReporter reporter, Statistics statistics, CallResult[] results)
        {
            TimeSpan[] sortedResults = Array.Empty<TimeSpan>();
            var (fastest, slowest, average) = (TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
            if (results.Any())
            {
                sortedResults = results.Select(x => x.Duration).OrderBy(x => x).ToArray();
                fastest = sortedResults[0];
                slowest = sortedResults[^1];
                average = results.Select(x => x.Duration).Average();
            }
            var item = new BenchReportItem
            {
                ExecuteId = reporter.ExecuteId,
                ClientId = reporter.ClientId,
                TestName = testName,
                Begin = statistics.Begin,
                End = DateTime.UtcNow,
                Duration = statistics.Elapsed,
                RequestCount = results.Length,
                Type = testType,
                Average = average,
                Fastest = fastest,
                Slowest = slowest,
                Rps = results.Length / statistics.Elapsed.TotalSeconds,
                Errors = results.Where(x => x.Error != null).Count(),
                StandardDeviation = StandardDeviation.Calculate(sortedResults, average),
                StatusCodeDistributions = StatusCodeDistribution.FromCallResults(results),
                ErrorCodeDistribution = ErrorCodeDistribution.FromCallResults(results),
                Latencies = LatencyDistribution.Calculate(sortedResults),
                Histogram = HistogramBucket.Calculate(sortedResults, slowest.TotalMilliseconds, fastest.TotalMilliseconds),
            };
            _items.Add(item);
        }

        /// <summary>
        /// Output report as a json
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.Serialize(_report);
        }

        public string GetJsonFileName()
        {
            return ClientId + "-" + ExecuteId + ".json";
        }

        /// <summary>
        /// System Memory for GB
        /// </summary>
        /// <returns></returns>
        private static long GetSystemMemory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows require wmic and priviledged. lets omit
                return 0;
            }
            else
            {
                if (File.Exists("/proc/meminfo"))
                {
                    // sample data (64GB machine)
                    // MemTotal:        6110716 kB
                    var memTotal = System.IO.File.ReadAllLines("/proc/meminfo").FirstOrDefault();
                    var lines = memTotal.Split("MemTotal:");
                    if (lines.Length == 2)
                    {
                        var memString = lines[1].Split("kB")[0];
                        if (double.TryParse(memString.Trim(), out var memory))
                        {
                            return (long)Math.Floor(memory / 1024 / 1024);
                        }
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// Shorten OS Description to enough length. Remove Time information
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        private string ShortOs(string description)
        {
            if (description.Contains("SMP"))
            {
                // Gentoo	Linux 4.19.104-gentoo #1 SMP Wed Feb 19 06:37:35 UTC 2020
                // Ubuntu 18.04	Linux 4.15.0-106-generic #107-Ubuntu SMP Thu Jun 4 11:27:52 UTC 2020
                // AmazonLinux2 Linux 4.14.209-160.339.amzn2.x86_64 #1 SMP Wed Dec 16 22:44:04 UTC 2020 (Linux 4.14.209-160.339.amzn2.x86_64 #1 SMP Wed Dec 16 22:44:04 UTC 2020)
                return description.Split("SMP").First();
            }
            else if (description.Contains("#"))
            {
                // FreeBSD 11	FreeBSD 11.0-RELEASE-p1 FreeBSD 11.0-RELEASE-p1 #0 r306420: Thu Sep 29 01:43:23 UTC 2016 root@releng2.nyi.freebsd.org:/usr/obj/usr/src/sys/GENERIC
                return description.Split("#").First();
            }
            else if (description.Contains(": "))
            {
                // macOS 10.14.6	Darwin 18.7.0 Darwin Kernel Version 18.7.0: Mon Feb 10 21:08:05 PST 2020; root:xnu-4903.278.28~1/RELEASE_X86_64
                return description.Split(": ").First();
            }
            else
            {
                // SmartOS 2020	SunOS 5.11 joyent_20200408T231825Z
                // Solaris 11.3	SunOS 5.11 11.3
                // Windows 10	Microsoft Windows 10.0.19635
                return description;
            }
        }
    }
}
