using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GrpcClientFactory.Lab.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly Greeter.GreeterClient _client;
        public TestController(Greeter.GreeterClient client)
        {
            _client = client;
        }

        // GET
        [HttpGet]
        public async Task<ActionResult<string>> Index()
        {
            var reply = await _client.SayHelloAsync(new HelloRequest { Name = $"Restful Client, at {DateTime.Now.ToString("O")}" });
            return reply.Message;
        }
    }
}