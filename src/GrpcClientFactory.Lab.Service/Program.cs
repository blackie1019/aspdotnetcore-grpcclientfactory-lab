using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GrpcClientFactory.Lab.Service
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
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var environmentName = webBuilder.GetSetting("Environment");
                    if (string.Compare(environmentName,"Development",StringComparison.InvariantCultureIgnoreCase)==0)
                    {
                        Log.Information(environmentName);
                        webBuilder.ConfigureKestrel(options =>
                        {
                            // Setup a HTTP/2 endpoint without TLS.
                            options.ListenLocalhost(5001, o => o.Protocols = HttpProtocols.Http2);
                        });
                    }
                    webBuilder.UseStartup<Startup>();
                });
    }
}