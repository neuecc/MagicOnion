using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using ZLogger;

namespace Benchmark.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // expand thread pool
            //ThreadPools.ModifyThreadPool(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);

            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureEmbeddedConfiguration(args)
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.ClearProviders();
                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        logging.AddZLoggerConsole(configure => configure.EnableStructuredLogging = false);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .ConfigureKestrel(serverOptions =>
                        {
                            // per channel Stream (ServerStream, ClientStream, Duplex) will cap by this value.
                            serverOptions.Limits.Http2.MaxStreamsPerConnection = 50000;
                        })
                        .UseStartup<Startup>();
                });
    }
} 
