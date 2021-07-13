using System;
using System.Collections.Generic;

namespace Benchmark.ClientLib.Reports
{
    public record HtmlReport(
        HtmlReportClient Client, 
        HtmlReportSummary Summary,
        HtmlReportConfig Config,
        HtmlReportRequest[] Requests);
    public record HtmlReportClient
    {
        public string Os { get; init; }
        public string Architecture { get; init; }
        public int Processors { get; init; }
        public long Memory { get; init; }
        public string Framework { get; init; }
        public string Version { get; init; }
    }
    public record HtmlReportSummary
    {
        public string ScenarioName { get; init; }
        public string ReportId { get; init; }
        public int Clients { get; init; }
        public int Concurrency { get; init; }
        public int Connections { get; init; }
        public DateTime Begin { get; init; }
        public DateTime End { get; init; }
        public TimeSpan Duration { get; init; }
        public long Requests { get; init; }
        public double Rps { get; init; }
        public TimeSpan Average { get; init; }
        public TimeSpan Fastest { get; init; }
        public TimeSpan Slowest { get; init; }
    }
    public record HtmlReportConfig(int ClientConcurrency, int ClientConnections);

    public record HtmlReportRequest
    {
        public string Key { get; init; }
        public HtmlReportRequestSummary[] Summaries { get; init; }
        public HtmlReportRequestStatusCode[] StatusCodes { get; init; }
        public HtmlReportRequestErrorCode[] ErrorCodes { get; init; }
        public HtmlReportRequestDuration[] Durations { get; init; }
        public HtmlReportRequestLatency[] Latencies { get; set; }
        public HtmlReportRequestHistogram[] Histograms { get; set; }
    }
    public record HtmlReportRequestSummary
    {
        public int RequestCount { get; init; }
        public TimeSpan Duration { get; init; }
        public double Rps { get; init; }
        public int Errors { get; init; }
    }
    public record HtmlReportRequestStatusCode(string StatusCode, int Count);
    public record HtmlReportRequestErrorCode(string StatusCode, int Count, string Detail);
    public record HtmlReportRequestDuration(string Client, HtmlReportRequestDurationItem[] Items);    
    public record HtmlReportRequestDurationItem
    {
        public int RequestCount { get; init; }
        public HtmlReportRequestSummary[] Summaries { get; init; }
    }
    public record HtmlReportRequestLatency(int Percentile, TimeSpan Duration);
    public record HtmlReportRequestHistogram(double Mark, int Count, double Frequency);
}
