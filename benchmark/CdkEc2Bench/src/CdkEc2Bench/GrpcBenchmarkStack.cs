using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;

namespace GrpcBenchmark
{
    public class GrpcBenchmarkStack : Stack
    {
        internal GrpcBenchmarkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "vpc", new VpcProps
            {
                MaxAzs = 1,
                NatGateways = 0,
                SubnetConfiguration = new[] { new SubnetConfiguration { Name = "public", SubnetType = SubnetType.PUBLIC } },
            });
            var subnets = new SubnetSelection { Subnets = vpc.PublicSubnets };
            var sg = new SecurityGroup(this, "MasterSg", new SecurityGroupProps
            {
                AllowAllOutbound = true,
                Vpc = vpc,
            });
            var role = new Role(this, "MasterRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
            });
            role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"));

            var spot = new AutoScalingGroup(this, "instances", new AutoScalingGroupProps
            {
                // Monitoring is default DETAILED.
                SpotPrice = "1.0", // 0.0096 for spot price average for m3.medium
                Vpc = vpc,
                SecurityGroup = sg,
                VpcSubnets = subnets,
                InstanceType = InstanceType.Of(InstanceClass.COMPUTE5_AMD, InstanceSize.XLARGE4),
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
                Role = role,
                UpdatePolicy = UpdatePolicy.ReplacingUpdate(),
            });
            // https://gist.github.com/npearce/6f3c7826c7499587f00957fee62f8ee9
            spot.AddUserData(new[]
            {
                "amazon-linux-extras install docker -y",
                "service docker start",
                "chkconfig docker on",
                "usermod -a -G docker ec2-user",
                "curl -L https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m) -o /usr/local/bin/docker-compose",
                "chmod +x /usr/local/bin/docker-compose",
                "yum install -y git",
                "reboot",
            });
        }
    }
}
