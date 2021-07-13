using Benchmark.Server.Shared;
using MagicOnion;

namespace Benchmark.Shared
{
    public interface IBenchmarkService : IService<IBenchmarkService>
    {
        UnaryResult<BenchmarkReply> SayHelloAsync(BenchmarkRequest data);
    }
}
