using Benchmark.ClientLib.Reports;
using Benchmark.ClientLib.Internal.Runtime;
using Benchmark.Server.Shared;
using Benchmark.Shared;
using Grpc.Net.Client;
using MagicOnion.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Benchmark.ClientLib.Scenarios
{
    public class UnaryBenchmarkScenario
    {
        private readonly IBenchmarkService[] _clients;
        private readonly BenchReporter _reporter;
        private readonly BenchmarkerConfig _config;

        public UnaryBenchmarkScenario(GrpcChannel[] channels, BenchReporter reporter, BenchmarkerConfig config)
        {
            _clients = channels.Select(x => MagicOnionClient.Create<IBenchmarkService>(x)).ToArray();
            _reporter = reporter;
            _config = config;
        }

        private IBenchmarkService GetClient(int n) => _clients[n % _clients.Length];

        public async Task Run(int requestCount, CancellationToken ct)
        {
            Statistics statistics = null;
            CallResult[] results = null;
            using (statistics = new Statistics(nameof(UnaryBenchmarkScenario) + requestCount))
            {
                results = await PlainTextAsync(requestCount, ct);
            }

            _reporter.AddDetail(nameof(PlainTextAsync), nameof(MethodType.Unary), _reporter, statistics, results);
        }

        private async Task<CallResult[]> PlainTextAsync(int requestCount, CancellationToken ct)
        {
            var data = new BenchmarkRequest { Name = _config.GetRequestPayload() };

            var duration = _config.GetDuration();
            if (duration != TimeSpan.Zero)
            {
                // timeout base
                using var cts = new CancellationTokenSource(duration);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
                var linkedCt = linkedCts.Token;

                using var pool = new UnaryResultWorkerPool<BenchmarkRequest, BenchmarkReply>(_config.ClientConcurrency, linkedCt);
                pool.RunWorkers((id, data, ct) => GetClient(id).SayHelloAsync(data), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
            else
            {
                // request base
                using var pool = new UnaryResultWorkerPool<BenchmarkRequest, BenchmarkReply>(_config.ClientConcurrency, ct)
                {
                    CompleteCondition = x => x.completed >= requestCount,
                };
                pool.RunWorkers((id, data, ct) => GetClient(id).SayHelloAsync(data), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
        }
    }
}
