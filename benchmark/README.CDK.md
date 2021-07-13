# Welcome to your CDK C# project!

This is a MagicOnion benchmark project with CDK.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET Core CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Step to deploy

install cdk cli.

```shell
npm install -g aws-cdk
npm update -g aws-cdk
```

build and deploy

```shell
# build servers binary
dotnet publish app/dotnet_grpc_bench/ -c Release -o out/linux/server/Benchmark.Server -r linux-x64 -p:PublishSingleFile=true --no-self-contained
dotnet publish app/dotnet_grpc_https_bench/ -c Release -o out/linux/server/Benchmark.Server.Https -r linux-x64 -p:PublishSingleFile=true --no-self-contained
dotnet publish app/dotnet_api_bench/ -c Release -o out/linux/server/Benchmark.Server.Api -r linux-x64 -p:PublishSingleFile=true --no-self-contained
# deploy via CDK
cdk synth
cdk bootstrap # only on initial execution
cdk deploy
```

## Deploy TIPS

* Use Datadog to monitor benchmark ec2 and fargate metrics.

CDK template use AWS SecretsManager to keep datadog token.
First, create datadog token secret with secret-id `magiconion-benchmark-datadog-token` via aws cli.

```shell
SECRET_ID=magiconion-benchmark-datadog-token
DD_TOKEN=abcdefg12345
aws secretsmanager create-secret --name "$SECRET_ID"
aws secretsmanager put-secret-value --secret-id "$SECRET_ID" --secret-string "${DD_TOKEN}"
```

Confirm token is successfully set to secrets manager.

```shell
aws secretsmanager describe-secret --secret-id "$SECRET_ID"
aws secretsmanager get-secret-value --secret-id "$SECRET_ID"
```

To install Datadog agent to ec2 or fargate, set `true` in `ReportStackProps` Property.
EC2 MagicOnion also support install CloudWatch Agent, this agent will collect Mem used and TCP status.

```csharp
new ReportStackProps
{
    UseEc2DatadogAgentProfiler = true, // install datadog agent to MagicOnion Ec2.
    UseFargateDatadogAgentProfiler = true, // instance datadog fargate agent to bench master/worker.
    UseEc2CloudWatchAgentProfiler = false, // true to install cloudwatch agent to magiconion ec2
}
```

## Destroy TIPS

* cdk destoy failed because instance remain on service discovery.

use script to remove all instances from service discovery.

```csharp
async Task Main()
{
    var serviceName = "server";
    var client = new Amazon.ServiceDiscovery.AmazonServiceDiscoveryClient();
    var services = await client.ListServicesAsync(new ListServicesRequest());
    var service = services.Services.First(x => x.Name == serviceName);
    var instances = await client.ListInstancesAsync(new ListInstancesRequest
    {
        ServiceId = service.Id,
    });
    foreach (var instance in instances.Instances)
    {
        await client.DeregisterInstanceAsync(new DeregisterInstanceRequest
        {
            InstanceId = instance.Id,
            ServiceId = service.Id,
        });
    }
}
```
