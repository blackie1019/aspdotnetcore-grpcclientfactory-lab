using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GrpcClientFactory.Lab.Client.BackgroundServices
{
    public class GreetingCheckServices: BackgroundService
    {
        private readonly Greeter.GreeterClient _client;
        
        public GreetingCheckServices(Greeter.GreeterClient client)
        {
            _client = client;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var reply = await _client.SayHelloAsync(new HelloRequest { Name = "GrpcClientFactory.Lab.Client" });
                    Log.Information(reply.Message);
                    Log.Information("done");
                }
                catch (Exception e)
                {
                    Log.Error(e,);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}