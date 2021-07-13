using Benchmark.ClientLib.Internal;
using System;

namespace Benchmark.ClientLib
{
    public class Statistics : IDisposable
    {
        public string Name { get; }
        public DateTime Begin { get; }
        public DateTime End { get; private set; }
        public TimeSpan Duration { get; private set; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        private readonly ValueStopwatch _stopwatch;

        public Statistics(string name = "")
        {
            Name = name;
            Begin = DateTime.UtcNow;
            _stopwatch = ValueStopwatch.StartNew();
        }

        public void Dispose()
        {
            End = DateTime.UtcNow;
            Duration = Elapsed;
        }
    }
}
