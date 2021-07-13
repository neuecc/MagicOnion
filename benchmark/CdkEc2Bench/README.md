# Welcome to your CDK C# project!

This is a CDK project to create EC2 instance to run benchmark.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET Core CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Step to deploy

install cdk cli.

```shell
npm install -g aws-cdk
npm update -g aws-cdk
```

deploy cdk.

```shell
cdk synth
cdk bootstrap # only on initial execution
cdk deploy
```

## After cdk deployed

login to ec2 via Session Manager.

```shell
aws ssm start-session --target $(aws ec2 describe-instances --filter "Name=tag-key,Values=Name" "Name=tag-value,Values=GrpcBenchmarkStack/instances" "Name=instance-state-name,Values=running" --query "Reservations[].Instances[].InstanceId" --output text)
```

make sure you can run docker.

```shell
docker run --rm hello-world
```

run command to bench.

```sh
cd ~
git clone https://github.com/Cysharp/MagicOnion.git
cd MagicOnion/benchmark-lab/benchmark/app
bash ./build.sh
bash ./bench.sh
```

benchmark for following to keep in Wiki,

```sh
bash ./build.sh
GRPC_SERVER_CPUS=1 GRPC_CLIENT_CPUS=8 GRPC_CLIENT_CONCURRENCY=50 GRPC_CLIENT_CONNECTIONS=5 bash ./bench.sh
GRPC_SERVER_CPUS=4 GRPC_CLIENT_CPUS=12 GRPC_CLIENT_CONCURRENCY=100 GRPC_CLIENT_CONNECTIONS=5 bash ./bench.sh
```

