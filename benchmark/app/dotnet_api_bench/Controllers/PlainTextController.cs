using Benchmark.Server.Api.Shared;
using Benchmark.Server.Api.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Server.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlainTextController : ControllerBase
    {
        public PipeWriter Writer { get; private set; }
        public PipeReader Reader { get; private set; }

        private readonly ILogger<PlainTextController> _logger;

        public PlainTextController(ILogger<PlainTextController> logger)
        {
            _logger = logger;

            var pipe = new Pipe();
            Writer = pipe.Writer;
            Reader = pipe.Reader;
        }

        [HttpPost]
        public Task<BenchmarkReply> Post(BenchmarkRequest data)
        {
            // ProcessRequest(RequestType.PlainText, data);
            return Task.FromResult(new BenchmarkReply { Message = data.Name });
        }

        private void ProcessRequest(RequestType requestType, BenchmarkRequest body)
        {
            if (requestType == RequestType.PlainText)
            {
                var writer = GetWriter(Writer, sizeHint: 160 * 16); // 160*16 is for Plaintext, for Json 160 would be enough
                PlainText(ref writer, Encoding.UTF8.GetBytes(body.Name).AsSpan());
            }
        }

        private static void PlainText(ref BufferWriter<WriterAdapter> writer, ReadOnlySpan<byte> body)
        {
            writer.Write(body);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BufferWriter<WriterAdapter> GetWriter(PipeWriter pipeWriter, int sizeHint)
            => new BufferWriter<WriterAdapter>(new WriterAdapter(pipeWriter), sizeHint);

        private struct WriterAdapter : IBufferWriter<byte>
        {
            public PipeWriter Writer;

            public WriterAdapter(PipeWriter writer)
                => Writer = writer;

            public void Advance(int count)
                => Writer.Advance(count);

            public Memory<byte> GetMemory(int sizeHint = 0)
                => Writer.GetMemory(sizeHint);

            public Span<byte> GetSpan(int sizeHint = 0)
                => Writer.GetSpan(sizeHint);
        }
    }
}
