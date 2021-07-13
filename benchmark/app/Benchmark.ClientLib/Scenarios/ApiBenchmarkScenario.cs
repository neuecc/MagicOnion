using Benchmark.ClientLib.Internal.Runtime;
using Benchmark.ClientLib.Reports;
using Benchmark.Server.Shared;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.ClientLib.Scenarios
{
    public class ApiBenchmarkScenario
    {
        private readonly ApiClient[] _clients;
        private readonly BenchReporter _reporter;
        private readonly BenchmarkerConfig _config;

        public ApiBenchmarkScenario(ApiClient[] clients, BenchReporter reporter, BenchmarkerConfig config)
        {
            _clients = clients;
            _reporter = reporter;
            _config = config;
        }

        private ApiClient GetClient(int n) => _clients[n % _clients.Length];

        public async Task Run(int requestCount, CancellationToken ct)
        {
            Statistics statistics = null;
            CallResult[] results = Array.Empty<CallResult>();
            using (statistics = new Statistics(nameof(UnaryBenchmarkScenario) + requestCount))
            {
                results = await PlainTextAsync(requestCount, ct);
            }

            _reporter.AddDetail(nameof(PlainTextAsync), "REST", _reporter, statistics, results);
        }

        private async Task<CallResult[]> PlainTextAsync(int requestCount, CancellationToken ct)
        {
            var data = new BenchmarkRequest
            {
                Name = _config.GetRequestPayload(),
            };

            var duration = _config.GetDuration();
            if (duration != TimeSpan.Zero)
            {
                // timeout base
                using var cts = new CancellationTokenSource(duration);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct);
                var linkedCt = linkedCts.Token;

                using var pool = new TaskWorkerPool<BenchmarkRequest, BenchmarkReply>(_config.ClientConcurrency, linkedCt);
                pool.RunWorkers((id, data, ct) => GetClient(id).SayHelloAsync(data), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
            else
            {
                // request base
                using var pool = new TaskWorkerPool<BenchmarkRequest, BenchmarkReply>(_config.ClientConcurrency, ct)
                {
                    CompleteCondition = x => x.completed >= requestCount,
                };
                pool.RunWorkers((id, data, ct) => GetClient(id).SayHelloAsync(data), data, ct);
                await Task.WhenAny(pool.WaitForCompleteAsync(), pool.WaitForTimeout());
                return pool.GetResult();
            }
        }

        /// <summary>
        /// ThreadSafe client
        /// </summary>
        public class ApiClient
        {
            private readonly HttpClient _client;
            private readonly string _endpointPlainText;

            public ApiClient(string endpoint, bool useHttp2)
            {
                if (useHttp2)
                {
                    var clientHandler = new HttpClientHandler();
                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    _client = new HttpClient(clientHandler);
                    _client.DefaultRequestVersion = new Version(2, 0);
                }
                else
                {
                    _client = new HttpClient();
                }
                _endpointPlainText = endpoint + "/plaintext";
            }

            public async Task<BenchmarkReply> SayHelloAsync(BenchmarkRequest data)
            {
                var request = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                using var res = await _client.PostAsync(_endpointPlainText, request);
                res.EnsureSuccessStatusCode();
                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BenchmarkReply>(content);
            }
        }
    }
}
