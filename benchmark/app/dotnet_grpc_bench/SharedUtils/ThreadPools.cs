using System;
using System.Threading;

namespace Benchmark.Server
{
    public class ThreadPools
    {
        public static void ModifyThreadPool(int workerThread, int completionPortThread)
        {
            GetCurrentThread();
            SetThread(workerThread, completionPortThread);
            GetCurrentThread();
        }

        public static void GetCurrentThread()
        {
            ThreadPool.GetMinThreads(out var minWorkerThread, out var minCompletionPorlThread);
            ThreadPool.GetAvailableThreads(out var availWorkerThread, out var availCompletionPorlThread);
            ThreadPool.GetMaxThreads(out var maxWorkerThread, out var maxCompletionPorlThread);
            Console.WriteLine($"min: {minWorkerThread} {minCompletionPorlThread}");
            Console.WriteLine($"max: {maxWorkerThread} {maxCompletionPorlThread}");
            Console.WriteLine($"available: {availWorkerThread} {availCompletionPorlThread}");
        }

        private static void SetThread(int workerThread, int completionPortThread)
        {
            Console.WriteLine($"Changing ThreadPools. workerthread: {workerThread} completionPortThread: {completionPortThread}");
            ThreadPool.SetMinThreads(workerThread, completionPortThread);
        }
    }
}
