using MagicOnion;
using MessagePack;
using System.Threading.Tasks;

namespace Benchmark.Server.Shared
{
    public interface IBenchmarkHubReciever
    {
        void OnProcess();
    }

    public interface IBenchmarkHub : IStreamingHub<IBenchmarkHub, IBenchmarkHubReciever>
    {
        Task<BenchmarkReply> SayHelloAsync(BenchmarkRequest data);
    }

    [MessagePackObject]
    public class BenchmarkRequest
    {
        [Key(0)]
        public string Name { get; set; }
    }

    [MessagePackObject]
    public class BenchmarkReply
    {
        [Key(0)]
        public string Message { get; set; }
    }
}
