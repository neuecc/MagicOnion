using MagicOnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmark.ClientLib
{
    internal static class ValueTaskUtils
    {
        public static async ValueTask<T[]> WhenAll<T>(ValueTask<T>[] tasks)
        {
            // We don't allocate the list if no task throws
            List<Exception> exceptions = null;
            var results = new T[tasks.Length];

            for (var i = 0; i < tasks.Length; i++)
            {
                try
                {
                    results[i] = await tasks[i].ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>(tasks.Length);
                    exceptions.Add(ex);
                }
            }

            return exceptions is null
                ? results
                : throw new AggregateException(exceptions);
        }
        public static async ValueTask<T[]> WhenAll<T>(List<ValueTask<T>> tasks)
        {
            // We don't allocate the list if no task throws
            List<Exception> exceptions = null;
            var results = new T[tasks.Count];

            for (var i = 0; i < tasks.Count; i++)
            {
                try
                {
                    results[i] = await tasks[i].ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>(tasks.Count);
                    exceptions.Add(ex);
                }
            }

            return exceptions is null
                ? results
                : throw new AggregateException(exceptions);
        }

        public static async UnaryResult<T[]> WhenAll<T>(UnaryResult<T>[] tasks)
        {
            // don't allocate the list if no task throws
            List<Exception> exceptions = null;
            var results = new T[tasks.Length];

            for (var i = 0; i < tasks.Length; i++)
            {
                try
                {
                    results[i] = await tasks[i];
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>(tasks.Length);
                    exceptions.Add(ex);
                }
            }

            return exceptions is null
                ? results
                : throw new AggregateException(exceptions);
        }

        public static async UnaryResult<T[]> WhenAll<T>(List<UnaryResult<T>> tasks)
        {
            // don't allocate the list if no task throws
            List<Exception> exceptions = null;
            var results = new T[tasks.Count];

            for (var i = 0; i < tasks.Count; i++)
            {
                try
                {
                    results[i] = await tasks[i];
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>(tasks.Count);
                    exceptions.Add(ex);
                }
            }

            return exceptions is null
                ? results
                : throw new AggregateException(exceptions);
        }
    }
}
