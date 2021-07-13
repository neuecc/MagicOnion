# Benchmark

This is MagicOnion and gRPC Benchmark project.
You can run benchmark with `GitHub Actions`, `C# Benchmarker`, `grpc_bench`, `AWS EC2` and `AWS EC2 and ECS`
If you want run your benchmark on AWS EC2 VM, use AWS CDK.

* Benchmark apps are locate under `./app`.
* AWS CDK Benchmark Project is locate under `./Cdk`.

## Benchmark Servers

There are 3 servers to compare difference.

* dotnet_grpc_bench: gRPC server implemeted with both gRPC and MagicOnion.
* dotnet_grpc_https_bench: Self Cert gRPC server implemeted with both gRPC and MagicOnion.
* dotnet_api_bench: REST server implemented with ASP.NET Core API.
VM.

All Server implementations logic is same for each style, recieve message, deserialize and return response.
This identify each server performance changes.

## Run benchmark on docker

We offer docker to benchmark on local or any env.

Are you using macOS/Linux? Run bench on Bash.

```sh
git clone https://github.com/Cysharp/MagicOnion.git
cd MagicOnion/benchmark-lab/benchmark/app
bash ./build.sh
bash ./bench.sh
```

Are you using Windows? Run bench via PowerShell.

```sh
git clone https://github.com/Cysharp/MagicOnion.git
cd MagicOnion/benchmark-lab/benchmark/app
./build.ps1
./bench.ps1
```

## Run benchmark on AWS EC2

We offer AWS CDK project building you ec2 benchmark environment and prepare docker and binary on it.
Deploy CDK will make VM for you, login to ec2 and run your benchmark.

> see detail [README](CdkEc2Bench/README.md)

## Run benchmark on AWS EC2 & Amazon ECS

We offer AWS CDK project building you ec2 & run benchmarker from ECS.
Build binary and deploy CDK will begin benchmark and complete it, you can check result on HTML Report.

> see detail [README.CDK.md](README.CDK.md)

## Benchmarkers

There are 2 benchmarkers, C# Benchmarker and grpc_bench.
This project's benchmark is obtain via C# Benchmarker.

> see detail [README.md](app/README.md)

### C# Benchmarker

C# Benchmarker is minimum implementation with following what comminuty run benchmark do.
It benchmark to MagicOnion, gRPC and REST API implementaion through C# code sharing.

We use this benchmark to compare out MagicOnion and gRPC performance changes.

### grpc_bench Benchmarker

[grpc_bench](https://github.com/LesnyRumcajs/grpc_bench) is comminuty run benchmark of different gRPC server implemetaions.
It benchmark to gRPC implementation though Proto scheme.

We use this benchmark to compare out Benchmarker, this identify Server implementation misstake or Client Benchmarker misstake.

