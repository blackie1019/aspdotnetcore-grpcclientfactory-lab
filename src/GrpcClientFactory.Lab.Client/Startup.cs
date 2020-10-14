using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using Grpc.Core;
using GrpcClientFactory.Lab.Client.BackgroundServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Serilog;

namespace GrpcClientFactory.Lab.Client
{
    public class Startup
    {
        private const int RetryCount = 3;
        private const int RetryBaseIntervalInSecond = 3;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<GreetingCheckServices>();

            var serverErrors = new [] { 
                HttpStatusCode.BadGateway, 
                HttpStatusCode.GatewayTimeout, 
                HttpStatusCode.ServiceUnavailable, 
                HttpStatusCode.InternalServerError, 
                HttpStatusCode.TooManyRequests, 
                HttpStatusCode.RequestTimeout
            };

            var gRpcErrors = new [] {
                StatusCode.DeadlineExceeded,
                StatusCode.Internal,
                StatusCode.NotFound,
                StatusCode.ResourceExhausted,
                StatusCode.Unavailable,
                StatusCode.Unknown
            };

            Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> retryFunc = request =>
            {
                return Policy.HandleResult<HttpResponseMessage>(r =>
                    {

                        var grpcStatus = StatusManager.GetStatusCode(r);
                        var httpStatusCode = r.StatusCode;
                        Log.Information($"{grpcStatus.ToString()}, {httpStatusCode.ToString()}");

                        return grpcStatus == null &&
                               serverErrors.Contains(httpStatusCode) || // if the server send an error before gRPC pipeline
                               httpStatusCode == HttpStatusCode.OK &&
                               gRpcErrors.Contains(grpcStatus.Value); // if gRPC pipeline handled the request (gRPC always answers OK)
                    })
                    .WaitAndRetryAsync(RetryCount, (input) => TimeSpan.FromSeconds(RetryBaseIntervalInSecond + input),
                        (result, timeSpan, retryCount, context) =>
                        {
                            var grpcStatus = StatusManager.GetStatusCode(result.Result);
                            Console.WriteLine($"Request failed with {grpcStatus}. Retry");
                        });
            };
            
            services.AddGrpcClient<Greeter.GreeterClient>(o =>
            {
                o.Address = new Uri("http://localhost:5001");
            }).ConfigureHttpClient(o =>
            {
                //Add for macOS
                o.DefaultRequestVersion = new Version(2,0);
            }).ConfigureChannel(o =>
            {
                o.Credentials = ChannelCredentials.Insecure;
            }).AddPolicyHandler(retryFunc);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
        }
    }
}