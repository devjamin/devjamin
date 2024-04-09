/*
    ContactController.cs, houses the API endpoint for contacting site owner (me) via contact form at /.
*/
using Microsoft.AspNetCore.Mvc;
using devjamin.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace devjamin.Controllers
{
    [Route("API")]
    [ApiController]
    public class ContactController(IEmailService emailService, IAntiforgery antiforgery) : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;
        private readonly IAntiforgery _antiforgery = antiforgery;

        [HttpPost("Contact")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please check your input and try again");
            }

            // Extract CSRF token from request headers
            var csrfTokenHeader = HttpContext.Request.Headers["RequestVerificationToken"].FirstOrDefault();
            if (string.IsNullOrEmpty(csrfTokenHeader))
            {
                return BadRequest("Missing CSRF-token");
            }

            // Validate CSRF token
            try
            {
                await _antiforgery.ValidateRequestAsync(HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return BadRequest("Invalid CSRF-token");
            }

            // Input length validation
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
    }
}
