using Benchmark.ClientLib.Internal;
using System;

namespace Benchmark.ClientLib
{
    public class BenchmarkerConfig
    {
        // follow to ghz https://github.com/LesnyRumcajs/grpc_bench
        /// <summary>
        /// Number of requests to run. Default is 200.
        /// </summary>
        public int[] TotalRequests { get; init; } = new[] { 200 };
        /// <summary>
        /// Duration of application to send requests. When duration is reached, application stops and exits. If duration is specified, TotalRequest is ignored. Examples: -z 10s -z 3m.
        /// </summary>
        public string Duration { get; init; } = "0";
        /// <summary>
        /// Payload size of request
        /// </summary>
        public RequestPayload RequestPayload { get; init; } = RequestPayload.Byte100;
        /// <summary>
        /// Number of connections to use. Concurrency is distributed evenly among all the connections. Default is 1.
        /// </summary>
        public int ClientConnections { get; init; } = 30;
        /// <summary>
        /// Number of request workers to run concurrently for const concurrency schedule. Default is 1.
        /// </summary>
        public int ClientConcurrency { get; init; } = 50;
        /// <summary>
        /// Benchmark target endpoint is tls.
        /// </summary>
        public bool UseSelfCertEndpoint { get; set; }
        /// <summary>
        /// enable to auto-generate Html Report after Benchmark execution. false to generate manually.
        /// </summary>
        public bool GenerateHtmlReportAfterBench { get; set; }

        public TimeSpan GetDuration() => Durations.FromString(Duration);
        public string GetRequestPayload() => Payload.FromPayload(RequestPayload);
        public void Validate()
        {
            if (ClientConnections <= 0)
                throw new ArgumentOutOfRangeException($"number of connections cannot be smaller than 0");
            if (ClientConcurrency <= 0)
                throw new ArgumentOutOfRangeException($"number of connections cannot be smaller than 0");
            if (ClientConnections > ClientConcurrency)
                throw new ArgumentOutOfRangeException($"number of connections ({ClientConnections}) cannot be greater than concurrency ({ClientConcurrency})");

            Durations.Validate(Duration);
        }
    }
}
