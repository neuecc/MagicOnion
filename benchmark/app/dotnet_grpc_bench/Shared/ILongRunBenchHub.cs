using MagicOnion;
using MessagePack;
using System.Threading.Tasks;

namespace Benchmark.Server.Shared
{
    public interface ILongRunBenchmarkHubReciever
    {
        void OnProcess();
    }

    public interface ILongRunBenchmarkHub : IStreamingHub<ILongRunBenchmarkHub, ILongRunBenchmarkHubReciever>
    {
        Task Process(LongRunBenchmarkData data);
    }

    [MessagePackObject]
    public class LongRunBenchmarkData
    {
        [Key(0)]
        public int WaitMilliseconds { get; set; }
    }
}
