using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if MAGICONION_UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
using Channel = Grpc.Core.Channel;
#endif
using Grpc.Core;
using MagicOnion.Unity;
using UnityEngine;

namespace MagicOnion
{
    /// <summary>
    /// gRPC Channel wrapper that managed by the channel provider.
    /// </summary>
    public sealed partial class GrpcChannelx : IMagicOnionAwareGrpcChannel, IDisposable
#if UNITY_EDITOR
        , IGrpcChannelxDiagnosticsInfo
#endif
#if MAGICONION_UNITASK_SUPPORT
        , IUniTaskAsyncDisposable
#endif
    {
        private readonly Action<GrpcChannelx> _onDispose;
        private readonly Dictionary<IStreamingHubMarker, Func<Task>> _streamingHubs = new Dictionary<IStreamingHubMarker, Func<Task>>();
        private readonly Channel _channel;
        private bool _disposed;

        public Uri Target { get; }
        public int Id { get; }

        public ChannelState ChannelState => _channel.State;

#if UNITY_EDITOR
        private readonly string _stackTrace;
        private readonly IReadOnlyList<ChannelOption> _channelOptions;
        private readonly ChannelStats _channelStats;

        string IGrpcChannelxDiagnosticsInfo.StackTrace => _stackTrace;
        ChannelStats IGrpcChannelxDiagnosticsInfo.Stats => _channelStats;
        IReadOnlyList<ChannelOption> IGrpcChannelxDiagnosticsInfo.ChannelOptions => _channelOptions;
#endif

        public GrpcChannelx(int id, Action<GrpcChannelx> onDispose, Channel channel, Uri target, IReadOnlyList<ChannelOption> channelOptions)
        {
            Id = id;
            Target = target;
            _onDispose = onDispose;
            _channel = channel;
            _disposed = false;

#if UNITY_EDITOR
            _stackTrace = new System.Diagnostics.StackTrace().ToString();
            _channelStats = new ChannelStats();
            _channelOptions = channelOptions;
#endif
        }

        /// <summary>
        /// Create a channel to the specified target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static GrpcChannelx FromTarget(GrpcChannelTarget target)
            => GrpcChannelProvider.Default.CreateChannel(target);

        /// <summary>
        /// Create a channel to the specified target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static GrpcChannelx FromAddress(Uri target)
            => GrpcChannelProvider.Default.CreateChannel(target.Host, target.Port, (target.Scheme == "http" ? ChannelCredentials.Insecure : new SslCredentials()));

        /// <summary>
        /// Create a <see cref="CallInvoker"/>.
        /// </summary>
        /// <returns></returns>
        public CallInvoker CreateCallInvoker()
        {
            ThrowIfDisposed();
#if UNITY_EDITOR
            return new ChannelStats.WrappedCallInvoker(((IGrpcChannelxDiagnosticsInfo)this).Stats, _channel.CreateCallInvoker());
#else
            return _channel.CreateCallInvoker();
#endif
        }

        public static implicit operator CallInvoker(GrpcChannelx channel)
            => channel.CreateCallInvoker();

        /// <summary>
        /// Connect to the target using gRPC channel. see <see cref="Grpc.Core.Channel.ConnectAsync"/>.
        /// </summary>
        /// <param name="deadline"></param>
        /// <returns></returns>
#if MAGICONION_UNITASK_SUPPORT
        public async UniTask ConnectAsync(DateTime? deadline = null)
#else
        public async Task ConnectAsync(DateTime? deadline = null)
#endif
        {
            ThrowIfDisposed();
            await _channel.ConnectAsync(deadline);
        }


        /// <inheritdoc />
        IReadOnlyCollection<IStreamingHubMarker> IMagicOnionAwareGrpcChannel.GetAllManagedStreamingHubs()
        {
            lock (_streamingHubs)
            {
                return _streamingHubs.Keys.ToArray();
            }
        }

        /// <inheritdoc />
        void IMagicOnionAwareGrpcChannel.ManageStreamingHubClient(IStreamingHubMarker streamingHub, Func<Task> disposeAsync, Task waitForDisconnect)
        {
            lock (_streamingHubs)
            {
                _streamingHubs.Add(streamingHub, disposeAsync);

                // 切断されたら管理下から外す
                Forget(WaitForDisconnectAndDisposeAsync(streamingHub, waitForDisconnect));
            }
        }

#if MAGICONION_UNITASK_SUPPORT
        private async UniTask WaitForDisconnectAndDisposeAsync(IStreamingHubMarker streamingHub, Task waitForDisconnect)
#else
        private async Task WaitForDisconnectAndDisposeAsync(IStreamingHubMarker streamingHub, Task waitForDisconnect)
#endif
        {
            await waitForDisconnect;
            DisposeStreamingHubClient(streamingHub);
        }

        private void DisposeStreamingHubClient(IStreamingHubMarker streamingHub)
        {
            lock (_streamingHubs)
            {
                if (_streamingHubs.TryGetValue(streamingHub, out var disposeAsync))
                {
                    try
                    {
                        Forget(disposeAsync());
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    _streamingHubs.Remove(streamingHub);
                }
            }

            async void Forget(Task t)
            {
                try
                {
                    await t;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void DisposeAllManagedStreamingHubs()
        {
            lock (_streamingHubs)
            {
                foreach (var streamingHub in _streamingHubs.Keys.ToArray() /* Snapshot */)
                {
                    DisposeStreamingHubClient(streamingHub);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            try
            {
                DisposeAllManagedStreamingHubs();
                Forget(ShutdownCoreAsync());
            }
            finally
            {
                _onDispose(this);
            }
        }

#if MAGICONION_UNITASK_SUPPORT
        public async UniTask DisposeAsync()
#else
        public async Task DisposeAsync()
#endif
        {
            if (_disposed) return;

            _disposed = true;
            try
            {
                DisposeAllManagedStreamingHubs();
                await ShutdownCoreAsync();
            }
            finally
            {
                _onDispose(this);
            }
        }

#if MAGICONION_UNITASK_SUPPORT
        private async UniTask ShutdownCoreAsync()
#else
        private async Task ShutdownCoreAsync()
#endif
        {
            await _channel.ShutdownAsync();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GrpcChannelx));
        }

#if MAGICONION_UNITASK_SUPPORT
        private static async void Forget(UniTask t)
            => t.Forget();
#endif

        private static async void Forget(Task t)
        {
            try
            {
                await t;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

#if UNITY_EDITOR
        public class ChannelStats
        {
            private int _sentBytes = 0;
            private int _receivedBytes = 0;

            private int _indexSentBytes;
            private int _indexReceivedBytes;
            private DateTime _prevSentBytesAt;
            private DateTime _prevReceivedBytesAt;
            private readonly int[] _sentBytesHistory = new int[10];
            private readonly int[] _receivedBytesHistory = new int[10];

            public int SentBytes => _sentBytes;
            public int ReceivedBytes => _receivedBytes;

            public int SentBytesPerSecond
            {
                get
                {
                    AddValue(ref _prevSentBytesAt, ref _indexSentBytes, _sentBytesHistory, DateTime.Now, 0);
                    return _sentBytesHistory.Sum();
                }
            }

            public int ReceiveBytesPerSecond
            {
                get
                {
                    AddValue(ref _prevReceivedBytesAt, ref _indexReceivedBytes, _receivedBytesHistory, DateTime.Now, 0);
                    return _receivedBytesHistory.Sum();
                }
            }

            public void AddSentBytes(int bytesLength)
            {
                Interlocked.Add(ref _sentBytes, bytesLength);
                AddValue(ref _prevSentBytesAt, ref _indexSentBytes, _sentBytesHistory, DateTime.Now, bytesLength);
            }

            public void AddReceivedBytes(int bytesLength)
            {
                Interlocked.Add(ref _receivedBytes, bytesLength);
                AddValue(ref _prevReceivedBytesAt, ref _indexReceivedBytes, _receivedBytesHistory, DateTime.Now, bytesLength);
            }

            private void AddValue(ref DateTime prev, ref int index, int[] values, DateTime d, int value)
            {
                lock (values)
                {
                    var elapsed = d - prev;

                    if (elapsed.TotalMilliseconds > 1000)
                    {
                        index = 0;
                        Array.Clear(values, 0, values.Length);
                        prev = d;
                    }
                    else if (elapsed.TotalMilliseconds > 100)
                    {
                        var advance = (int)(elapsed.TotalMilliseconds / 100);
                        for (var i = 0; i < advance; i++)
                        {
                            values[(++index % values.Length)] = 0;
                        }
                        prev = d;
                    }

                    values[index % values.Length] += value;
                }
            }

            public class WrappedCallInvoker : CallInvoker
            {
                private readonly CallInvoker _baseCallInvoker;
                private readonly ChannelStats _channelStats;


                public WrappedCallInvoker(ChannelStats channelStats, CallInvoker callInvoker)
                {
                    _channelStats = channelStats;
                    _baseCallInvoker = callInvoker;
                }

                public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
                {
                    //Debug.Log($"Unary(Blocking): {method.FullName}");
                    return _baseCallInvoker.BlockingUnaryCall(WrapMethod(method), host, options, request);
                }

                public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
                {
                    //Debug.Log($"Unary: {method.FullName}");
                    return _baseCallInvoker.AsyncUnaryCall(WrapMethod(method), host, options, request);
                }

                public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
                {
                    //Debug.Log($"ServerStreaming: {method.FullName}");
                    return _baseCallInvoker.AsyncServerStreamingCall(WrapMethod(method), host, options, request);
                }

                public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
                {
                    //Debug.Log($"ClientStreaming: {method.FullName}");
                    return _baseCallInvoker.AsyncClientStreamingCall(WrapMethod(method), host, options);
                }

                public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
                {
                    //Debug.Log($"DuplexStreaming: {method.FullName}");
                    return _baseCallInvoker.AsyncDuplexStreamingCall(WrapMethod(method), host, options);
                }

                private Method<TRequest, TResponse> WrapMethod<TRequest, TResponse>(Method<TRequest, TResponse> method)
                {
                    var wrappedMethod = new Method<TRequest, TResponse>(
                        method.Type,
                        method.ServiceName,
                        method.Name,
                        new Marshaller<TRequest>(x =>
                        {
                            var bytes = method.RequestMarshaller.Serializer(x);
                            _channelStats.AddSentBytes(bytes.Length);
                            return bytes;
                        }, x => method.RequestMarshaller.Deserializer(x)),
                        new Marshaller<TResponse>(x => method.ResponseMarshaller.Serializer(x), x =>
                        {
                            _channelStats.AddReceivedBytes(x.Length);
                            return method.ResponseMarshaller.Deserializer(x);
                        })
                    );

                    return wrappedMethod;
                }
            }
        }
#endif
    }

    public interface IMagicOnionAwareGrpcChannel
    {
        /// <summary>
        /// Register the StreamingHub under the management of the channel.
        /// </summary>
        void ManageStreamingHubClient(IStreamingHubMarker streamingHub, Func<Task> disposeAsync, Task waitForDisconnect);

        /// <summary>
        /// Gets all StreamingHubs that depends on the channel.
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<IStreamingHubMarker> GetAllManagedStreamingHubs();
    }

#if UNITY_EDITOR
    public interface IGrpcChannelxDiagnosticsInfo
    {
        string StackTrace { get; }

        GrpcChannelx.ChannelStats Stats { get; }

        IReadOnlyList<ChannelOption> ChannelOptions { get; }
    }
#endif
}