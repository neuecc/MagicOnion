using System;
using System.Threading.Tasks;

namespace Benchmark.ClientLib
{
    internal static class TaskExtensions
    {
        public static void FireAndForget(this Task task)
        {
            task.ContinueWith(x =>
            {
                Console.WriteLine("TaskUnhandled", x.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
