using Microsoft.AspNetCore.Mvc;
using devjamin.Services;

namespace devjamin.Controllers
{
    [Route("API")]
    [ApiController]
    public class EmailController(IEmailService emailService) : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;

        [HttpPost("Contact")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Do not allow large requests
            if (request.From.Length > 100 || request.Subject.Length > 100 || request.Body.Length > 1000)
            {
                return BadRequest("Request fields are too large.");
            }

            try
            {
                await _emailService.SendEmailAsync(request.From, request.Subject, request.Body);
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    }

    public class EmailRequest
    {
        public required string From { get; set; }

        public required string Subject { get; set; }

        public required string Body { get; set; }
    }
}
