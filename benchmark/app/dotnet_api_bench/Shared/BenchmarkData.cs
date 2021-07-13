using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmark.Server.Api.Shared
{
    public class BenchmarkRequest
    {
        public string Name { get; set; }
    }

    public class BenchmarkReply
    {
        public string Message { get; set; }
    }
}
