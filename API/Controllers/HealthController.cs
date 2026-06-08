using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Repositories.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IRedisService _redis;
        private readonly IConfiguration _config;

        public HealthController(IRedisService redis, IConfiguration config)
        {
            _redis = redis;
            _config = config;
        }

        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok" });

        [HttpGet("redis")]
        public async Task<IActionResult> Redis()
        {
            var ok = await _redis.PingAsync();
            if (ok) return Ok(new { redis = "ok" });
            return StatusCode(503, new { redis = "unavailable" });
        }

        [HttpGet("smtp")]
        public IActionResult Smtp()
        {
            try
            {
                var section = _config.GetSection("Smtp");
                var host = section.GetValue<string>("Host");
                var port = section.GetValue<int>("Port");
                if (string.IsNullOrEmpty(host) || port == 0)
                    return StatusCode(503, new { smtp = "not-configured" });

                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(host, port);
                var completed = connectTask.Wait(TimeSpan.FromSeconds(5));
                if (!completed || !tcp.Connected)
                    return StatusCode(503, new { smtp = "unreachable" });

                return Ok(new { smtp = "ok" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { smtp = "error", detail = ex.Message });
            }
        }
    }
}
