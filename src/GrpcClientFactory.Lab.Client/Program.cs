using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GrpcClientFactory.Lab.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/application.log", rollingInterval: RollingInterval.Hour)
                .CreateLogger();
            
            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            // Add for macOS
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var environmentName = webBuilder.GetSetting("Environment");
                    if (string.Compare(environmentName, "Development", StringComparison.InvariantCultureIgnoreCase) ==
                        0)
                    {
                        Log.Information(environmentName);
                        webBuilder.ConfigureKestrel(options =>
                        {
                            // Setup a HTTP/2 endpoint without TLS.
                            options.ListenLocalhost(6001, o => o.Protocols = HttpProtocols.Http2);
                            // Setup a HTTP1 for RestfulAPI
                            options.ListenLocalhost(6002, o => o.Protocols =
                                HttpProtocols.Http1);
                        });
                    }

                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}