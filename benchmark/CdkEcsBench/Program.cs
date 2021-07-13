using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Cdk
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new CdkStack(app, "MagicOnionBenchmarkCdkStack", new ReportStackProps
            {
                BenchmarkEndpoint = BenchmarkEndpoint.ServiceDiscoveryWithHttp,
                AlbDomain = ("dev.cysharp.io", "Z075519318R3LY1VXMWII"),
                ForceRecreateMagicOnion = false,
                EnableMagicOnionScaleInCron = true,
                AutomaticScaleInAfterHour = 1,
                UseEc2CloudWatchAgentProfiler = true,
                UseEc2DatadogAgentProfiler = false,
                UseFargateDatadogAgentProfiler = true,
                MagicOnionInstanceType = InstanceType.Of(InstanceClass.COMPUTE5_AMD, InstanceSize.LARGE),
                MasterFargate = new Fargate(Fargate.CpuSpec.Half, Fargate.MemorySpec.Low),
                WorkerFargate = new Fargate(Fargate.CpuSpec.Double, Fargate.MemorySpec.Low),
                Tags = new Dictionary<string, string>()
                {
                    { "environment", "bench" },
                    { "cf-stack", "MagicOnionBenchmarkCdkStack" },
                },
            });
            app.Synth();
        }
    }

    public class ReportStackProps : StackProps
    {
        /// <summary>
        /// Benchmark BinaryNames
        /// </summary>
        public string[] BenchmarkBinaryNames { get; set; } = new[] { "Benchmark.Server", "Benchmark.Server.Https", "Benchmark.Server.Api" };
        /// <summary>
        /// MagicOnion Endpoint to accept Worker request
        /// </summary>
        public BenchmarkEndpoint BenchmarkEndpoint { get; set; } = BenchmarkEndpoint.ServiceDiscoveryWithHttp;
        /// <summary>
        /// Register AlbDomain on AlbMode
        /// </summary>
        public (string domain, string zoneId) AlbDomain { get; set; }
        /// <summary>
        /// Enable ScaleIn on 0:00:00 (UTC)
        /// </summary>
        public bool EnableMagicOnionScaleInCron { get; set; }
        /// <summary>
        /// Automatically scale in after specified hour. 0 to not scale in.
        /// </summary>
        public int AutomaticScaleInAfterHour { get; set; } = 0;
        /// <summary>
        /// Flag to force recreate MagicOnion Instance.
        /// </summary>
        public bool ForceRecreateMagicOnion { get; set; }
        /// <summary>
        /// ReportId
        /// </summary>
        public string ReportId { get; set; }
        /// <summary>
        /// Number of days to keep reports in S3 bucket.
        /// </summary>
        public int DaysKeepReports { get; set; } = 3;
        /// <summary>
        /// Install CloudWatch Agent to EC2 MagicOnion and get MemoryUsage / TCP Established metrics.
        /// </summary>
        public bool UseEc2CloudWatchAgentProfiler { get; set; }
        /// <summary>
        /// Install Datadog Agent to EC2 MagicOnion.
        /// </summary>
        public bool UseEc2DatadogAgentProfiler { get; set; }
        /// <summary>
        /// Install Datadog Agent as Fargate sidecar container.
        /// </summary>
        public bool UseFargateDatadogAgentProfiler { get; set; }
        /// <summary>
        /// MagicOnion InstanceType
        /// </summary>
        public InstanceType MagicOnionInstanceType { get; set; }
        /// <summary>
        /// Fargate of Dframe master 
        /// </summary>
        public Fargate MasterFargate { get; set; }
        /// <summary>
        /// Fargate of Dframe worker
        /// </summary>
        public Fargate WorkerFargate { get; set; }

        public ReportStackProps()
        {
            var now = DateTime.Now;
            ReportId = $"{now.ToString("yyyyMMdd-HHmmss")}-{Guid.NewGuid().ToString()}";
        }

        public BenchNetwork GetBenchNetwork()
        {
            return new BenchNetwork(BenchmarkEndpoint);
        }

        public string GetBenchmarkServerBinariesHash()
        {
            var hash = string.Join(",", BenchmarkBinaryNames.Select(x => GetBenchmarkServerBinaryHash(x)));
            if (ForceRecreateMagicOnion )
            {
                hash += "-" + Guid.NewGuid().ToString();
            }
            return hash;
        }

        private string GetBenchmarkServerBinaryHash(string binaryName)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "out/linux/server", binaryName, binaryName);
            var bytes = File.ReadAllBytes(path);
            using var provider = new SHA256CryptoServiceProvider();
            var hash = provider.ComputeHash(bytes);
            return string.Join("", hash.Select(x => $"{x:x2}"));
        }

        public static ReportStackProps ParseOrDefault(IStackProps props, ReportStackProps @default = null)
        {
            if (props is ReportStackProps r)
            {
                return r;
            }
            else
            {
                return @default != null ? @default : new ReportStackProps();
            }
        }
    }

    public enum BenchmarkEndpoint
    {
        /// <summary>
        /// Worker to Insecure MagicOnion with ServiceDiscovery, direct machine to machine over Insecure HTTP/2 (or HTTP for REST)
        /// </summary>
        ServiceDiscoveryWithHttp = 0,
        /// <summary>
        /// Worker to HTTPS MagicOnion with ServiceDiscovery, direct machine to machine over HTTPS
        /// </summary>
        ServiceDiscoveryWithHttps,
        /// <summary>
        /// Worker to HTTP MagicOnion with ALB (HTTPS), machine to alb over TLS.
        /// </summary>
        Alb,
    }

    public class BenchNetwork
    {
        /// <summary>
        /// Bench worker to target Https MagicOnion Endpoint Http Scheme
        /// </summary>
        public string EndpointScheme { get; }
        /// <summary>
        /// Indicate Alb is required.
        /// </summary>
        public bool RequireAlb { get; }
        /// <summary>
        /// Grpc Alb Ports
        /// </summary>
        public (int listenerPort, int targetgroupPort) AlbGrpcPort { get; } = (443, 80);
        /// <summary>
        /// Https Alb Ports
        /// </summary>
        public (int listenerPort, int targetgroupPort) AlbHttpsPort { get; } = (5001, 5000);

        public BenchNetwork(BenchmarkEndpoint endpoint)
        {
            switch (endpoint)
            {
                case BenchmarkEndpoint.ServiceDiscoveryWithHttp:
                    RequireAlb = false;
                    EndpointScheme = "http";
                    break;
                case BenchmarkEndpoint.ServiceDiscoveryWithHttps:
                    RequireAlb = false;
                    EndpointScheme = "https";
                    break;
                case BenchmarkEndpoint.Alb:
                    RequireAlb = true;
                    EndpointScheme = "https";
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// FargateSpec follow to the rule.
    /// https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task-cpu-memory-error.html
    /// </summary>
    public class Fargate
    {
        /// <summary>
        /// Cpu Size to use
        /// </summary>
        public int CpuSize => _cpuSize;
        private int _cpuSize;
        /// <summary>
        /// Memory Size to use
        /// </summary>
        public int MemorySize => _memorysize;
        private int _memorysize;

        public Fargate() : this(CpuSpec.Quater, MemorySpec.Low)
        {
        }
        public Fargate(CpuSpec cpu, MemorySpec memory)
        {
            _cpuSize = (int)cpu;
            _memorysize = CalculateMemorySize(cpu, memory);
        }
        public Fargate(CpuSpec cpu, int memorySize)
        {
            _cpuSize = (int)cpu;
            _memorysize = CalculateMemorySize(cpu, memorySize);
        }

        /// <summary>
        /// Memory Calculation for Typical MemorySize
        /// </summary>
        /// <param name="cpu"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        private int CalculateMemorySize(CpuSpec cpu, MemorySpec memory) => (int)cpu * (int)memory;
        /// <summary>
        /// Memory Calculation for Custom MemorySize
        /// </summary>
        /// <param name="memorySize"></param>
        /// <returns></returns>
        private int CalculateMemorySize(CpuSpec cpu, int memorySize)
        {
            switch (cpu)
            {
                case CpuSpec.Quater:
                case CpuSpec.Half:
                case CpuSpec.Single:
                    throw new ArgumentOutOfRangeException($"You must select CpuSpec of Double or Quadruple.");
                case CpuSpec.Double:
                    {
                        // 4096 < n < 16384, n can be increments of 1024
                        if (memorySize % 1024 != 0)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} must be increments of 1024.");
                        if (memorySize < _cpuSize * 2)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too low, must be larger than {_cpuSize * 2}");
                        if (memorySize > _cpuSize * 4)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too large, must be lower than {_cpuSize * 4}");
                    }
                    break;
                case CpuSpec.Quadruple:
                    {
                        // 8192 < n < 30720, n can be increments of 1024
                        if (memorySize % 1024 != 0)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} must be increments of 1024.");
                        if (memorySize < _cpuSize * 2)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too low, must be larger than {_cpuSize * 2}");
                        if (memorySize > _cpuSize * 7.5)
                            throw new ArgumentOutOfRangeException($"{nameof(memorySize)} too large, must be lower than {_cpuSize * 7.5}");
                    }
                    break;
            }
            return memorySize;
        }

        /// <summary>
        /// Fargate Cpu Spec. default 256 = 0.25
        /// </summary>
        public enum CpuSpec
        {
            Quater = 256,
            Half = 512,
            Single = 1024,
            Double = 2048,
            Quadruple = 4096,
        }

        /// <summary>
        /// Fargate Memory Spec. default 512 = 0.5GB
        /// </summary>
        public enum MemorySpec
        {
            /// <summary>
            /// Only available when <see cref="CpuSpec"/> is Double or Quadruple.
            /// </summary>
            Custom = 0,
            Low = 2,
            Medium = 4,
            High = 8,
        }
    }
}
