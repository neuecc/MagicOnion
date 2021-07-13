using Benchmark.ClientLib.Reports;
using Benchmark.ClientLib.Internal.Runtime;
using Benchmark.Server;
using Grpc.Net.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Benchmark.ClientLib.Scenarios
{
    public class GrpcBenchmarkScenario
    {
        private readonly Greeter.GreeterClient[] _clients;
        private readonly BenchReporter _reporter;
        private readonly BenchmarkerConfig _config;

        public GrpcBenchmarkScenario(GrpcChannel[] channels, BenchReporter reporter, BenchmarkerConfig config)
        {
            _clients = channels.Select(x => new Greeter.GreeterClient(x)).ToArray();
            _reporter = reporter;
            _config = config;
        }

        private Greeter.GreeterClient GetClient(int n) => _clients[n % _clients.Length];

        public async Task Run(int requestCount, CancellationToken ct)
        {
            Statistics statistics = null;
            CallResult[] results = null;
            using (statistics = new Statistics(nameof(UnaryBenchmarkScenario) + requestCount))
            {
                results = await SayHelloAsync(requestCount, ct);
            }

            _reporter.AddDetail(nameof(SayHelloAsync), nameof(MethodType.Unary), _reporter, statistics, results);
        }

        /// <summary>
        /// Concurrent Run
        /// </summary>
        /// <param name="requestCount"></param>
        /// <param name="ct"></param>
        /// <param name="reportAction"></param>
        /// <returns></returns>
        private async Task<CallResult[]> SayHelloAsync(int requestCount, CancellationToken ct)
        {
            var data = new HelloRequest { Name = _config.GetRequestPayload() };

            var duration = _config.GetDuration();
            if (duration != TimeSpan.Zero)
            {
                // duration base
                using var cts = new CancellationTokenSource(duration);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
                var linkedCt = linkedCts.Token;

                using var pool = new AsyncUnaryCallWorkerPool<HelloRequest, HelloReply>(_config.ClientConcurrency, linkedCt);
                pool.RunWorkers((id, data, ct) => GetClient(id).SayHelloAsync(data, cancellationToken: ct), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
            else
            {
                // request base
                using var pool = new AsyncUnaryCallWorkerPool<HelloRequest, HelloReply>(_config.ClientConcurrency, ct)
                {
                    CompleteCondition = x => x.completed >= requestCount,
                };
                pool.RunWorkers((id, data, ct) => GetClient(id).SayHelloAsync(data, cancellationToken: ct), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
        }
    }
}
