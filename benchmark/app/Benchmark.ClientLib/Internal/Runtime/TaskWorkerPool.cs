using Benchmark.ClientLib.Reports;
using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Benchmark.ClientLib.Internal.Runtime
{
    public class TaskWorkerPool<TRequest> : IDisposable
    {
        private readonly int _workerCount;
        private readonly CancellationToken _ct;
        private readonly TaskCompletionSource _timeoutTcs = new TaskCompletionSource();
        private readonly TaskCompletionSource _completeTask = new TaskCompletionSource();
        private int _completeCount;

        private readonly Channel<Func<int, TRequest, CancellationToken, Task>> _channel;

        private readonly ConcurrentBag<CallResult> _results = new ConcurrentBag<CallResult>();

        public Func<(int current, int completed), bool> CompleteCondition { get; init; } = (x) => false;
        public int CompleteCount => _completeCount;
        public bool Timeouted => _timeoutTcs.Task.IsCompleted;
        public bool Completed => _completeTask.Task.IsCompleted;

        public TaskWorkerPool(int workerCount, CancellationToken ct) : this(workerCount, 2000, ct)
        {
        }

        public TaskWorkerPool(int workerCount, int channelSize, CancellationToken ct)
        {
            _workerCount = workerCount;
            _ct = ct;
            _ct.Register(() => _timeoutTcs.TrySetResult());
            _channel = System.Threading.Channels.Channel.CreateBounded<Func<int, TRequest, CancellationToken, Task>>(new BoundedChannelOptions(channelSize)
            {
                SingleReader = false,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait,
            });
        }

        /// <summary>
        /// Wait for Pool complete
        /// </summary>
        /// <returns></returns>
        public Task WaitForCompleteAsync() => _completeTask.Task;

        /// <summary>
        /// Wait for Pool end by timeout
        /// </summary>
        /// <returns></returns>
        public Task WaitForTimeout() => _timeoutTcs.Task;

        public void RunWorkers(Func<int, TRequest, CancellationToken, Task> action, TRequest request, CancellationToken ct)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            RunCore(action, request, ct);
            WatchComplete();
        }

        public CallResult[] GetResult()
        {
            return _results.ToArray();
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();
            _results.Clear();
        }

        /// <summary>
        /// Main execution
        /// </summary>
        /// <returns></returns>
        private void RunCore(Func<int, TRequest, CancellationToken, Task> action, TRequest request, CancellationToken ct)
        {
            // write
            Task.Run(async () =>
            {
                do
                {
                    try
                    {
                        _channel.Writer.TryWrite(action);
                    }
                    catch (ChannelClosedException)
                    {
                        // already closed.
                    }
                    catch (OperationCanceledException)
                    {
                        // canceled
                    }

                } while (await _channel.Writer.WaitToWriteAsync(_ct).ConfigureAwait(false));
            }, _ct).ConfigureAwait(false);

            // read
            var workerId = 0;
            for (var i = 0; i < _workerCount; i++)
            {
                var id = workerId++;
                Task.Run(async () =>
                {
                    do
                    {
                        while (_channel.Reader.TryRead(out var item))
                        {
                            var sw = ValueStopwatch.StartNew();
                            Exception error = null;
                            var status = Status.DefaultSuccess;

                            try
                            {
                                await item.Invoke(id, request, ct);
                            }
                            catch (RpcException rex)
                            {
                                error = rex;
                                status = rex.Status;
                            }
                            catch (OperationCanceledException oex)
                            {
                                error = oex;
                                status = Status.DefaultCancelled;
                            }
                            catch (ChannelClosedException)
                            {
                                // already closed.
                            }
                            catch (Exception ex)
                            {
                                error = ex;
                                status = new Status(StatusCode.Internal, ex.Message);
                            }
                            finally
                            {
                                Interlocked.Increment(ref _completeCount);
                                _results.Add(new CallResult
                                {
                                    Duration = sw.Elapsed,
                                    Error = error,
                                    Status = status,
                                    TimeStamp = DateTime.UtcNow,
                                });
                            }
                        }
                    }
                    while (await _channel.Reader.WaitToReadAsync(_ct).ConfigureAwait(false));
                }, _ct).ConfigureAwait(false);
            }
        }

        private void WatchComplete()
        {
            // complete
            Task.Run(async () =>
            {
                do
                {
                    if (_ct.IsCancellationRequested)
                    {
                        _completeTask.SetCanceled();
                        return;
                    }
                    await Task.Delay(100).ConfigureAwait(false);
                } while (!CompleteCondition((_channel.Reader.Count, _completeCount)));
                _completeTask.SetResult();
            }, _ct).ConfigureAwait(false);
        }
    }
}
