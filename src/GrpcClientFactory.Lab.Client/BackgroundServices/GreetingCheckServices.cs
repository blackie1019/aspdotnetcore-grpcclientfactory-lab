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
                    Log.Information($"Done!, {reply.Message}");
                }
                catch (Exception e)
                {
                    //Log.Error(e.Message, e.StackTrace);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}