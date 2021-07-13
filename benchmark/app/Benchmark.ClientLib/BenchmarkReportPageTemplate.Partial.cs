using Benchmark.ClientLib.Reports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Benchmark.ClientLib
{
    public partial class BenchmarkReportPageTemplate
    {
        public HtmlReport Report { get; init; }

        private static readonly string[] colorPatterns = new[]
        {
            "rgba(254,97,132,0.8)",
        };
        private static readonly string[] pastelColorPatterns = new[]
        {
            // https://colorhunt.co/palette/189886
            "#abc2e8",
            "#dbc6eb",
            "#d1eaa3",
            "#efee9d",
        };

        public string GetLineColor() => GetLineColor(0);
        public string GetLineColor(int current) => colorPatterns[current % colorPatterns.Length];
        public string GetColor() => GetColor(0);
        public string GetColor(int current) => pastelColorPatterns[current % pastelColorPatterns.Length];

        public string FormatMs(double ms)
        {
            return ms < 1
                ? string.Format("{0:f4}", ms)
                : string.Format("{0:f2}", ms);
        }
    }
}
