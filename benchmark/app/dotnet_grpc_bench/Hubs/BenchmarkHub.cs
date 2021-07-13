using Benchmark.Server.Shared;
using MagicOnion.Server.Hubs;
using System.Threading.Tasks;

namespace Benchmark.Server.Hubs
{
    public partial class BenchmarkHub : StreamingHubBase<IBenchmarkHub, IBenchmarkHubReciever>, IBenchmarkHub
    {
        public Task<BenchmarkReply> SayHelloAsync(BenchmarkRequest data)
        {
            return Task.FromResult(new BenchmarkReply { Message = data.Name });
        }
    }
}
