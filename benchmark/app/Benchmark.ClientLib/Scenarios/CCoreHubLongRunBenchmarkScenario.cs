using Benchmark.ClientLib.Reports;
using Benchmark.ClientLib.Internal.Runtime;
using Benchmark.Server.Shared;
using Grpc.Core;
using MagicOnion.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.ClientLib.Scenarios
{
    public class CCoreHubLongRunBenchmarkScenario : ILongRunBenchmarkHubReciever, IAsyncDisposable
    {
        private readonly BenchReporter _reporter;
        private readonly BenchmarkerConfig _config;
        private ILongRunBenchmarkHub[] _clients;

        public CCoreHubLongRunBenchmarkScenario(Channel[] channels, BenchReporter reporter, BenchmarkerConfig config)
        {
            _clients = channels.Select(x => StreamingHubClient.ConnectAsync<ILongRunBenchmarkHub, ILongRunBenchmarkHubReciever>(new DefaultCallInvoker(x), this).GetAwaiter().GetResult()).ToArray();
            _reporter = reporter;
            _config = config;
        }

        private ILongRunBenchmarkHub GetClient(int n) => _clients[n % _clients.Length];

        public async Task Run(int requestCount, int waitMilliseconds, CancellationToken ct)
        {
            Statistics statistics = null;
            CallResult[] results = null;
            using (statistics = new Statistics(nameof(UnaryBenchmarkScenario) + requestCount))
            {
                results = await ProcessAsync(requestCount, waitMilliseconds, ct);
            }

            _reporter.AddDetail(nameof(ProcessAsync), nameof(MethodType.DuplexStreaming), _reporter, statistics, results);
        }

        private async Task<CallResult[]> ProcessAsync(int requestCount, int waitMilliseonds, CancellationToken ct)
        {
            var data = new LongRunBenchmarkData
            {
                WaitMilliseconds = waitMilliseonds,
            };
            var duration = _config.GetDuration();
            if (duration != TimeSpan.Zero)
            {
                // timeout base
                using var cts = new CancellationTokenSource(duration);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
                var linkedCt = linkedCts.Token;

                using var pool = new TaskWorkerPool<LongRunBenchmarkData>(_config.ClientConcurrency, linkedCt);
                pool.RunWorkers((id, data, ct) => GetClient(id).Process(data), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
            else
            {
                // request base
                using var pool = new TaskWorkerPool<LongRunBenchmarkData>(_config.ClientConcurrency, ct)
                {
                    CompleteCondition = x => x.completed >= requestCount,
                };
                pool.RunWorkers((id, data, ct) => GetClient(id).Process(data), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await Task.WhenAll(_clients.Select(x => x.DisposeAsync()));
        }

        void ILongRunBenchmarkHubReciever.OnProcess()
        {
            throw new NotImplementedException();
        }
    }
}
