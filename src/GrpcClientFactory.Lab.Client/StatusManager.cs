using System.Linq;
using System.Net;
using System.Net.Http;
using Grpc.Core;

namespace GrpcClientFactory.Lab.Client
{
    public static class StatusManager
    {
        public static StatusCode? GetStatusCode(HttpResponseMessage response)
        {
            const string grpcStatus = "grpc-status";
            var headers = response.Headers;

            if (!headers.Contains(grpcStatus) && response.StatusCode == HttpStatusCode.OK)
                return StatusCode.OK;

            if (headers.Contains(grpcStatus))
                return (StatusCode)int.Parse(headers.GetValues(grpcStatus).First());

            return null;
        }
    }
}