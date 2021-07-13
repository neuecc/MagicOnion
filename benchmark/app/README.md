# Benchmarker

There are 3 server implementaion projects.

* dotnet_grpc_bench: gRPC server implemeted for both gRPC and MagicOnion.
* dotnet_grpc_https_bench: Self Cert gRPC server implemeted for both gRPC and MagicOnion.
* dotnet_api_bench: REST server implemented with ASP.NET Core API.

You can benchmark these implementaion with `C# Benchmarker` or `grpc_bench`.

## C# Benchmarker

C# implemented benchmarker for all server implementations `dotnet_grpc_bench`, `dotnet_grpc_https_bench` and `dotnet_api_bench`.

### Prerequisites

Linux or Windows or MacOS or All platform with Docker. Keep in mind that the results on MacOS/Windows may not be that reliable, Docker for Mac/Windows runs on a VM.

### Running benchmark on Docker

* To build the benchmarks images use following. You need them to run the benchmarks.
  * linux: `./build.sh [BENCH1] [BENCH2]`
  * windows: `./build.ps1 [BENCH1] [BENCH2]`

To run the benchmarks use following. They will be run sequentially.
  * linux: `./bench.sh [BENCH1] [BENCH2]`
  * windows: `./bench.ps1 [BENCH1] [BENCH2]`

To clean-up the benchmark images use following.
  * linux: `./clean.sh [BENCH1] [BENCH2]`.
  * windows: not supported.

> TIPS: to change benchclient command, write command to `bench_command` file.

## grpc_bench

[grpc_bench](https://github.com/LesnyRumcajs/grpc_bench) is comminuty run benchmark of different gRPC server implemetaions.
It benchmark to gRPC implementation though Proto scheme.

We use this benchmark to compare out Benchmarker, this identify Server implementation misstake or Client Benchmarker misstake.

### Prerequisites

Windows or Linux or MacOS with Docker. Keep in mind that the results on MacOS/Windows may not be that reliable, Docker for Mac/Windows runs on a VM.

### Running benchmark

* To build the benchmarks images use following. You need them to run the benchmarks.
  * linux: `./ghz_build.sh [BENCH1] [BENCH2]`
  * windows: `./ghz_build.ps1 [BENCH1] [BENCH2]`

To run the benchmarks use following. They will be run sequentially.
  * linux: `./ghz_bench.sh [BENCH1] [BENCH2]`
  * windows: `./ghz_bench.ps1 [BENCH1] [BENCH2]`

To clean-up the benchmark images use following.
  * linux: `./ghz_clean.sh [BENCH1] [BENCH2]`.
  * windows: not supported.

## TIPS

### [DO] Configuring the benchmark

The benchmark can be configured through the following environment variables:

|**Name**|**Description**|**Default value**|
|--------|---------------|:---------------:|
|GRPC_BENCHMARK_DURATION|Duration of the benchmark.|30s|
|GRPC_SERVER_CPUS|Maximum number of cpus used by the server.|1|
|GRPC_CLIENT_CONNECTIONS|Number of connections to use.|5|
|GRPC_CLIENT_CONCURRENCY|Number of requests to run concurrently. It can't be smaller than the number of connections.|50|
|GRPC_CLIENT_CPUS|Maximum number of cpus used by the client.|1|

### [DO] Parameter recommendations

* `GRPC_BENCHMARK_DURATION` should not be too small. Some implementations need a *warm-up* before achieving their optimal performance and most real-life gRPC services are expected to be long running processes. From what we measured, **300s** should be enough.
* `GRPC_SERVER_CPUS` + `GRPC_CLIENT_CPUS` should not exceed total number of cores on the machine. The reason for this is that you don't want the `ghz` client to steal precious CPU cycles from the service under test. Keep in mind that having the `GRPC_CLIENT_CPUS` too low may not saturate the service in some of the more performant implementations. Also keep in mind limiting the number of `GRPC_SERVER_CPUS` to 1 will severely hamper the performance for some technologies - is running a service on 1 CPU your use case? It may be, but keep in mind eventual load balancer also incurs some costs.

Other parameters will depend on your use-case. Choose wisely.

### [DO NOT] Run your benchmarker from Visual Studio

Running benchmarker on Visual Studio will drop RPS particularly.
Please publish your benchmarker binary and use it.
`Release` build will increase +10%-20% performance gain then `Debug` build.

```
dotnet publish Benchmark.Client -c Release
```

### [DO] Run your benchmark server on Visual Studio

Running benchmark server on Visual Studio cause small drop of RPS.
It still show you 25000rps for both MagicOnion and gRPC.

> What you must take case is benchmarker, not benchmark server.

### [CONSIDER] Configure Benchmarker Concurrency and Connections parameters impacts performance

Benchmarker has 2 importance parameters, `-concurrency` and `-connections`.
These 2 parameter has direct performance impact and it depends on your Machine.

* `-concurrency`: Benchmarker internal worker concurrency, default 50. Increase this parameter will gain performance if CPU, Thread has magin.
* `-connections`: Benchmarker internal gRPC connection reuse, default 30. You can not set larger then concurrency. 20-50 will be suitable for many cases.

### [DO NOT] Use localhost for hostaddress parameter when target is local machine http server

You should use `127.0.0.1:PORT` to benchmark http server on local machine.

Specifing `-hostaddress localhost:PORT` will try IPv6 connection then fall back to IPv4 when failed.
When this occurd, slowest latency will be more then 1000ms, and RPS drops performance.
If you doubt this happen on you environment, use Wireshark on loop back address.

> TIPS: If your server is `https` use `localhost` instead of `127.0.0.1`.
