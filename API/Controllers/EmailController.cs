using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using API.Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/email")]
    public class EmailController : ControllerBase
    {
        private readonly IMailService _mailService;

        public EmailController(IMailService mailService)
        {
            _mailService = mailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.To))
                return BadRequest("Invalid request");

            try
            {
                await _mailService.SendEmailAsync(request.To, request.Subject ?? string.Empty, request.Body ?? string.Empty);
                return Ok("Email sent successfully");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, "Failed to send email: " + ex.Message);
            }
        }
    }
}
