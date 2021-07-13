using Benchmark.Server.Shared;
using Benchmark.Shared;
using MagicOnion;
using MagicOnion.Server;

namespace Benchmark.Server.Services
{
    public partial class BenchmarkService : ServiceBase<IBenchmarkService>, IBenchmarkService
    {
        public UnaryResult<BenchmarkReply> SayHelloAsync(BenchmarkRequest data)
        {
            return UnaryResult(new BenchmarkReply { Message = data.Name });
        }
    }
}
