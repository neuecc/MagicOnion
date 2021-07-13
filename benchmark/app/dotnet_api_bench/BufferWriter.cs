using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Benchmark.Server.Api
{
    // https://github.com/TechEmpower/FrameworkBenchmarks/blob/77fd80534ab640725b6be26d527da406244fcaad/frameworks/CSharp/aspnetcore/PlatformBenchmarks/BufferWriter.cs
    public ref struct BufferWriter<T> where T : IBufferWriter<byte>
    {
        private T _output;
        private Span<byte> _span;
        private int _buffered;

        public BufferWriter(T output, int sizeHint)
        {
            _buffered = 0;
            _output = output;
            _span = output.GetSpan(sizeHint);
        }

        public Span<byte> Span => _span;

        public T Output => _output;

        public int Buffered => _buffered;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            var buffered = _buffered;
            if (buffered > 0)
            {
                _buffered = 0;
                _output.Advance(buffered);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            _buffered += count;
            _span = _span.Slice(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> source)
        {
            if (_span.Length >= source.Length)
            {
                source.CopyTo(_span);
                Advance(source.Length);
            }
            else
            {
                WriteMultiBuffer(source);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ensure(int count = 1)
        {
            if (_span.Length < count)
            {
                EnsureMore(count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureMore(int count = 0)
        {
            if (_buffered > 0)
            {
                Commit();
            }

            _span = _output.GetSpan(count);
        }

        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (_span.Length == 0)
                {
                    EnsureMore();
                }

                var writable = Math.Min(source.Length, _span.Length);
                source.Slice(0, writable).CopyTo(_span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteNumeric(uint number)
        {
            const byte AsciiDigitStart = (byte)'0';

            var span = this.Span;

            // Fast path, try copying to the available memory directly
            var advanceBy = 0;
            if (span.Length >= 3)
            {
                if (number < 10)
                {
                    span[0] = (byte)(number + AsciiDigitStart);
                    advanceBy = 1;
                }
                else if (number < 100)
                {
                    var tens = (byte)((number * 205u) >> 11); // div10, valid to 1028

                    span[0] = (byte)(tens + AsciiDigitStart);
                    span[1] = (byte)(number - (tens * 10) + AsciiDigitStart);
                    advanceBy = 2;
                }
                else if (number < 1000)
                {
                    var digit0 = (byte)((number * 41u) >> 12); // div100, valid to 1098
                    var digits01 = (byte)((number * 205u) >> 11); // div10, valid to 1028

                    span[0] = (byte)(digit0 + AsciiDigitStart);
                    span[1] = (byte)(digits01 - (digit0 * 10) + AsciiDigitStart);
                    span[2] = (byte)(number - (digits01 * 10) + AsciiDigitStart);
                    advanceBy = 3;
                }
            }

            if (advanceBy > 0)
            {
                Advance(advanceBy);
            }
            else
            {
                BufferExtensions.WriteNumericMultiWrite(ref this, number);
            }
        }
    }

    // Same as KestrelHttpServer\src\Kestrel.Core\Internal\Http\PipelineExtensions.cs
    // However methods accept T : struct, IBufferWriter<byte> rather than PipeWriter.
    // This allows a struct wrapper to turn CountingBufferWriter into a non-shared generic,
    // while still offering the WriteNumeric extension.

    public static class BufferExtensions
    {
        private const int _maxULongByteLength = 20;

        [ThreadStatic]
        private static byte[] _numericBytesScratch;

        internal static void WriteUtf8String<T>(ref this BufferWriter<T> buffer, string text)
             where T : struct, IBufferWriter<byte>
        {
            var byteCount = Encoding.UTF8.GetByteCount(text);
            buffer.Ensure(byteCount);
            byteCount = Encoding.UTF8.GetBytes(text.AsSpan(), buffer.Span);
            buffer.Advance(byteCount);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WriteNumericMultiWrite<T>(ref this BufferWriter<T> buffer, uint number)
             where T : IBufferWriter<byte>
        {
            const byte AsciiDigitStart = (byte)'0';

            var value = number;
            var position = _maxULongByteLength;
            var byteBuffer = NumericBytesScratch;
            do
            {
                // Consider using Math.DivRem() if available
                var quotient = value / 10;
                byteBuffer[--position] = (byte)(AsciiDigitStart + (value - quotient * 10)); // 0x30 = '0'
                value = quotient;
            }
            while (value != 0);

            var length = _maxULongByteLength - position;
            buffer.Write(new ReadOnlySpan<byte>(byteBuffer, position, length));
        }

        private static byte[] NumericBytesScratch => _numericBytesScratch ?? CreateNumericBytesScratch();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] CreateNumericBytesScratch()
        {
            var bytes = new byte[_maxULongByteLength];
            _numericBytesScratch = bytes;
            return bytes;
        }
    }
}
