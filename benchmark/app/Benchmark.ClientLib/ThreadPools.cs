using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Benchmark.ClientLib
{
    public class ThreadPools
    {
        public static void ModifyThreadPool(int workerThread, int completionPortThread, ILogger logger)
        {
            GetCurrentThread(logger);
            SetThread(workerThread, completionPortThread, logger);
            GetCurrentThread(logger);
        }

        public static void GetCurrentThread(ILogger logger)
        {
            ThreadPool.GetMinThreads(out var minWorkerThread, out var minCompletionPorlThread);
            ThreadPool.GetAvailableThreads(out var availWorkerThread, out var availCompletionPorlThread);
            ThreadPool.GetMaxThreads(out var maxWorkerThread, out var maxCompletionPorlThread);
            logger?.LogDebug($"min: {minWorkerThread} {minCompletionPorlThread}");
            logger?.LogDebug($"max: {maxWorkerThread} {maxCompletionPorlThread}");
            logger?.LogDebug($"available: {availWorkerThread} {availCompletionPorlThread}");
        }

        private static void SetThread(int workerThread, int completionPortThread, ILogger logger)
        {
            logger?.LogDebug($"Changing ThreadPools. workerthread: {workerThread} completionPortThread: {completionPortThread}");
            ThreadPool.SetMinThreads(workerThread, completionPortThread);
        }
    }
}
