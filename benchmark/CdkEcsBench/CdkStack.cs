using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.ServiceDiscovery;
using Amazon.CDK.CustomResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cdk
{
    public class CdkStack : Stack
    {
        internal CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var stackProps = ReportStackProps.ParseOrDefault(props);
            var dframeWorkerLogGroup = "MagicOnionBenchWorkerLogGroup";
            var dframeMasterLogGroup = "MagicOnionBenchMasterLogGroup";
            var benchNetwork = stackProps.GetBenchNetwork();
            var recreateMagicOnionTrigger = stackProps.GetBenchmarkServerBinariesHash();

            // s3
            var s3 = new Bucket(this, "Bucket", new BucketProps
            {
                AutoDeleteObjects = true,
                RemovalPolicy = RemovalPolicy.DESTROY,
                AccessControl = BucketAccessControl.PRIVATE,
            });
            var lifecycleRule = new LifecycleRule
            {
                Enabled = true,
                Prefix = "reports/",
                Expiration = Duration.Days(stackProps.DaysKeepReports),
                AbortIncompleteMultipartUploadAfter = Duration.Days(1),
            };
            s3.AddLifecycleRule(lifecycleRule);
            s3.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Sid = "AllowPublicRead",
                Effect = Effect.ALLOW,
                Principals = new[] { new AnyPrincipal() },
                Actions = new[] { "s3:GetObject*" },
                Resources = new[] { $"{s3.BucketArn}/html/*" },
            }));
            s3.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Sid = "AllowAwsAccountAccess",
                Effect = Effect.ALLOW,
                Principals = new[] { new AccountRootPrincipal() },
                Actions = new[] { "s3:*" },
                Resources = new[] { $"{s3.BucketArn}/*" },
            }));

            // s3 deploy
            var masterDllDeployment = new BucketDeployment(this, "DeployMasterDll", new BucketDeploymentProps
            {
                DestinationBucket = s3,
                Sources = new[] { Source.Asset(Path.Combine(Directory.GetCurrentDirectory(), $"out/linux/server")) },
                DestinationKeyPrefix = $"assembly/linux/server"
            });
            var userdataDeployment = new BucketDeployment(this, "UserData", new BucketDeploymentProps
            {
                DestinationBucket = s3,
                Sources = new[] { Source.Asset(Path.Combine(Directory.GetCurrentDirectory(), "userdata/")) },
                DestinationKeyPrefix = "userdata/"
            });

            // docker deploy
            var dockerImage = new DockerImageAsset(this, "dframeWorkerImage", new DockerImageAssetProps
            {
                Directory = Path.Combine(Directory.GetCurrentDirectory(), "app"),
                File = "ConsoleAppEcs/Dockerfile.Ecs",
            });
            var dframeImage = ContainerImage.FromDockerImageAsset(dockerImage);

            // network
            var vpc = new Vpc(this, "Vpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 0,
                SubnetConfiguration = new[] { new SubnetConfiguration { Name = "public", SubnetType = SubnetType.PUBLIC } },
            });
            var allsubnets = new SubnetSelection { Subnets = vpc.PublicSubnets };
            var singleSubnets = new SubnetSelection { Subnets = new[] { vpc.PublicSubnets.First() } };
            var sg = new SecurityGroup(this, "MasterSg", new SecurityGroupProps
            {
                AllowAllOutbound = true,
                Vpc = vpc,
            });
            foreach (var subnet in vpc.PublicSubnets)
                sg.AddIngressRule(Peer.Ipv4(vpc.VpcCidrBlock), Port.AllTcp(), "VPC", true);

            // service discovery
            var serviceDiscoveryDomain = "local";
            var serverMapName = "server";
            var dframeMapName = "dframe-master";
            var ns = new PrivateDnsNamespace(this, "Namespace", new PrivateDnsNamespaceProps
            {
                Vpc = vpc,
                Name = serviceDiscoveryDomain,
            });
            var serviceDiscoveryServer = ns.CreateService("server", new DnsServiceProps
            {
                Name = serverMapName,
                DnsRecordType = DnsRecordType.A,
                RoutingPolicy = RoutingPolicy.MULTIVALUE,
            });

            // alb
            var albDnsName = "benchmark-alb";
            var benchToMagicOnionDnsName = benchNetwork.RequireAlb
                ? $"{benchNetwork.EndpointScheme}://{albDnsName}.{stackProps.AlbDomain.domain}"
                : $"{benchNetwork.EndpointScheme}://{serverMapName}.{serviceDiscoveryDomain}";
            IApplicationTargetGroup grpcTargetGroup = null;
            IApplicationTargetGroup httpsTargetGroup = null;
            if (benchNetwork.RequireAlb)
            {
                // route53
                var hostedZone = HostedZone.FromHostedZoneAttributes(this, "HostedZone", new HostedZoneAttributes
                {
                    HostedZoneId = stackProps.AlbDomain.zoneId,
                    ZoneName = stackProps.AlbDomain.domain,
                });

                // acm
                var certificate = new DnsValidatedCertificate(this, "certificate", new DnsValidatedCertificateProps
                {
                    DomainName = $"{albDnsName}.{hostedZone.ZoneName}",
                    HostedZone = hostedZone,
                });
                // alb
                var lb = new ApplicationLoadBalancer(this, "LB", new ApplicationLoadBalancerProps
                {
                    Vpc = vpc,
                    VpcSubnets = allsubnets,
                    SecurityGroup = new SecurityGroup(this, "AlbSg", new SecurityGroupProps
                    {
                        AllowAllOutbound = true,
                        Vpc = vpc,
                    }),
                    InternetFacing = false,
                    Http2Enabled = true,
                });
                grpcTargetGroup = AddGrpcTargetGroup(benchNetwork, vpc, certificate, lb);
                httpsTargetGroup = AddHttpsTargetGroup(benchNetwork, vpc, certificate, lb);

                // Dns Record
                _ = new CnameRecord(this, "alb-alias-record", new CnameRecordProps
                {
                    RecordName = $"{albDnsName}.{stackProps.AlbDomain.domain}",
                    Ttl = Duration.Seconds(60),
                    Zone = hostedZone,
                    DomainName = lb.LoadBalancerDnsName,
                });
            }

            // iam
            var iamEc2MagicOnionRole = GetIamEc2MagicOnionRole(s3, serviceDiscoveryServer);
            var iamEcsTaskExecuteRole = GetIamEcsTaskExecuteRole(new[] { dframeWorkerLogGroup, dframeMasterLogGroup });
            var iamDFrameTaskDefRole = GetIamEcsDframeTaskDefRole(s3);
            var iamWorkerTaskDefRole = GetIamEcsWorkerTaskDefRole(s3);

            // secrets
            var ddToken = stackProps.UseEc2DatadogAgentProfiler || stackProps.UseFargateDatadogAgentProfiler
                ? Amazon.CDK.AWS.SecretsManager.Secret.FromSecretNameV2(this, "dd-token", "magiconion-benchmark-datadog-token")
                : null;

            // MagicOnion
            var asg = new AutoScalingGroup(this, "MagicOnionAsg", new AutoScalingGroupProps
            {
                // Monitoring is default DETAILED.
                SpotPrice = "1.0", // 0.0096 for spot price average for m3.medium
                Vpc = vpc,
                SecurityGroup = sg,
                VpcSubnets = singleSubnets,
                InstanceType = stackProps.MagicOnionInstanceType,
                DesiredCapacity = 1,
                MaxCapacity = 1,
                MinCapacity = 0,
                AssociatePublicIpAddress = true,
                MachineImage = new AmazonLinuxImage(new AmazonLinuxImageProps
                {
                    CpuType = AmazonLinuxCpuType.X86_64,
                    Generation = AmazonLinuxGeneration.AMAZON_LINUX_2,
                    Storage = AmazonLinuxStorage.GENERAL_PURPOSE,
                    Virtualization = AmazonLinuxVirt.HVM,
                }),
                AllowAllOutbound = true,
                GroupMetrics = new[] { GroupMetrics.All() },
                Role = iamEc2MagicOnionRole,
                UpdatePolicy = UpdatePolicy.ReplacingUpdate(),
                Signals = Signals.WaitForCount(1, new SignalsOptions
                {
                    Timeout = Duration.Minutes(10),
                }),
            });
            asg.AddSecretsReadGrant(ddToken, () => stackProps.UseEc2DatadogAgentProfiler);
            var userdata = GetUserData(recreateMagicOnionTrigger, s3.BucketName, stackProps.BenchmarkBinaryNames, serviceDiscoveryServer.ServiceId, stackProps.UseEc2CloudWatchAgentProfiler, stackProps.UseEc2DatadogAgentProfiler);
            asg.AddUserData(userdata);
            asg.UserData.AddSignalOnExitCommand(asg);
            asg.Node.AddDependency(masterDllDeployment);
            asg.Node.AddDependency(userdataDeployment);
            if (stackProps.EnableMagicOnionScaleInCron)
            {
                asg.ScaleOnSchedule("ScheduleOut", new BasicScheduledActionProps
                {
                    DesiredCapacity = 1,
                    MaxCapacity = 1,
                    // AM9:00 (JST+9) on Monday to Wednesday
                    Schedule = Schedule.Expression("0 0 * 1-3 *"),
                });
                asg.ScaleOnSchedule("ScheduleIn", new BasicScheduledActionProps
                {
                    DesiredCapacity = 0,
                    MaxCapacity = 0,
                    // PM9:00 (JST+9) on Everyday
                    Schedule = Schedule.Expression("0 12 * 1-7 *"),
                });
            }
            if (benchNetwork.RequireAlb)
            {
                asg.AttachToApplicationTargetGroup(grpcTargetGroup);
                asg.AttachToApplicationTargetGroup(httpsTargetGroup);
            }

            // ECS
            var cluster = new Cluster(this, "WorkerCluster", new ClusterProps
            {
                Vpc = vpc,
            });
            cluster.Node.AddDependency(asg); // wait until asg is up

            // dframe-worker
            var dframeWorkerContainerName = "worker";
            var dframeWorkerTaskDef = new FargateTaskDefinition(this, "DFrameWorkerTaskDef", new FargateTaskDefinitionProps
            {
                ExecutionRole = iamEcsTaskExecuteRole,
                TaskRole = iamWorkerTaskDefRole,
                Cpu = stackProps.WorkerFargate.CpuSize,
                MemoryLimitMiB = stackProps.WorkerFargate.MemorySize,
            });
            dframeWorkerTaskDef.AddContainer(dframeWorkerContainerName, new ContainerDefinitionOptions
            {
                Image = dframeImage,
                Command = new[] { "--worker-flag" },
                Environment = new Dictionary<string, string>
                {
                    { "DFRAME_MASTER_CONNECT_TO_HOST", $"{dframeMapName}.{serviceDiscoveryDomain}"},
                    { "DFRAME_MASTER_CONNECT_TO_PORT", "12345"},
                    { "BENCH_SERVER_HOST", benchToMagicOnionDnsName },
                    { "BENCH_REPORTID", stackProps.ReportId },
                    { "BENCH_S3BUCKET", s3.BucketName },
                },
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = new LogGroup(this, "WorkerLogGroup", new LogGroupProps
                    {
                        LogGroupName = dframeWorkerLogGroup,
                        RemovalPolicy = RemovalPolicy.DESTROY,
                        Retention = RetentionDays.TWO_WEEKS,
                    }),
                    StreamPrefix = dframeWorkerLogGroup,
                }),
            });
            dframeWorkerTaskDef.AddDatadogContainer($"{dframeWorkerContainerName}-datadog", ddToken, () => stackProps.UseFargateDatadogAgentProfiler);
            var dframeWorkerService = new FargateService(this, "DFrameWorkerService", new FargateServiceProps
            {
                ServiceName = "DFrameWorkerService",
                DesiredCount = 0,
                Cluster = cluster,
                TaskDefinition = dframeWorkerTaskDef,
                VpcSubnets = singleSubnets,
                SecurityGroups = new[] { sg },
                PlatformVersion = FargatePlatformVersion.VERSION1_4,
                MinHealthyPercent = 0,
                AssignPublicIp = true,
            });

            // dframe-master
            var dframeMasterTaskDef = new FargateTaskDefinition(this, "DFrameMasterTaskDef", new FargateTaskDefinitionProps
            {
                ExecutionRole = iamEcsTaskExecuteRole,
                TaskRole = iamDFrameTaskDefRole,
                Cpu = stackProps.MasterFargate.CpuSize,
                MemoryLimitMiB = stackProps.MasterFargate.MemorySize,
            });
            dframeMasterTaskDef.AddContainer("dframe", new ContainerDefinitionOptions
            {
                Image = dframeImage,
                Environment = new Dictionary<string, string>
                {
                    { "DFRAME_CLUSTER_NAME", cluster.ClusterName },
                    { "DFRAME_MASTER_SERVICE_NAME", "DFrameMasterService" },
                    { "DFRAME_WORKER_CONTAINER_NAME", dframeWorkerContainerName },
                    { "DFRAME_WORKER_SERVICE_NAME", dframeWorkerService.ServiceName },
                    { "DFRAME_WORKER_TASK_NAME", Fn.Select(1, Fn.Split("/", dframeWorkerTaskDef.TaskDefinitionArn)) },
                    { "DFRAME_WORKER_IMAGE", dockerImage.ImageUri },
                    { "BENCH_REPORTID", stackProps.ReportId },
                    { "BENCH_S3BUCKET", s3.BucketName },
                },
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    LogGroup = new LogGroup(this, "MasterLogGroup", new LogGroupProps
                    {
                        LogGroupName = dframeMasterLogGroup,
                        RemovalPolicy = RemovalPolicy.DESTROY,
                        Retention = RetentionDays.TWO_WEEKS,
                    }),
                    StreamPrefix = dframeMasterLogGroup,
                }),
            });
            dframeMasterTaskDef.AddDatadogContainer($"dframe-datadog", ddToken, () => stackProps.UseFargateDatadogAgentProfiler);
            var dframeMasterService = new FargateService(this, "DFrameMasterService", new FargateServiceProps
            {
                ServiceName = "DFrameMasterService",
                DesiredCount = 1,
                Cluster = cluster,
                TaskDefinition = dframeMasterTaskDef,
                VpcSubnets = singleSubnets,
                SecurityGroups = new[] { sg },
                PlatformVersion = FargatePlatformVersion.VERSION1_4,
                MinHealthyPercent = 0,
                AssignPublicIp = true,
            });
            dframeMasterService.EnableCloudMap(new CloudMapOptions
            {
                CloudMapNamespace = ns,
                Name = dframeMapName,
                DnsRecordType = DnsRecordType.A,
                DnsTtl = Duration.Seconds(300),
            });

            // output
            new CfnOutput(this, "ReportUrl", new CfnOutputProps { Value = $"https://{s3.BucketRegionalDomainName}/html/{stackProps.ReportId}/index.html" });
            new CfnOutput(this, "EndPointStyle", new CfnOutputProps { Value = stackProps.BenchmarkEndpoint.ToString() });
            new CfnOutput(this, "AsgName", new CfnOutputProps { Value = asg.AutoScalingGroupName });
            new CfnOutput(this, "EcsClusterName", new CfnOutputProps { Value = cluster.ClusterName });
            new CfnOutput(this, "DFrameWorkerEcsTaskdefImage", new CfnOutputProps { Value = dockerImage.ImageUri });
        }

        private IApplicationTargetGroup AddHttpsTargetGroup(BenchNetwork benchNetwork, Vpc vpc, DnsValidatedCertificate certificate, ApplicationLoadBalancer lb)
        {
            var targetGroup = new ApplicationTargetGroup(this, $"{StackName}-https-target-group", new ApplicationTargetGroupProps
            {
                Port = benchNetwork.AlbHttpsPort.targetgroupPort,
                Protocol = ApplicationProtocol.HTTP,
                Vpc = vpc,
                TargetType = TargetType.INSTANCE,
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Enabled = true,
                    Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                    HealthyThresholdCount = 2,
                    Interval = Duration.Seconds(15),
                    Timeout = Duration.Seconds(10),
                    Path = "/health",
                },
                DeregistrationDelay = Duration.Seconds(30),
            });
            var listener = lb.AddListener("HttpsListener", new BaseApplicationListenerProps
            {
                Port = benchNetwork.AlbHttpsPort.listenerPort,
                Protocol = ApplicationProtocol.HTTPS,
                Certificates = new[] { new ListenerCertificate(certificate.CertificateArn) },
            });
            listener.AddTargetGroups("HttpsTargetGroupAttachment", new AddApplicationTargetGroupsProps
            {
                TargetGroups = new[] { targetGroup },
            });
            return targetGroup;
        }

        private IApplicationTargetGroup AddGrpcTargetGroup(BenchNetwork benchNetwork, Vpc vpc, DnsValidatedCertificate certificate, ApplicationLoadBalancer lb)
        {
            var grpcTargetGroupResource = CreateGrpcTargetGroup(vpc, new Dictionary<string, object>()
                {
                    { "Name", "MagicOnionBench-grpc-target" },
                    { "Port", benchNetwork.AlbGrpcPort.targetgroupPort },
                    { "Protocol", ApplicationProtocol.HTTP.ToString() },
                    { "ProtocolVersion", "GRPC" },
                    { "VpcId", vpc.VpcId },
                    { "TargetType", "instance"},
                    { "HealthCheckEnabled", true },
                    { "HealthCheckProtocol", Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP.ToString() },
                    { "HealthyThresholdCount", 2 },
                    { "HealthCheckIntervalSeconds", 15 },
                    { "HealthCheckTimeoutSeconds", 10 },
                    { "HealthCheckPath", "/grpc.health.v1.Health/Check" },
                    { "Matcher", new Dictionary<string, string> { {"GrpcCode", "0-99"} } },
                });
            var targetGroup = ApplicationTargetGroup.FromTargetGroupAttributes(this, "grpc-target-group", new TargetGroupAttributes
            {
                TargetGroupArn = grpcTargetGroupResource.Ref,
            });
            var listener = lb.AddListener("GrpcListener", new BaseApplicationListenerProps
            {
                Port = benchNetwork.AlbGrpcPort.listenerPort,
                Protocol = ApplicationProtocol.HTTPS,
                Certificates = new[] { new ListenerCertificate(certificate.CertificateArn) },
            });
            listener.AddTargetGroups("GrpcTargetGroupAttachment", new AddApplicationTargetGroupsProps
            {
                TargetGroups = new[] { targetGroup },
            });
            listener.Node.AddDependency(grpcTargetGroupResource);

            return targetGroup;
        }

        private CustomResource CreateGrpcTargetGroup(Vpc vpc, Dictionary<string, object> properties)
        {
            // https://github.com/intercept6/example-aws-cdk-custom-resource
            // CustomResource Lambda for TargetGroup support ProtocolVersion Grpc
            var targetGroupEventHandler = new SingletonFunction(this, "grpc-targetgroup", new SingletonFunctionProps
            {
                Uuid = "4ddd3cf8-0a1b-43ee-994e-c15a2ffe1bd2",
                Code = Code.FromAsset(Path.Combine(Directory.GetCurrentDirectory(), "lambda"), new Amazon.CDK.AWS.S3.Assets.AssetOptions
                {
                    AssetHashType = AssetHashType.OUTPUT,
                    Bundling = new BundlingOptions
                    {
                        Image = Runtime.NODEJS_12_X.BundlingDockerImage,
                        User = "root",
                        Command = new[]
                        {
                                "bash",
                                "-c",
                                string.Join(" && ", new []
                                {
                                    "cp -au src package.json yarn.lock /tmp",
                                    "cd /tmp",
                                    "npm install --global yarn",
                                    "yarn install",
                                    "yarn -s esbuild src/lambda/target-group.ts --bundle --platform=node --target=node12 --outfile=/asset-output/index.js",
                                }),
                            },
                    },
                }),
                Runtime = Runtime.NODEJS_12_X,
                Handler = "index.handler",
                MemorySize = 512,
                Timeout = Duration.Minutes(10),
                InitialPolicy = new[] { new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new [] { "elasticloadbalancing:*" },
                        Resources = new [] { "*" },
                    })},
            });
            var provider = new Provider(this, "customProvider", new ProviderProps
            {
                OnEventHandler = targetGroupEventHandler,
            });
            var grpcTargetGroupResource = new CustomResource(this, "grpc-target-group-lambda", new CustomResourceProps
            {
                ServiceToken = provider.ServiceToken,
                Properties = properties,
            });
            return grpcTargetGroupResource;
        }

        private string GetUserData(string recreateMagicOnionTrigger, string bucketName, string[] binaryNames, string serviceDiscoveryId, bool useCloudWatchAgent, bool useDatadogAgent)
        {
            var source = @$"#!/bin/bash
echo ""{recreateMagicOnionTrigger}""";

            // dotnet sdk
            source += @$"
# install .NET 5
rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
yum install -y dotnet-sdk-5.0 aspnetcore-runtime-5.0
. /etc/profile.d/dotnet-cli-tools-bin-path.sh";

            // server binary
            source += $@"
# download server
mkdir -p /var/MagicOnion.Benchmark/server
aws s3 sync --exact-timestamps s3://{bucketName}/assembly/linux/server/ ~/server
cp -Rf ~/server/ /var/MagicOnion.Benchmark/.";
            foreach (var binaryName in binaryNames)
            {
                source += $@"
chmod +x /var/MagicOnion.Benchmark/server/{binaryName}/{binaryName}
cp -f /var/MagicOnion.Benchmark/server/{binaryName}/{binaryName}.service /etc/systemd/system/.
systemctl enable {binaryName}
systemctl restart {binaryName}";
            }

            // ALB だろうが ServiceDiscovery だろうが登録はする。パラメーター変えるだけでどっちでもアクセスできるしね。
            // cloudmap
            source += $@"
# cloudmap
EC2_METADATA=http://169.254.169.254/latest
INSTANCE_ID=$(curl -s $EC2_METADATA/meta-data/instance-id);
INSTANCE_IP=$(curl -s $EC2_METADATA/meta-data/local-ipv4);

sudo cat > /etc/init.d/cloudmap-register <<-EOF
#! /bin/bash -ex
aws servicediscovery register-instance \
    --region {this.Region} \
    --service-id {serviceDiscoveryId} \
    --instance-id $INSTANCE_ID \
    --attributes AWS_INSTANCE_IPV4=$INSTANCE_IP,AWS_INSTANCE_PORT=80
exit 0
EOF
chmod a+x /etc/init.d/cloudmap-register

sudo cat > /etc/init.d/cloudmap-deregister <<-EOF
#! /bin/bash -ex
aws servicediscovery deregister-instance \
 --region {this.Region} \
 --service-id {serviceDiscoveryId} \
 --instance-id $INSTANCE_ID
exit 0
EOF
chmod a+x /etc/init.d/cloudmap-deregister

cat > /usr/lib/systemd/system/cloudmap.service <<-EOF
[Unit]
Description=Run CloudMap service
Requires=network-online.target network.target
DefaultDependencies=no
Before=shutdown.target reboot.target halt.target

[Service]
Type=oneshot
KillMode=none
RemainAfterExit=yes
ExecStart=/etc/init.d/cloudmap-register
ExecStop=/etc/init.d/cloudmap-deregister

[Install]
WantedBy=multi-user.target
EOF

systemctl enable cloudmap.service
systemctl start  cloudmap.service";

            // datadog profiler
            if (useDatadogAgent)
            {
                source += @$"
# install datadog agent
yum install -y jq
export DD_API_KEY=$(aws secretsmanager get-secret-value --secret-id magiconion-benchmark-datadog-token --region {Region} | jq '.SecretString' | jq -r .)
bash -c ""$(curl -L https://raw.githubusercontent.com/DataDog/datadog-agent/master/cmd/agent/install_script.sh)""";
            }

            // cloudwatch profiler
            if (useCloudWatchAgent)
            {
                source += @$"
# install cloudwatch
aws s3 sync --exact-timestamps s3://{bucketName}/userdata/ ~/userdata
chmod 600 ~/userdata/amazon-cloudwatch-agent.json
rpm -Uvh https://s3.amazonaws.com/amazoncloudwatch-agent/amazon_linux/amd64/latest/amazon-cloudwatch-agent.rpm
cp ~/userdata/amazon-cloudwatch-agent.json /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json
rm /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.d/default
/opt/aws/amazon-cloudwatch-agent/bin/config-translator --input /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json --output /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.toml --mode ec2
/opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl -a start";
            }

            return source.Replace("\r\n", "\n");
        }
        private Role GetIamEc2MagicOnionRole(Bucket s3, Service meshService)
        {
            var policy = new Policy(this, "Ec2MagicOnionPolicy", new PolicyProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "s3:ListAllMyBuckets" },
                        Resources = new[] { "arn:aws:s3:::*" },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "s3:ListBucket","s3:GetBucketLocation" },
                        Resources = new[] { s3.BucketArn },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "s3:GetObject" },
                        Resources = new[] { $"{s3.BucketArn}/*" },
                    }),
                    // service discovery
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "servicediscovery:RegisterInstance", "servicediscovery:DeregisterInstance"},
                        Resources = new[] { meshService.ServiceArn },
                    }),
                    // datadog
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "ec2:DescribeInstanceStatus", "ec2:DescribeSecurityGroups", "ec2:DescribeInstances"},
                        Resources = new[] { "arn:aws:ec2:::*" },
                    }),
                }
            });
            var role = new Role(this, "MasterRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
            });
            role.AttachInlinePolicy(policy);
            role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"));
            role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy"));
            return role;
        }
        private Role GetIamEcsTaskExecuteRole(string[] logGroups)
        {
            var policy = new Policy(this, "WorkerTaskDefExecutionPolicy", new PolicyProps
            {
                Statements = new[]
                {
                    // s3
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[]
                        {
                            "logs:CreateLogStream",
                            "logs:PutLogEvents"
                        },
                        Resources = logGroups.Select(x => $"arn:aws:logs:{this.Region}:{this.Account}:log-group:{x}:*").ToArray(),
                    }),
                }
            });
            var role = new Role(this, "WorkerTaskDefExecutionRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            });
            role.AttachInlinePolicy(policy);
            role.AddManagedPolicy(ManagedPolicy.FromManagedPolicyArn(this, "WorkerECSTaskExecutionRolePolicy", "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"));
            return role;
        }
        private Role GetIamEcsDframeTaskDefRole(Bucket s3)
        {
            var policy = new Policy(this, "DframeTaskDefTaskPolicy", new PolicyProps
            {
                Statements = new[]
                {
                    // ecs
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[]
                        {
                            "ecs:Describe*",
                            "ecs:List*",
                            "ecs:Update*",
                            "ecs:DiscoverPollEndpoint",
                            "ecs:Poll",
                            "ecs:RegisterContainerInstance",
                            "ecs:RegisterTaskDefinition",
                            "ecs:StartTelemetrySession",
                            "ecs:UpdateContainerInstancesState",
                            "ecs:Submit*",
                        },
                        Resources = new[] { "*" },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new []
                        {
                            "iam:PassRole",
                        },
                        Resources = new [] { "*" },
                    }),
                    // s3
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "s3:ListAllMyBuckets" },
                        Resources = new[] { "arn:aws:s3:::*" },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "s3:ListBucket","s3:GetBucketLocation" },
                        Resources = new[] { s3.BucketArn },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[]
                        {
                            "s3:PutObject",
                            "s3:PutObjectAcl",
                            "s3:GetObject",
                            "s3:GetObjectAcl",
                        },
                        Resources = new[] { $"{s3.BucketArn}/*" },
                    }),
                }
            });
            var role = new Role(this, "DframeTaskDefTaskRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            });
            role.AttachInlinePolicy(policy);
            return role;
        }
        private Role GetIamEcsWorkerTaskDefRole(Bucket s3)
        {
            var policy = new Policy(this, "WorkerTaskDefTaskPolicy", new PolicyProps
            {
                Statements = new[]
                {
                    // s3
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "s3:ListAllMyBuckets" },
                        Resources = new[] { "arn:aws:s3:::*" },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[] { "s3:ListBucket","s3:GetBucketLocation" },
                        Resources = new[] { s3.BucketArn },
                    }),
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Actions = new[]
                        {
                            "s3:PutObject",
                            "s3:PutObjectAcl",
                            "s3:GetObject",
                            "s3:GetObjectAcl",
                        },
                        Resources = new[] { $"{s3.BucketArn}/*" },
                    }),
                }
            });
            var role = new Role(this, "WorkerTaskDefTaskRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            });
            role.AttachInlinePolicy(policy);
            return role;
        }
    }

    public static class AutoscalingGroupExtensions
    {
        public static void AddSecretsReadGrant(this AutoScalingGroup asg, ISecret secret, Func<bool> enable)
        {
            if (enable != null && enable.Invoke())
            {
                secret.GrantRead(asg);
            }
        }
    }

    public static class TaskDefinitionExtensions
    {
        public static void AddDatadogContainer(this TaskDefinition taskdef, string containerName, ISecret ddToken, Func<bool> enable)
        {
            if (enable != null && enable.Invoke())
            {
                taskdef.AddContainer(containerName, new ContainerDefinitionOptions
                {
                    Image = ContainerImage.FromRegistry("datadog/agent:latest"),
                    Environment = new Dictionary<string, string>
                    {
                        { "DD_API_KEY", ddToken.SecretValue.ToString() },
                        { "ECS_FARGATE","true"},
                    },
                    Cpu = 10,
                    MemoryReservationMiB = 256,
                    Essential = false,
                });
            }
        }
    }

}
