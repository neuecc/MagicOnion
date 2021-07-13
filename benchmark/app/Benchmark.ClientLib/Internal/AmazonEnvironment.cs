using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.ClientLib.Internal
{
    internal static class AmazonEnvironment
    {
        public static bool IsAmazonEc2()
        {
            const string hypervisor = "/sys/hypervisor/uuid";
            const string hvm = "/sys/devices/virtual/dmi/id/product_uuid";
            const string nitro = "/sys/devices/virtual/dmi/id/board_asset_tag";

            if (File.Exists(hypervisor) && File.ReadAllText(hypervisor).StartsWith("ec2", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (File.Exists(hvm) && File.ReadAllText(hvm).StartsWith("EC2", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (File.Exists(nitro) && File.ReadAllText(nitro).StartsWith("i-", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
