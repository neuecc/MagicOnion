using System;
using Benchmark.Server.Shared;
using MagicOnion.Server.Hubs;
using System.Threading.Tasks;

namespace Benchmark.Server.Hubs
{
    public partial class LongRunBenchmarkHub : StreamingHubBase<ILongRunBenchmarkHub, ILongRunBenchmarkHubReciever>, ILongRunBenchmarkHub
    {
        public async Task Process(LongRunBenchmarkData data)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(data.WaitMilliseconds));
        }
    }
}
