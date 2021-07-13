using Grpc.Core;
using System.Threading.Tasks;

namespace Benchmark.Server
{
    class GreeterImpl : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = request.Name });
        }
    }
}
