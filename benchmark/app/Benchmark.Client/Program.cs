using Benchmark.ClientLib;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZLogger;

var builder = Host.CreateDefaultBuilder()
    .ConfigureLogging((hostContext, logging) =>
    {
        // set `DOTNET_ENVIRONMENT=Production` or leave blank to use Production.
        var logLevel = hostContext.HostingEnvironment.IsDevelopment()
            ? LogLevel.Debug
            : LogLevel.Information; // output only result
        logging.ClearProviders();
        logging.AddZLoggerConsole(configure => configure.EnableStructuredLogging = false);
        logging.SetMinimumLevel(logLevel);
    });
if (Environment.GetEnvironmentVariable("BENCHCLIENT_RUNASWEB") == "true")
{
    var hostAddress = Environment.GetEnvironmentVariable("BENCHCLIENT_HOSTADDRESS") ?? "http://localhost:8080";
    await builder.RunConsoleAppFrameworkWebHostingAsync(hostAddress);
}
else
{
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BENCHCLIENT_DELAY")))
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
    await builder.RunConsoleAppFrameworkAsync(args);
}

public class BenchmarkRunner : ConsoleAppBase
{
    private readonly string _path;
    private readonly bool _generateHtmlReport;

    public BenchmarkRunner()
    {
        _path = Environment.GetEnvironmentVariable("BENCHCLIENT_S3BUCKET") ?? "0";
        _generateHtmlReport = (Environment.GetEnvironmentVariable("BENCHCLIENT_GEN_HTML") ?? "false") == "true";
    }

    private bool IsHttpsEndpoint(string endpoint) => endpoint.StartsWith("https://");

    /// <summary>
    /// Run MagicOnion Unary Benchmark
    /// </summary>
    /// <param name="hostAddress"></param>
    /// <param name="iterations"></param>
    /// <param name="duration"></param>
    /// <param name="concurrency"></param>
    /// <param name="connections"></param>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task MO(string hostAddress = "http://127.0.0.1:5000", string iterations = "1", string duration = "30s", int concurrency = 50, int connections = 5, string reportId = "")
    {
        var iter = iterations.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken)
        {
            Config = new BenchmarkerConfig
            {
                ClientConcurrency = concurrency,
                ClientConnections = connections,
                Duration = duration,
                TotalRequests = iter,
                UseSelfCertEndpoint = IsHttpsEndpoint(hostAddress),
                GenerateHtmlReportAfterBench = _generateHtmlReport,
            }
        };
        await benchmarker.MagicOnionUnary(hostAddress, reportId);
    }

    /// <summary>
    /// Run MagicOnion Hub Benchmark
    /// </summary>
    /// <param name="hostAddress"></param>
    /// <param name="iterations"></param>
    /// <param name="duration"></param>
    /// <param name="concurrency"></param>
    /// <param name="connections"></param>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task MOHub(string hostAddress = "http://127.0.0.1:5000", string iterations = "1", string duration = "30s", int concurrency = 50, int connections = 5, string reportId = "")
    {
        var iter = iterations.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken)
        {
            Config = new BenchmarkerConfig
            {
                ClientConcurrency = concurrency,
                ClientConnections = connections,
                Duration = duration,
                TotalRequests = iter,
                UseSelfCertEndpoint = IsHttpsEndpoint(hostAddress),
                GenerateHtmlReportAfterBench = _generateHtmlReport,
            }
        };
        await benchmarker.MagicOnionHub(hostAddress, reportId);
    }

    /// <summary>
    /// Run MagicOnion Long running Hub Benchmark
    /// </summary>
    /// <param name="waitMilliseconds"></param>
    /// <param name="hostAddress"></param>
    /// <param name="iterations"></param>
    /// <param name="duration"></param>
    /// <param name="concurrency"></param>
    /// <param name="connections"></param>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task MOLongRunHub(int waitMilliseconds, string hostAddress = "http://127.0.0.1:5000", string iterations = "1", string duration = "30s", int concurrency = 50, int connections = 5, string reportId = "")
    {
        var iter = iterations.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken)
        {
            Config = new BenchmarkerConfig
            {
                ClientConcurrency = concurrency,
                ClientConnections = connections,
                Duration = duration,
                TotalRequests = iter,
                UseSelfCertEndpoint = IsHttpsEndpoint(hostAddress),
                GenerateHtmlReportAfterBench = _generateHtmlReport,
            }
        };
        await benchmarker.MagicOnionLongRunHub(waitMilliseconds, hostAddress, reportId);
    }

    /// <summary>
    /// Run Grpc Unary Benchmark
    /// </summary>
    /// <param name="hostAddress"></param>
    /// <param name="iterations"></param>
    /// <param name="duration"></param>
    /// <param name="concurrency"></param>
    /// <param name="connections"></param>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task Grpc(string hostAddress = "http://127.0.0.1:5000", string iterations = "1", string duration = "30s", int concurrency = 50, int connections = 5, string reportId = "")
    {
        var iter = iterations.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken)
        {
            Config = new BenchmarkerConfig
            {
                ClientConcurrency = concurrency,
                ClientConnections = connections,
                Duration = duration,
                TotalRequests = iter,
                UseSelfCertEndpoint = IsHttpsEndpoint(hostAddress),
                GenerateHtmlReportAfterBench = _generateHtmlReport,
            }
        };
        await benchmarker.GrpcUnary(hostAddress, reportId);
    }

    /// <summary>
    /// Run REST Api Benchmark
    /// </summary>
    /// <param name="hostAddress"></param>
    /// <param name="iterations"></param>
    /// <param name="duration"></param>
    /// <param name="concurrency"></param>
    /// <param name="connections"></param>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task Api(string hostAddress = "http://127.0.0.1:5000", string iterations = "1", string duration = "30s", int concurrency = 50, int connections = 5, string reportId = "")
    {
        var iter = iterations.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken)
        {
            Config = new BenchmarkerConfig
            {
                ClientConcurrency = concurrency,
                ClientConnections = connections,
                Duration = duration,
                TotalRequests = iter,
                UseSelfCertEndpoint = IsHttpsEndpoint(hostAddress),
                GenerateHtmlReportAfterBench = _generateHtmlReport,
            }
        };
        await benchmarker.RestApi(hostAddress, reportId, false);
    }

    /// <summary>
    /// Run REST Api Http/2 Benchmark
    /// </summary>
    /// <param name="hostAddress"></param>
    /// <param name="iterations"></param>
    /// <param name="duration"></param>
    /// <param name="concurrency"></param>
    /// <param name="connections"></param>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task ApiHttp2(string hostAddress = "http://127.0.0.1:5000", string iterations = "1", string duration = "30s", int concurrency = 50, int connections = 5, string reportId = "")
    {
        var iter = iterations.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken)
        {
            Config = new BenchmarkerConfig
            {
                ClientConcurrency = concurrency,
                ClientConnections = connections,
                Duration = duration,
                TotalRequests = iter,
                UseSelfCertEndpoint = IsHttpsEndpoint(hostAddress),
                GenerateHtmlReportAfterBench = _generateHtmlReport,
            }
        };
        await benchmarker.RestApi(hostAddress, reportId, true);
    }

    /// <summary>
    /// List Reports
    /// </summary>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task ListReports(string reportId)
    {
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken);
        await benchmarker.ListReports(reportId);
    }

    /// <summary>
    /// Get Report
    /// </summary>
    /// <param name="reportId"></param>
    /// <returns></returns>
    public async Task GetReports(string reportId)
    {
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken);
        await benchmarker.GetReports(reportId);
    }

    /// <summary>
    /// Generate Report Html
    /// </summary>
    /// <param name="reportId"></param>
    /// <param name="htmlFileName"></param>
    /// <returns></returns>
    public async Task GenerateHtml(string reportId, string htmlFileName = "index.html")
    {
        var benchmarker = new Benchmarker(_path, Context.Logger, Context.CancellationToken);
        await benchmarker.GenerateHtmlAsync(reportId, htmlFileName);
    }
}
