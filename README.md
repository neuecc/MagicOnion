# MagicOnion
![build-debug](https://github.com/Cysharp/MagicOnion/workflows/build-debug/badge.svg) ![build-canary](https://github.com/Cysharp/MagicOnion/workflows/build-canary/badge.svg) ![build-release](https://github.com/Cysharp/MagicOnion/workflows/build-release/badge.svg) [![Releases](https://img.shields.io/github/release/Cysharp/MagicOnion.svg)](https://github.com/Cysharp/MagicOnion/releases)

Unified Realtime/API framework for .NET platform and Unity.

[📖 Table of contents](#-table-of-contents)

## About MagicOnion
MagicOnion is a RPC framework for .NET platform that provides bi-directional real-time communications such as [SignalR](https://github.com/aspnet/AspNetCore/tree/master/src/SignalR) and [Socket.io](https://socket.io/) and RPC mechanisms such as WCF and web-based APIs.

This framework is based on [gRPC](https://grpc.io/), which is a fast and compact binary network transport for HTTP/2. However, unlike plain gRPC, it treats C# interfaces as a protocol schema, enabling seamless code sharing between C# projects without the `.proto` (Protocol Buffers IDL).

![image](https://user-images.githubusercontent.com/46207/50965239-c4fdb000-1514-11e9-8365-304c776ffd77.png)

> Interfaces are schemas and provide API services, just like the plain C# code

![image](https://user-images.githubusercontent.com/46207/50965825-7bae6000-1516-11e9-9501-dc91582f4d1b.png)

> Using the StreamingHub real-time communication service, the server can broadcast data to multiple clients

MagicOnion can be adopted or replaced in the following use cases:

- RPC services such as gRPC, used by Microservices, and WCF, commonly used by WinForms/WPF
- API services such as ASP.NET Core MVC targeting Unity, Xamarin, and Windows clients
- Bi-directional real-time communication such as Socket.io, SignalR, Photon and UNet

MagicOnion uses [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) to serialize call arguments and return values. NET primitives and other complex types that can be serialized into MessagePack objects. See MessagePack for C# for details about serialization.

## Requirements
MagicOnion server requires NET Core 3.1 or .NET 5.0+.

MagicOnion client supports a wide range of platforms, including .NET Framework 4.6.1 to .NET 5.0 as well as Unity.

- Server-side (MagicOnion.Server)
    - .NET 5.0+
    - .NET Core 3.1
- Client-side (MagicOnion.Client)
    - .NET Standard 2.1 (.NET Core 3.x+, .NET 5.0+, Xamarin)
    - .NET Standard 2.0 (.NET Framework 4.6.1+, Universal Windows Platform, .NET Core 2.x)
    - Unity 2018.3+

## Quick Start
### Server-side project
#### Setup a project for MagicOnion
First, you need to create a **gRPC Service** project from within Visual Studio or the .NET CLI tools. MagicOnion Server is built on top of ASP.NET Core and gRPC, so the server project must be an ASP.NET Core project.

When you create a project, it contains `Protos` and `Services` folders, which are not needed in MagicOnion projects and should be removed.

Adds NuGet package `MagicOnion.Server` to your project. If you are using the .NET CLI tools to add it, you can run the following command.

```bash
dotnet add package MagicOnion.Server
```

Next, open Startup.cs and add the following line to `ConfigureServices` method.

```csharp
services.AddMagicOnion();
```

`app.UseEndpoints` call in `Configure` method is rewritten as follows.

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapMagicOnionService();

    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
    });
});
```

Now you are ready to use MagicOnion on your server project.

using System;

// define interface as Server/Client IDL.
// implements T : IService<T> and share this type between server and client.
public interface IMyFirstService : IService<IMyFirstService>
{
    // Return type must be `UnaryResult<T>` or `Task<UnaryResult<T>>`.
    // If you can use C# 7.0 or newer, recommend to use `UnaryResult<T>`.
    UnaryResult<int> SumAsync(int x, int y);
}

// implement RPC service to Server Project.
// inehrit ServiceBase<interface>, interface
public class MyFirstService : ServiceBase<IMyFirstService>, IMyFirstService
{
    // You can use async syntax directly.
    public async UnaryResult<int> SumAsync(int x, int y)
    {
        Logger.Debug($"Received:{x}, {y}");

        return x + y;
    }
}
```


and, launch the server.

```csharp
class Program
{
    static void Main(string[] args)
    {
        GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

        // setup MagicOnion and option.
        var service = MagicOnionEngine.BuildServerServiceDefinition(isReturnExceptionStackTraceInErrorDetail: true);

        var server = new global::Grpc.Core.Server
        {
            Services = { service },
            Ports = { new ServerPort("localhost", 12345, ServerCredentials.Insecure) }
        };
        
        // launch gRPC Server.
        server.Start();

        // and wait.
        Console.ReadLine();
    }
}
```

write the client.

```csharp
// standard gRPC channel
var channel = new Channel("localhost", 12345, ChannelCredentials.Insecure);

// get MagicOnion dynamic client proxy
var client = MagicOnionClient.Create<IMyFirstService>(channel);

// call method.
var result = await client.SumAsync(100, 200);
Console.WriteLine("Client Received:" + result);
```

MagicOnion allows primitive, multiple request value. Complex type is serialized by LZ4 Compressed MsgPack by [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) so type should follow MessagePack for C# rules.

> for Server Hosting, We recommend to use `MagicOnion.Hosting`, it is easy to host and wait terminate signal, load from config, support DI, etc. see [Server host](#server-host) section.


## 📖 Table of contents

- [What is it?](#what-is-it)
- [Quick Start](#quick-start)
- Fundamentals
    - [StreamingHub](#streaminghub)
    - [Filter](#filter)
    - [ClientFilter](#clientfilter)
    - [ServiceContext and Lifecycle](#servicecontext-and-lifecycle)
    - [ExceptionHandling and StatusCode](#exceptionhandling-and-statuscode)
    - [Group and GroupConfiguration](#group-and-groupconfiguration)
    - [Project Structure](#project-structure)
    - [Dependency Injection](#dependency-injection)
- Client
    - [Unity client support](#unity-client-support)
        - [iOS build with gRPC](#ios-build-with-grpc)
        - [Stripping debug symbols from ios/libgrpc.a](#stripping-debug-symbols-from-ioslibgrpca)
    - [gRPC Keepalive](#grpc-keepalive)
- Deployment
- [Deployment](#deployment)
- Integrations
    - [Swagger](#swagger)
- Advanced
    - [MagicOnionOption/Logging](#magiconionoptionlogging)
    - [Raw gRPC APIs](#raw-grpc-apis)
    - [Zero deserialization mapping](#zero-deserialization-mapping)
- Experimentals
    - [OpenTelemetry](#opentelemetry)
- [Author Info](#author-info)
- [License](#license)

## Fundamentals
### StreamingHub
StreamingHub is a fully-typed realtime server<->client communication framework.

This sample is for Unity(use Vector3, GameObject, etc) but StreamingHub supports .NET Core, too.

```csharp
// Server -> Client definition
public interface IGamingHubReceiver
{
    // return type shuold be `void` or `Task`, parameters are free.
    void OnJoin(Player player);
    void OnLeave(Player player);
    void OnMove(Player player);
}
 
// Client -> Server definition
// implements `IStreamingHub<TSelf, TReceiver>`  and share this type between server and client.
public interface IGamingHub : IStreamingHub<IGamingHub, IGamingHubReceiver>
{
    // return type shuold be `Task` or `Task<T>`, parameters are free.
    Task<Player[]> JoinAsync(string roomName, string userName, Vector3 position, Quaternion rotation);
    Task LeaveAsync();
    Task MoveAsync(Vector3 position, Quaternion rotation);
}
 
// for example, request object by MessagePack.
[MessagePackObject]
public class Player
{
    [Key(0)]
    public string Name { get; set; }
    [Key(1)]
    public Vector3 Position { get; set; }
    [Key(2)]
    public Quaternion Rotation { get; set; }
}
```

```csharp
// Server implementation
// implements : StreamingHubBase<THub, TReceiver>, THub
public class GamingHub : StreamingHubBase<IGamingHub, IGamingHubReceiver>, IGamingHub
{
    // this class is instantiated per connected so fields are cache area of connection.
    IGroup room;
    Player self;
    IInMemoryStorage<Player> storage;

    public async Task<Player[]> JoinAsync(string roomName, string userName, Vector3 position, Quaternion rotation)
    {
        self = new Player() { Name = userName, Position = position, Rotation = rotation };

        // Group can bundle many connections and it has inmemory-storage so add any type per group. 
        (room, storage) = await Group.AddAsync(roomName, self);

        // Typed Server->Client broadcast.
        Broadcast(room).OnJoin(self);

        return storage.AllValues.ToArray();
    }

    public async Task LeaveAsync()
    {
        await room.RemoveAsync(this.Context);
        Broadcast(room).OnLeave(self);
    }

    public async Task MoveAsync(Vector3 position, Quaternion rotation)
    {
        self.Position = position;
        self.Rotation = rotation;
        Broadcast(room).OnMove(self);
    }

    // You can hook OnConnecting/OnDisconnected by override.
    protected override async ValueTask OnDisconnected()
    {
        // on disconnecting, if automatically removed this connection from group.
        return CompletedTask;
    }
}
```

You can write client like this.

```csharp
public class GamingHubClient : IGamingHubReceiver
{
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
 
    IGamingHub client;
 
    public async Task<GameObject> ConnectAsync(Channel grpcChannel, string roomName, string playerName)
    {
        var client = StreamingHubClient.Connect<IGamingHub, IGamingHubReceiver>(grpcChannel, this);
 
        var roomPlayers = await client.JoinAsync(roomName, playerName, Vector3.zero, Quaternion.identity);
        foreach (var player in roomPlayers)
        {
            (this as IGamingHubReceiver).OnJoin(player);
        }
 
        return players[playerName];
    }
 
    // methods send to server.
 
    public Task LeaveAsync()
    {
        return client.LeaveAsync();
    }
 
    public Task MoveAsync(Vector3 position, Quaternion rotation)
    {
        return client.MoveAsync(position, rotation);
    }
 
    // dispose client-connection before channel.ShutDownAsync is important!
    public Task DisposeAsync()
    {
        return client.DisposeAsync();
    }
 
    // You can watch connection state, use this for retry etc.
    public Task WaitForDisconnect()
    {
        return client.WaitForDisconnect();
    }
 
    // Receivers of message from server.
 
    void IGamingHubReceiver.OnJoin(Player player)
    {
        Debug.Log("Join Player:" + player.Name);
 
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = player.Name;
        cube.transform.SetPositionAndRotation(player.Position, player.Rotation);
        players[player.Name] = cube;
    }
 
    void IGamingHubReceiver.OnLeave(Player player)
    {
        Debug.Log("Leave Player:" + player.Name);
 
        if (players.TryGetValue(player.Name, out var cube))
        {
            GameObject.Destroy(cube);
        }
    }
 
    void IGamingHubReceiver.OnMove(Player player)
    {
        Debug.Log("Move Player:" + player.Name);
 
        if (players.TryGetValue(player.Name, out var cube))
        {
            cube.transform.SetPositionAndRotation(player.Position, player.Rotation);
        }
    }
}
```

### Filter
MagicOnion filter is powerful feature to hook before-after invoke. It is useful than gRPC server interceptor.

![image](https://user-images.githubusercontent.com/46207/50969421-cb465900-1521-11e9-8824-8a34cc52bbe4.png)

```csharp
// You can attach per class/method like [SampleFilter]
// for StreamingHub methods, implement StreamingHubFilterAttribute instead.
public class SampleFilterAttribute : MagicOnionFilterAttribute
{
    public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        try
        {
            /* on before */
            await next(context); // next
            /* on after */
        }
        catch
        {
            /* on exception */
            throw;
        }
        finally
        {
            /* on finally */
        }
    }
}
```

Here is example of what kind of filter can be stacked.

![image](https://user-images.githubusercontent.com/46207/50969539-2bd59600-1522-11e9-84ab-15dd85e3dcac.png)

GlobalFilter can attach to MagicOnionOptions.

MagicOnion filters supports [DI](#dependency-injection).

```csharp
public class MyStreamingHubFilterAttribute : StreamingHubFilterAttribute
{
    private readonly ILogger _logger;

    // the `logger` parameter will be injected at instantiating.
    public MyStreamingHubFilterAttribute(ILogger<MyStreamingHubFilterAttribute> logger)
    {
        _logger = logger;
    }

    public override async ValueTask Invoke(StreamingHubContext context, Func<StreamingHubContext, ValueTask> next)
    {
        _logger.LogInformation($"MyStreamingHubFilter Begin: {context.Path}");
        await next(context);
        _logger.LogInformation($"MyStreamingHubFilter End: {context.Path}");
    }
}
```

Register filters using attributes with constructor injection(you can use `[FromTypeFilter]` and `[FromServiceFilter]`).

```
[FromTypeFilter(typeof(MyFilterAttribute))]
public class MyService : ServiceBase<IMyService>, IMyService
{
    // The filter will instantiate from type.
    [FromTypeFilter(typeof(MySecondFilterAttribute))]
    public UnaryResult<int> Foo()
    {
        return UnaryResult(0);
    }

    // The filter will instantiate from type with some arguments. if the arguments are missing, it will be obtained from `IServiceLocator` 
    [FromTypeFilter(typeof(MyThirdFilterAttribute), Arguments = new object[] { "foo", 987654 })]
    public UnaryResult<int> Bar()
    {
        return UnaryResult(0);
    }

    // The filter instance will be provided via `IServiceLocator`.
    [FromServiceFilter(typeof(MyFourthFilterAttribute))]
    public UnaryResult<int> Baz()
    {
        return UnaryResult(0);
    }
}
```

### ClientFilter
MagicOnion client-filter is a powerful feature to hook before-after invoke. It is useful than gRPC client interceptor.

> Currently only suppots on Unary.

```csharp
// you can attach in MagicOnionClient.Create.
var client = MagicOnionClient.Create<ICalcService>(channel, new IClientFilter[]
{
    new LoggingFilter(),
    new AppendHeaderFilter(),
    new RetryFilter()
});
```

You can create custom client-filter by implements `IClientFilter.SendAsync`.

```csharp
public class IDemoFilter : IClientFilter
{
    public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        try
        {
            /* Before Request, context.MethodPath/CallOptions/Items, etc */

            var response = await next(context); /* Call next filter or method body */

            /* After Request, response.GetStatus/GetTrailers/GetResponseAs<T>, etc */

            return response;
        }
        catch (RpcException ex)
        {
            /* Get gRPC Error Response */
            throw;
        }
        catch (OperationCanceledException ex)
        {
            /* If canceled */
            throw;
        }
        catch (Exception ex)
        {
            /* Other Exception */
            throw;
        }
        finally
        {
            /* Common Finalize */
        }
    }
}
```

Here is the sample filters, you can imagine what you can do.

```csharp
public class AppendHeaderFilter : IClientFilter
{
    public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        // add the common header(like authentcation).
        var header = context.CallOptions.Headers;
        if (!header.Any(x => x.Key == "x-foo"))
        {
            header.Add("x-foo", "abcdefg");
            header.Add("x-bar", "hijklmn");
        }

        return await next(context);
    }
}

public class LoggingFilter : IClientFilter
{
    public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        Console.WriteLine("Request Begin:" + context.MethodPath); // Debug.Log in Unity.

        var sw = Stopwatch.StartNew();
        var response = await next(context);
        sw.Stop();

        Console.WriteLine("Request Completed:" + context.MethodPath + ", Elapsed:" + sw.Elapsed.TotalMilliseconds + "ms");

        return response;
    }
}

public class ResponseHandlingFilter : IClientFilter
{
    public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        var response = await next(context);

        if (context.MethodPath == "ICalc/Sum")
        {
            // You can cast response type.
            var sumResult = await response.GetResponseAs<int>();
            Console.WriteLine("Called Sum, Result:" + sumResult);
        }

        return response;
    }
}

public class MockRequestFilter : IClientFilter
{
    public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        if (context.MethodPath == "ICalc/Sum")
        {
            // don't call next, return mock result.
            return new ResponseContext<int>(9999);
        }

        return await next(context);
    }
}

public class RetryFilter : IClientFilter
{
    public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        Exception lastException = null;
        var retryCount = 0;
        while (retryCount != 3)
        {
            try
            {
                // using same CallOptions so be careful to add duplicate headers or etc.
                return await next(context);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
            retryCount++;
        }

        throw new Exception("Retry failed", lastException);
    }
}

public class EncryptFilter : IClientFilter
{
    public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        context.SetRequestMutator(bytes => Encrypt(bytes));
        context.SetResponseMutator(bytes => Decrypt(bytes));
        
        return await next(context);
    }
}
```

### ServiceContext and Lifecycle
Service/StreamingHub's method or `MagicOnionFilter` can access `this.Context` it is 

| Property | Description |
| --- | --- |
| `ConcurrentDictionary<string, object>` Items | Object storage per request/connection. |
| `Guid` ContextId | Unieuq ID per request(Service)/connection(StreamingHub). |
| `DateTime` Timestamp | Timestamp that request/connection is started time. |
| `Type` ServiceType | Invoked Class. |
| `MethodInfo` MethodInfo | Invoked Method. |
| `ILookup<Type, Attribute> AttributeLookup | Cached Attributes that merged both service and method. |
| `ServerCallContext` CallContext | Raw gRPC Context. |
| `IFormatterResolver` FormatterResolver | Using MessagePack resolver. |
| `IServiceLocator` ServiceLocator | Get the registered service. |

`Items` is useful, for example authentication filter add UserId to Items and take out from service method.

> If using StreamingHub, ServiceContext means per connected context so `Items` is not per method invoke. `StreamingHubContext.Items` supports per streaming hub method request but currently can not take from streaming hub method(only use in StreamingHubFilter). [Issue:#67](https://github.com/Cysharp/MagicOnion/issues/67), it will fix.

MagicOnion supports get current context globaly like HttpContext.Current. `ServiceContext.Current` can get it but it requires `MagicOnionOptions.EnableCurrentContext = true`, default is false.

Lifecycle image of ServiceBase

```
gRPC In(
    var context = new ServiceContext();
    Filter.Invoke(context, 
        var service = new ServiceImpl();
        service.ServiceContext = context;
        service.MethodInvoke(
            /* method impl */
        )
    )
)
```

Lifecycle image of StreamingHub(StreamingHub is inherited from ServiceBase)

```
gRPC In(
    var context = new ServiceContext();
    Filter.Invoke(context, 
        var hub = new StreamingHubImpl();
        hub.ServiceContext = context;
        hub.Connect(
            while (connecting) {
                Streaming In(
                    var streamingHubContext = new StreamingHubContext(context);
                    StreamingHubFilter.Invoke(streamingHubContext,
                        hub.MethodInvoke(
                            /* method impl */
                        )
                    )
                )
            }
        )
    )
)
```

StreamingHub instance is shared while connecting so StreamingHub's field can use cache area of connection.

### ExceptionHandling and StatusCode
If you are return custom status code from server to client, you can use `throw new ReturnStatusException`.

```csharp
public Task SendMessageAsync(string message)
{
    if (message.Contains("foo"))
    {
        //
        throw new ReturnStatusException((Grpc.Core.StatusCode)99, "invalid");
    }

    // ....
```

Client can receive exception as gRPC's `RpcException`. If performance centric to avoid exception throw, you can use raw gRPC CallContext.Status(`ServiceContext.CallCaontext.Status`) and set status directly.

MagicOnion's engine catched exception(except ReturnStatusException), set `StatusCode.Unknown` and client received gRPC's `RpcException`. If `MagicOnionOption.IsReturnExceptionStackTraceInErrorDetail` is true, client can receive StackTrace of server exception, it is very useful for debugging but has critical issue about sercurity so should only to enable debug build.

### Group and GroupConfiguration
StreamingHub's broadcast system is called Group. It can get from StreamingHub impl method, `this.Group`(this.Group type is `HubGroupRepository`, not `IGroup`).

Current connection can add to group by `this.Group.AddAsync(string groupName)`, return value(`IGroup`) is joined group broadcaster so cache to field. It is enable per connection(if disconnected, automaticaly leaved from group). If you want to use some restriction, you can use `TryAddAsync(string groupName, int incluciveLimitCount, bool createIfEmpty)`.

`IGroup` can pass to StreamingHub.`Broadcast`, `BroadcastExceptSelf`, `BroadcastExcept` and calls client proxy.

```csharp
public class ChatHub : StreamingHubBase<IChatHub, IMessageReceiver>, IChatHub
{
    string userName;
    IGroup room;

    public async Task JoinAsync(string userName, string roomName)
    {
        this.userName = userName;
        this.room = await Group.AddAsync(roomName);
    }

    public async Task SendMessageAsync(string message)
    {
        Broadcast(room).OnReceiveMessage(userName, message);
    }
}
```

> GroupRepository is created per StreamingHub type

> If you want to create ServerSide loop and broadcast out of StreamingHub, you can pass Broadcast(room) result but it is unnatural, I'll add support kit of create server-side loop  

Group has in-memory storage, it can store extra data to group member. It can set `Group.AddAsync(string groupName, TStorage data)` instead of standard AddAsync.

Data is can get from `group.GetInMemoryStorage<T>` and can invoke `AllValues`, `Set(Guid connectionId, T Value)`, `Get(Guid connectionId)`.

> StreamingHub's ConnectionId is ServiceContext.ContextId

Default MagicOnion's group is inmemory and using `ImmutableArrayGroup`. This group implementation is tuned for small room, not enter/leave frequently. If large room and enter/leave frequently design, you can use `ConcurrentDictionaryGroup`. It can configure by `GroupConfigurationAttribute` or `MagicOnionOptions.DefaultGroupRepositoryFactory`.

```csharp
// use ***GroupRepositoryFactory type.
[GroupConfiguration(typeof(ConcurrentDictionaryGroupRepositoryFactory))]
public class ChatHub : StreamingHubBase<IChatHub, IMessageReceiver>, IChatHub
{
    // ...
}
```

MagicOnion has distribute system called redis-backplane for group broadcast.

![image](https://user-images.githubusercontent.com/46207/50974777-5f6aed00-152f-11e9-97f3-ba2a0c97f0eb.png)

```bash
dotnet add package MagicOnion.Server.Redis
```

```csharp
services.AddMagicOnion()
    .UseRedisGroupRepository(options =>
    {
        options.ConnectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
    });
    // If you want to use Redis backplane by default, you can specify `registerAsDefault: true`.
```
```csharp
// Use Redis as backplane
[GroupConfiguration(typeof(RedisGroupRepositoryFactory))]
public class ...
{
}
```

### Project Structure
If creates Server-Client project, I recommend make three projects. `Server`, `ServerDefinition`, `Client`.

![image](https://cloud.githubusercontent.com/assets/46207/21081857/e0f6dfce-c012-11e6-850d-358c5b928a82.png)

ServerDefinition is only defined interface(`IService<>`, `IStreamingHub<,>`)(and some share request/response types).

If debugging, I recommend use [SwitchStartupProject](https://marketplace.visualstudio.com/items?itemName=vs-publisher-141975.SwitchStartupProjectforVS2017) exteinson of VisualStudio and launch both Server and Client.

```json
"MultiProjectConfigurations": {
    "Server + Client": {
        "Projects": {
            "FooService": {},
            "FooClient": {}
        }
    }
}
```

It can step-in/out seamlessly in server and client.

for Unity, you can't share by DLL(because can't share `IServer<>` because it is different reference both Unity and Server). It is slightly complex so we provides sample project and explanation.

see: [samples](https://github.com/Cysharp/MagicOnion/tree/master/samples) page and ReadMe.


### Dependency Injection
You can use DI(constructor injection) on the server.

```csharp
public class MyFirstService : ServiceBase<IMyFirstService>, IMyFirstService
{
    IOptions<MyConfig> config;
    ILogger<MyFirstService> logger;

    public MyFirstService(IOptions<MyConfig> config, ILogger<MyFirstService> logger)
    {
        this.config = config;
        this.logger = logger;
    }

    // ...
}
```

## Server and Clients

### Unity client Supports

You can download `MagicOnion.Client.Unity.package` and `moc.zip`(MagicOnionCompiler) in the [releases page](https://github.com/cysharp/MagicOnion/releases). But MagicOnion has no dependency so download gRPC lib from [gRPC daily builds](https://packages.grpc.io/), click Build ID and download `grpc_unity_package.*.*.*-dev.zip`. One more, requires MessagePack for C# for serialization, you can download `MessagePack.Unity.*.*.*.unitypackage` from [MessagePack-CSharp/releases](https://github.com/neuecc/MessagePack-CSharp/releases).

MagicOnion only supports `.NET 4.x` runtime and recommend to supports C# 7.0(Unity 2018.3) version.

Default MagicOnion's Unity client works well on Unity Editor or not IL2CPP env. But for IL2CPP environment, you need client code generation. `moc` is cross-platform standalone application but it requires [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download).

```
argument list:
-i, -input: Input path of analyze csproj or directory.
-o, -output: Output path(file) or directory base(in separated mode).
-u, -unuseUnityAttr: [default=False]Unuse UnityEngine's RuntimeInitializeOnLoadMethodAttribute on MagicOnionInitializer.
-n, -namespace: [default=MagicOnion]Set namespace root name.
-c, -conditionalSymbol: [default=null]Conditional compiler symbols, split with ','.
```

You also need MessagePack-CSharp code generation, please see [MessagePack-CSharp AOT Code Generation (to support Unity/Xamarin)
](https://github.com/neuecc/MessagePack-CSharp#aot-code-generation-to-support-unityxamarin) section.

Other than download moc.zip, you can install `MagicOnion.Generator` as .NET Core Tools or use `MagicOnion.MSBuild.Tasks` to prebuild hook.

[.NET Core Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) is easiest way to use moc directly.

```
dotnet tool install --global MagicOnion.Generator
```

```
dotnet moc -h
```

`MagicOnion.MSBuild.Tasks` is easy way of generate code that target to shared project. We're mostly recommended to use this way. For example, PostCompile sample.

```csharp
<!-- in Shared.csproj -->

<ItemGroup>
    <!-- Install MSBuild Task(with PrivateAssets="All", it means to use dependency only in build time). -->
    <PackageReference Include="MessagePack.MSBuild.Tasks" Version="*" PrivateAssets="All" />
    <PackageReference Include="MagicOnion.MSBuild.Tasks" Version="*" PrivateAssets="All" />
</ItemGroup>

<!-- Call code generator after compile successfully. -->
<Target Name="GenerateMessagePack" AfterTargets="Compile">
    <MessagePackGenerator Input="$(ProjectPath)" Output="..\UnityClient\Assets\Scripts\Generated\MessagePack.Generated.cs" />
</Target>
<Target Name="GenerateMagicOnion" AfterTargets="Compile">
    <MagicOnionGenerator Input="$(ProjectPath)" Output="..\UnityClient\Assets\Scripts\Generated\MagicOnion.Generated.cs" />
</Target>
 ```

Full options are below.

```xml
<MagicOnionGenerator
    Input="string:required"
    Output="string:required"
    ConditionalSymbol="string:optional"
    ResolverName="string:optional"
    Namespace="string:optional"
    UnuseUnityAttr="bool:optional"
/>
```

Project structure and code generation samples are found in [samples](https://github.com/Cysharp/MagicOnion/tree/master/samples) directory and README.

## iOS build with gRPC
gRPC iOS build require two additional operation on build.

1. Disable Bitcode
1. Add libz.tbd

We introduce OnPostProcessingBuild sample [BuildIos.cs](https://github.com/Cysharp/MagicOnion/blob/master/samples/ChatApp/ChatApp.Unity/Assets/Editor/BuildeIos.cs) for ChatApp.Unity to automate these steps.

```csharp
#if UNITY_IPHONE
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class BuildIos
{
    /// <summary>
    /// Handle libgrpc project settings.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="path"></param>
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        var projectPath = PBXProject.GetPBXProjectPath(path);
        var project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));
        var targetGuid = project.TargetGuidByName(PBXProject.GetUnityTargetName());

        // libz.tbd for grpc ios build
        project.AddFrameworkToProject(targetGuid, "libz.tbd", false);

        // libgrpc_csharp_ext missing bitcode. as BITCODE exand binary size to 250MB.
        project.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
        
        File.WriteAllText(projectPath, project.WriteToString());
    }
}
#endif
```

## Stripping debug symbols from ios/libgrpc.a
When you download gRPC daily build and extract Native Libararies for Unity, you will find file size of Plugins/Grpc.Core/runtime/ios/libgrpc.a beyonds 100MB. GitHub will reject commit when file size is over 100MB, therefore libgrpc.a often become unwelcome for gif-low.
The reason of libgrpc.a file size is because it includes debug symbols for 3 architectures, arm64, armv7 and x86_64.

We introduce strip debug symbols and generate reduced size `libgrpc_stripped.a`, it's about 17MB.
This may useful for whom want commit `libgrpc.a` to GitHub, and understanding stripped library missing debug symbols.

**How to strip**

Download gRPC lib `grpc_unity_package.*.*.*-dev.zip` from [gRPC daily builds](https://packages.grpc.io/) and extract it, copy Plugins folder to Unity's Assets path.

Open terminal on `Plugins/Grpc.Core/runtimes/ios/` and execute following will generate `libgrpc_stripped.a` and replace original libgrpc.a with stripped version.

```shell
$ cd ${UNITY_PATH}/Plugins/Grpc.Core/runtimes/ios
$ strip -S -x libgrpc.a -o libgrpc_stripped.a
$ rm libgrpc.a && mv libgrpc_stripped.a libgrpc.a
```

Make sure you can build app with iOS and works fine.

## gRPC Keepalive

When you want detect network termination on Client or vice-versa, you can configure gRPC Keepalive.
It's nothing special, just follow to the [Keepalive UserGuide for gRPC Core](https://github.com/grpc/grpc/blob/master/doc/keepalive.md) but let's see how in actual.

MagicOnion offers two ways to configure gRPC Core Keepalive, `ChannelOption` and `MagicOnionOption`.

**ChannelOption**

ChannelOptions is primitive way to configure options.
Below uses `ChannelOption` and offer keepalive for every 10 second even RPC is not called.

```csharp
// If you want configure KEEP_ALIVE interval, then....
// * set same value for `grpc.keepalive_time_ms` and `grpc.http2.min_time_between_pings_ms`
// * keep `grpc.http2.min_ping_interval_without_data_ms < grpc.http2.min_time_between_pings_ms`
var options = new[]
{
    // send keepalive ping every 10 second, default is 2 hours
    new ChannelOption("grpc.keepalive_time_ms", 10000),
    // keepalive ping time out after 5 seconds, default is 20 seoncds
    new ChannelOption("grpc.keepalive_timeout_ms", 5000),
    // allow grpc pings from client every 10 seconds
    new ChannelOption("grpc.http2.min_time_between_pings_ms", 10000),
    // allow unlimited amount of keepalive pings without data
    new ChannelOption("grpc.http2.max_pings_without_data", 0),
    // allow keepalive pings when there's no gRPC calls
    new ChannelOption("grpc.keepalive_permit_without_calls", 1),
    // allow grpc pings from client without data every 5 seconds
    new ChannelOption("grpc.http2.min_ping_interval_without_data_ms", 5000),
};
```

Pass this options to Channel on Client, or to `IHostBuilder.UseMagicOnion` on Server will configure Keepalive.

```csharp
// Client
this.channel = new Channel("localhost", 12345, ChannelCredentials.Insecure, options);
```

Now you can detect client network disconnection on serverside, let's override `OnDisconnected` and set debugger, disconnect Client network and wait for interval sec!

## Deployment
MagicOnion is also supported in Docker containers and running on Kubernetes.

See [docs/articles/deployment/](docs/articles/deployment/) for information on deploying to Amazon Web Service and other cloud services.

## Integrations
### Swagger
MagicOnion has built-in HTTP/1.1 JSON Gateway and [Swagger](http://swagger.io/) integration for Unary operation. It can execute and debug RPC-API easily.

```bash
dotnet add package MagicOnion.Server.HttpGateway
```

```csharp
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();

        services.AddGrpc(); // MagicOnion depends on ASP.NET Core gRPC service.
        services.AddMagicOnion();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMagicOnionHttpGateway("_", app.ApplicationServices.GetService<MagicOnion.Server.MagicOnionServiceDefinition>().MethodHandlers, GrpcChannel.ForAddress("https://localhost:5001"));
            endpoints.MapMagicOnionSwagger("swagger", app.ApplicationServices.GetService<MagicOnion.Server.MagicOnionServiceDefinition>().MethodHandlers, "/_/");

            endpoints.MapMagicOnionService();
        });
    }
}
```

Open `http://localhost:5000`, you can see swagger view.

![image](https://cloud.githubusercontent.com/assets/46207/21295663/6a9d3e28-c59d-11e6-8081-18d14e359567.png)

## Advanced
### MagicOnionOption

`MagicOnionOption` can pass to `MagicOnionEngine.BuildServerServiceDefinition(MagicOnionOptions option)`.

| Property | Description |
| --- | --- |
| `IList<MagicOnionFilterDescriptor>` GlobalFilters | Global MagicOnion filters. |
| `bool` EnableCurrentContext | Enable ServiceContext.Current option by AsyncLocal, default is false. |
| `IList<StreamingHubFilterDescriptor>` Global StreamingHub filters. | GlobalStreamingHubFilters |
| `IGroupRepositoryFactory` DefaultGroupRepositoryFactory | Default GroupRepository factory for StreamingHub, default is ``. |
| `bool` IsReturnExceptionStackTraceInErrorDetail | If true, MagicOnion handles exception ownself and send to message. If false, propagate to gRPC engine. Default is false. |
| `MessagePackSerializerOptions` SerializerOptions | MessagePack serialization resolver. Default is used ambient default(MessagePackSerializer.DefaultOptions). |

### Internal Logging
`IMagicOnionLogger` is structured logger of MagicOnion internal information.

 Implements your custom logging code and append it, default is `NullMagicOnionLogger`(do nothing). MagicOnion has some built in logger, `MagicOnionLogToLogger` that structured log to string log and send to `Microsoft.Extensions.Logging.ILogger`. `MagicOnionLogToLoggerWithDataDump` is includes data dump it is useful for debugging(but slightly heavy, recommended to only use debugging). `MagicOnionLogToLoggerWithNamedDataDump` is more readable than simple WithDataDump logger.

### Raw gRPC APIs
MagicOnion can define and use primitive gRPC APIs(ClientStreaming, ServerStreaming, DuplexStreaming). Especially DuplexStreaming is used underlying StreamingHub. If there is no reason, we recommend using StreamingHub.

```csharp
// Definitions
public interface IMyFirstService : IService<IMyFirstService>
{
    UnaryResult<string> SumAsync(int x, int y);
    Task<UnaryResult<string>> SumLegacyTaskAsync(int x, int y);
    Task<ClientStreamingResult<int, string>> ClientStreamingSampleAsync();
    Task<ServerStreamingResult<string>> ServertSreamingSampleAsync(int x, int y, int z);
    Task<DuplexStreamingResult<int, string>> DuplexStreamingSampleAync();
}

// Server
public class MyFirstService : ServiceBase<IMyFirstService>, IMyFirstService
{
    // VisualStudio 2017(C# 7.0), Unity 2018.3 supports return `async UnaryResult` directly
    // I recommend disable async-warning on project level. <NoWarn>1998</NoWarn>
    public async UnaryResult<string> SumAsync(int x, int y)
    {
        Logger.Debug($"Called SumAsync - x:{x} y:{y}");

        return (x + y).ToString();
    }

    // VS2015(C# 6.0), Unity 2018.2 use Task
    public async Task<UnaryResult<string>> SumLegacyTaskAsync(int x, int y)
    {
        Logger.Debug($"Called SumAsync - x:{x} y:{y}");

        // use UnaryResult method.
        return UnaryResult((x + y).ToString());
    }

    public async Task<ClientStreamingResult<int, string>> ClientStreamingSampleAsync()
    {
        Logger.Debug($"Called ClientStreamingSampleAsync");

        // If ClientStreaming, use GetClientStreamingContext.
        var stream = GetClientStreamingContext<int, string>();

        // receive from client asynchronously
        await stream.ForEachAsync(x =>
        {
            Logger.Debug("Client Stream Received:" + x);
        });

        // StreamingContext.Result() for result value.
        return stream.Result("finished");
    }

    public async Task<ServerStreamingResult<string>> ServertSreamingSampleAsync(int x, int y, int z)
    {
        Logger.Debug($"Called ServertSreamingSampleAsync - x:{x} y:{y} z:{z}");

        var stream = GetServerStreamingContext<string>();

        var acc = 0;
        for (int i = 0; i < z; i++)
        {
            acc = acc + x + y;
            await stream.WriteAsync(acc.ToString());
        }

        return stream.Result();
    }

    public async Task<DuplexStreamingResult<int, string>> DuplexStreamingSampleAync()
    {
        Logger.Debug($"Called DuplexStreamingSampleAync");

        // DuplexStreamingContext represents both server and client streaming.
        var stream = GetDuplexStreamingContext<int, string>();

        var waitTask = Task.Run(async () =>
        {
            // ForEachAsync(MoveNext, Current) can receive client streaming.
            await stream.ForEachAsync(x =>
            {
                Logger.Debug($"Duplex Streaming Received:" + x);
            });
        });

        // WriteAsync is ServerStreaming.
        await stream.WriteAsync("test1");
        await stream.WriteAsync("test2");
        await stream.WriteAsync("finish");

        await waitTask;

        return stream.Result();
    }
}
```

Client sample.

```csharp
static async Task UnaryRun(IMyFirstService client)
{
    // await(C# 7.0, Unity 2018.3)
    var vvvvv = await client.SumAsync(10, 20);
    Console.WriteLine("SumAsync:" + vvvvv);
    
    // if use Task<UnaryResult>(Unity 2018.2), use await await
    var vvvv2 = await await client.SumLegacyTaskAsync(10, 20);
}

static async Task ClientStreamRun(IMyFirstService client)
{
    var stream = await client.ClientStreamingSampleAsync();

    for (int i = 0; i < 3; i++)
    {
        await stream.RequestStream.WriteAsync(i);
    }
    await stream.RequestStream.CompleteAsync();

    var response = await stream.ResponseAsync;

    Console.WriteLine("Response:" + response);
}

static async Task ServerStreamRun(IMyFirstService client)
{
    var stream = await client.ServertSreamingSampleAsync(10, 20, 3);

    await stream.ResponseStream.ForEachAsync(x =>
    {
        Console.WriteLine("ServerStream Response:" + x);
    });
}

static async Task DuplexStreamRun(IMyFirstService client)
{
    var stream = await client.DuplexStreamingSampleAync();

    var count = 0;
    await stream.ResponseStream.ForEachAsync(async x =>
    {
        Console.WriteLine("DuplexStream Response:" + x);

        await stream.RequestStream.WriteAsync(count++);
        if (x == "finish")
        {
            await stream.RequestStream.CompleteAsync();
        }
    });
}
```

### Zero deserialization mapping
In RPC, especially in real-time communication involving frequent transmission of data, it is often the serialization process where data is converted before being sent that limits the performance. In MagicOnion, serialization is done by my MessagePack for C#, which is the fastest binary serializer for C#, so it cannot be a limiting factor. Also, in addition to performance, it also provides flexibility regarding data in that variables of any type can be sent as long as they can be serialized by MessagePack for C#.

Also, taking advantage of the fact that both the client and the server run on C# and data stored on internal memory are expected to share the same layout, I added an option to do mapping through memory copy without serialization/deserialization in case of a value-type variable.

Especially in Unity, this is can combinate with `MessagePack.UnityShims` package of NuGet.

```csharp
// It supports standard struct-type variables that are provided by Unity, such as Vector3, and arrays containing them, as well as custom struct-type variables and their arrays.
// I recommend doing this explicitly using [StructLayout(LayoutKind.Explicit)] to accurately match the size.
public struct CustomStruct
{
    public long Id;
    public int Hp;
    public int Mp;
    public byte Status;
}
 
// ---- Register the following code when initializing.
 
// By registering it, T and T[] can be handled using zero deserialization mapping.
UnsafeDirectBlitResolver.Register<CustomStruct>();
 
// The struct-type above as well as Unity-provided struct-types (Vector2, Rect, etc.), and their arrays are registered as standards.
CompositeResolver.RegisterAndSetAsDefault(
    UnsafeDirectBlitResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitResolver.Instance
    );
 
// --- Now the communication will be in the format above when they are used for transmission.
await client.SendAsync(new CustomStruct { Hp = 99 });
```

Nothing needs to be processed here, so it promises the best performance theoretically possible in terms of transmission speed. However, since these struct-type variables need to be copied, I recommend handling everything as ref as a rule when you need to define a large struct-type, or it might slow down the process.

I believe that this can be easily and effectively applied to sending a large number of Transforms, such as an array of Vector3 variables.

## Experimentals
### OpenTelemetry
MagicOnion.OpenTelemetry is implementation of [open\-telemetry/opentelemetry\-dotnet: OpenTelemetry \.NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet), so you can use any OpenTelemetry exporter, like [Prometheus](https://prometheus.io/), [StackDriver](https://cloud.google.com/stackdriver/pricing), [Zipkin](https://zipkin.io/) and others.

See details at [MagicOnion.Server.OpenTelemetry](src/MagicOnion.Server.OpenTelemetry)

## Author Info
This library is mainly developed by Yoshifumi Kawai(a.k.a. neuecc).  
He is the CEO/CTO of Cysharp which is a subsidiary of [Cygames](https://www.cygames.co.jp/en/).  
He is awarding Microsoft MVP for Developer Technologies(C#) since 2011.  
He is known as the creator of [UniRx](https://github.com/neuecc/UniRx/) and [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp/).

## License
This library is under the MIT License.
