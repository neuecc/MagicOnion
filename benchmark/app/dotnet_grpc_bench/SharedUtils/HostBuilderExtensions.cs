using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;

namespace Benchmark.Server
{
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configure Configuration with embedded appsettings.json
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder ConfigureEmbeddedConfiguration(this IHostBuilder builder, string[] args)
            => ConfigureEmbeddedConfiguration(builder, args, Assembly.GetEntryAssembly(), AppContext.BaseDirectory);

        /// <summary>
        /// Configure Configuration with embedded appsettings.json
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="args"></param>
        /// <param name="assembly"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static IHostBuilder ConfigureEmbeddedConfiguration(this IHostBuilder builder, string[] args, Assembly assembly, string rootPath)
        {
            var embedded = new EmbeddedFileProvider(assembly);
            var physical = new PhysicalFileProvider(rootPath);

            // added Embedded Config selection for AddJsonFile, based on https://github.com/dotnet/runtime/blob/6a5a78bec9a6e14b4aa52cd5ac558f6cf5c6a211/src/libraries/Microsoft.Extensions.Hosting/src/Host.cs
            return builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                IHostEnvironment env = hostingContext.HostingEnvironment;
                bool reloadOnChange = hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);

                config.AddJsonFile(new CompositeFileProvider(physical, embedded), "appsettings.json", optional: true, reloadOnChange: reloadOnChange)
                      .AddJsonFile(new CompositeFileProvider(physical, embedded), $"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: reloadOnChange);

                if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true, reloadOnChange: reloadOnChange);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });
        }
    }
}
