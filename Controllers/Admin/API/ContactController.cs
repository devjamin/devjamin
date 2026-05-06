/*
    ContactController.cs, houses the API endpoint for contacting site owner (me) via contact form at /.
*/
using Microsoft.AspNetCore.Mvc;
using devjamin.Services;

namespace devjamin.Controllers
{
    [Route("API")]
    [ApiController]
    public class ContactController(IEmailService emailService, IWebHostEnvironment env) : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;
        private readonly IWebHostEnvironment _env = env;

        [HttpPost("Contact")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            // Skipped in dev so localhost testing still works
            if (!_env.IsDevelopment())
            {
                const string allowedOrigin = "https://www.devjamin.com";
                var origin = HttpContext.Request.Headers["Origin"].FirstOrDefault();
                var referer = HttpContext.Request.Headers["Referer"].FirstOrDefault();
                var fromAllowedOrigin = origin == allowedOrigin || (referer != null && referer.StartsWith(allowedOrigin + "/"));
                if (!fromAllowedOrigin)
                {
                    return BadRequest("Invalid origin");
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Please check your input and try again");
            }

            // Honeypot — silently succeed so bots don't retry with a different shape
            if (!string.IsNullOrEmpty(request.Website))
            {
                return Ok("Email sent successfully.");
            }

            if (request.Name.Length > 100 | request.From.Length > 100 || request.Subject.Length > 100 || request.Body.Length > 1000)
            {
                return BadRequest("Too much content for me to read mate");
            }

            try
            {
                await _emailService.SendEmailAsync(request.Name, request.From, request.Subject, request.Body);
                return Ok("Email sent successfully.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class EmailRequest
    {
        public required string Name { get; set; }
        public required string From { get; set; }
        public required string Subject { get; set; }
        public required string Body { get; set; }
        public string? Website { get; set; }
    }
}
