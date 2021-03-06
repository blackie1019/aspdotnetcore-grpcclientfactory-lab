﻿using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using Grpc.Core;
using GrpcClientFactory.Lab.Client.BackgroundServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Serilog;

namespace GrpcClientFactory.Lab.Client
{
    public class Startup
    {
        private const int RetryCount = 3;
        private const int RetryBaseIntervalInSecond = 1;

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
            
            var retryPolicy = Policy.Handle<HttpRequestException>(requestException =>
                {
                    Log.Warning(requestException.Message);

                    return true;
                }).OrResult<HttpResponseMessage>(response =>
                {

                    var grpcStatus = StatusManager.GetStatusCode(response);
                    var httpStatusCode = response.StatusCode;

                    var hasConnectionIssue = grpcStatus == null &&
                                             serverErrors.Contains(
                                                 httpStatusCode) || // if the server send an error before gRPC pipeline
                                             (httpStatusCode == HttpStatusCode.OK &&
                                              gRpcErrors.Contains(grpcStatus
                                                  .Value)); // if gRPC pipeline handled the request (gRPC always answers OK)

                    Log.Warning($"{grpcStatus.ToString()}, {httpStatusCode.ToString()}");
                    if (!hasConnectionIssue)
                    {
                        Log.Information($"Request Passed:{grpcStatus.ToString()}, {httpStatusCode.ToString()}");

                        return false;
                    }

                    Log.Warning($"Connection Issue:{grpcStatus.ToString()}, {httpStatusCode.ToString()}");

                    return true;

                })
                .WaitAndRetryAsync(RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(RetryBaseIntervalInSecond + 1, retryAttempt)),
                    (result, timeSpan, retryCount, context) =>
                    {
                        Log.Warning(
                            result.Exception == null
                                ? $"Request failed with grpcStatus:{StatusManager.GetStatusCode(result.Result).ToString()}, httpStatusCode: {result.Result.StatusCode.ToString()}. Retry:{retryCount}"
                                : $"Request failed with Exception:{result.Exception.Message}, Retry:{retryCount}");
                    });

            services.AddGrpcClient<Greeter.GreeterClient>(o =>
            {
                o.Address = new Uri("http://localhost:5001");
            }).ConfigureHttpClient(o =>
            {
                //Add for macOS
                o.DefaultRequestVersion = new Version(2, 0);
            }).ConfigureChannel(o =>
            {
                o.Credentials = ChannelCredentials.Insecure;
            }).AddPolicyHandler(retryPolicy);
            
            services.AddControllers();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                // app.UseDeveloperExceptionPage();
            }
            
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
                
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}