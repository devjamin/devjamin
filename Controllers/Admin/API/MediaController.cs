/*
    MediaUploadController.cs, handles uploading media files to Cloudflare Images when creating or editing blogs.
    Image files are compressed and resized.

    Supported file types: JPEG, PNG

    - POST: /API/Upload-Media
        IFormFile:
        {
            "File": File,
            "BlogPostId": int (optional, don't have yet when creating a new blog post)
        }
    - POST: /API/Select-Cover-Image
        {
            "MediaId": int,
            "BlogPostId": int (optional, don't have yet when creating a new blog post)
        }
    - DELETE: /API/Delete-Media/{mediaId}
*/
using devjamin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace devjamin.Controllers
{
    [Route("API")]
    [Authorize]
    [ApiController]
    public class MediaUploadController(IConfiguration configuration, BlogDbContext context, IHttpClientFactory httpClientFactory) : Controller
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly BlogDbContext _context = context;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        readonly List<string> allowedFileContentTypes =
            [
                "image/jpeg",
                "image/png"
            ];

        // POST: API/Upload-Media
        [HttpPost("Upload-Media")]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            // Ensure file is uploaded
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Error = "Upload a file." });
            }

            // Get BlogPostId from the form data and ensure it's a valid integer
            if (!Request.Form.TryGetValue("BlogPostId", out var blogPostIdValues) || !int.TryParse(blogPostIdValues, out int blogPostId))
            {
                return BadRequest(new { Error = "A valid BlogPostId is required." });
            }

            var contentType = file.ContentType;

            // Ensure file type is allowed
            if (!allowedFileContentTypes.Contains(contentType))
            {
                return StatusCode(415, new { Error = $"Invalid file type. Accepts only: {string.Join(", ", allowedFileContentTypes)}" });
            }

            Stream fileStream = file.OpenReadStream();
            try
            {
                // Compress and resize image
                fileStream = await CompressAndResizeImageAsync(fileStream, contentType);

                // Upload file to Cloudflare Images
                var uploadResult = await UploadFileAsync(file.FileName, fileStream, contentType, blogPostId);

                return Ok(uploadResult);
            }
            catch (Exception exp)
            {
                return BadRequest(new { Error = exp.Message });
            }
            finally
            {
                fileStream.Close();
            }
        }

        // Uploads file to Cloudflare Images
        private class UploadResult
        {
            public int MediaId { get; set; }
            public string FilePath { get; set; } = "";
        }

        private async Task<UploadResult> UploadFileAsync(string fileName, Stream fileStream, string contentType, int blogPostId)
        {
            var accountId = _configuration.GetValue<string>("CloudflareImages:AccountId");
            var apiKey = _configuration.GetValue<string>("CloudflareImages:ApiKey");

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Cloudflare configuration is missing.");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Create custom filename with blog ID while preserving extension
            var fileExtension = Path.GetExtension(fileName);
            var customFileName = $"Upload from Devjamin Blog: {blogPostId}{fileExtension}";

            var formData = new MultipartFormDataContent();
            formData.Add(new StreamContent(fileStream), "file", customFileName);

            var response = await client.PostAsync($"https://api.cloudflare.com/client/v4/accounts/{accountId}/images/v1", formData);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload file to Cloudflare Images: {response.StatusCode}. Response: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var cloudflareResponse = JsonSerializer.Deserialize<CloudflareResponse>(responseContent);

            if (cloudflareResponse?.Success != true)
            {
                throw new Exception($"Failed to upload to Cloudflare Images. Response: {responseContent}");
            }

            if (string.IsNullOrEmpty(cloudflareResponse.Result?.Id))
            {
                throw new Exception($"No image ID returned from Cloudflare. Response: {responseContent}");
            }

            // Add image to database
            var blogMedia = new BlogMedia
            {
                ContentType = contentType,
                BlogPostId = blogPostId,
                FilePath = cloudflareResponse.Result.Id,
                CreatedDate = DateTime.UtcNow
            };
            _context.BlogMedia.Add(blogMedia);
            await _context.SaveChangesAsync();

            var result = new UploadResult
            {
                MediaId = blogMedia.MediaId,
                FilePath = blogMedia.FilePath
            };

            return result;
        }

        private class CloudflareResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("result")]
            public CloudflareResult? Result { get; set; }

            [JsonPropertyName("errors")]
            public List<CloudflareError>? Errors { get; set; }

            [JsonPropertyName("messages")]
            public List<string>? Messages { get; set; }
        }

        private class CloudflareResult
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("filename")]
            public string? Filename { get; set; }

            [JsonPropertyName("uploaded")]
            public DateTime? Uploaded { get; set; }

            [JsonPropertyName("requireSignedURLs")]
            public bool RequireSignedURLs { get; set; }

            [JsonPropertyName("variants")]
            public List<string>? Variants { get; set; }
        }

        private class CloudflareError
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }

        // Model to capture incoming data
        public class CoverImageSelectionModel
        {
            public int BlogPostId { get; set; }
            public int MediaId { get; set; }
        }

        // POST: API/Select-Cover-Image
        [HttpPost("Select-Cover-Image")]
        public async Task<IActionResult> SelectCoverImage([FromBody] CoverImageSelectionModel model)
        {
            if (model == null)
            {
                return BadRequest(new { Error = "Invalid request payload." });
            }

            // Validate blog post exists
            var blogPost = await _context.BlogPosts.FindAsync(model.BlogPostId);
            if (blogPost == null)
            {
                return NotFound(new { Error = "Blog post not found." });
            }

            // Validate media belongs to blog post
            var media = await _context.BlogMedia.FirstOrDefaultAsync(m => m.MediaId == model.MediaId && m.BlogPostId == model.BlogPostId);
            if (media == null)
            {
                return NotFound(new { Error = "Media not found or does not belong to the specified blog post." });
            }

            // Toggle the IsCoverImage status
            if (media.IsCoverImage)
            {
                // If already the cover image, remove the flag
                media.IsCoverImage = false;
            }
            else
            {
                // If not the cover image, first clear any existing cover image flag
                var allMedia = await _context.BlogMedia.Where(m => m.BlogPostId == model.BlogPostId).ToListAsync();
                foreach (var m in allMedia)
                {
                    m.IsCoverImage = false;
                }

                // Then mark selected media as the cover image
                media.IsCoverImage = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cover image status updated successfully." });
        }

        // DELETE: API/Delete-Media/{mediaId}
        [HttpDelete("Delete-Media/{mediaId}")]
        public async Task<IActionResult> DeleteMedia(int mediaId)
        {
            // Fetch the media item to be deleted
            var mediaItem = await _context.BlogMedia.FindAsync(mediaId);
            if (mediaItem == null)
            {
                return NotFound(new { Error = "Media not found." });
            }

            // Disallow if media item is marked as a cover image
            if (mediaItem.IsCoverImage)
            {
                return BadRequest(new { Error = "Cannot delete a cover image." });
            }

            var accountId = _configuration.GetValue<string>("CloudflareImages:AccountId");
            var apiKey = _configuration.GetValue<string>("CloudflareImages:ApiKey");

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Cloudflare configuration is missing.");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.DeleteAsync($"https://api.cloudflare.com/client/v4/accounts/{accountId}/images/v1/{mediaItem.FilePath}");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(500, new { Error = $"Error deleting media from Cloudflare Images: {response.StatusCode}" });
            }

            // Proceed with deletion from database if successful Cloudflare deletion
            _context.BlogMedia.Remove(mediaItem);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Media deleted successfully." });
        }

        // Compresses and resizes image
        private static async Task<Stream> CompressAndResizeImageAsync(Stream imageStream, string contentType)
        {
            try
            {
                var image = await Image.LoadAsync(imageStream);
                var outputStream = new MemoryStream();

                // Resize if width or height is greater than 1080p
                if (image.Width > 1920 || image.Height > 1080)
                {
                    // Calculate new dimensions while maintaining aspect ratio
                    int newWidth, newHeight;
                    if (image.Width > image.Height)
                    {
                        newWidth = 1920;
                        newHeight = (int)(image.Height / (image.Width / (double)newWidth));
                    }
                    else
                    {
                        newHeight = 1080;
                        newWidth = (int)(image.Width / (image.Height / (double)newHeight));
                    }

                    // Resize image
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                // Save as JPEG or PNG based on the contentType
                if (contentType.Contains("png", StringComparison.InvariantCultureIgnoreCase))
                {
                    // PNG images are not compressed, due to it being lossless format
                    await image.SaveAsPngAsync(outputStream);
                }
                else
                {
                    var encoder = new JpegEncoder
                    {
                        Quality = 60
                    };
                    await image.SaveAsJpegAsync(outputStream, encoder);
                }

                outputStream.Seek(0, SeekOrigin.Begin);
                return outputStream;
            }
            catch
            {
                throw new Exception("Failed to compress image.");
            }
        }
    }
}
