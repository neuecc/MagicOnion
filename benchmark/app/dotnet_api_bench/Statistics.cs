using System.Threading;

namespace Benchmark.Server.Api
{
    public static class Statistics
    {
        // todo: show statistics in interval
        public static int Connections => connections;
        public static int Errors => errors;

        private static int connections = 0;
        private static int errors = 0;

        public static void Connected()
        {
            Interlocked.Increment(ref connections);
        }
        public static void Disconnected()
        {
            Interlocked.Decrement(ref connections);
        }
        public static void Error()
        {
            Interlocked.Increment(ref errors);
        }
    }
}
