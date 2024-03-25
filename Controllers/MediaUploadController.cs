/*
    MediaUploadController.cs, handles uploading media files to an S3 bucket when creating or editing blogs.
    Image files are compressed and resized based on conditions where videos are uploaded as is.

    Supported file types: JPEG, PNG, MP4

    POST: /API/Upload-Media
    Form data:
    {
        "File": File
        "BlogPostId": int
    }
*/
using Amazon.S3;
using Amazon.S3.Model;
using devjamin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace devjamin.Controllers
{
    [Route("API")]
    [Authorize]
    [ApiController]
    public class MediaUploadController(IAmazonS3 s3Client, IConfiguration configuration, BlogDbContext context) : Controller
    {
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly IConfiguration _configuration = configuration;
        private readonly BlogDbContext _context = context;
        readonly List<string> allowedFileContentTypes =
            [
                "image/jpeg",
                "image/png",
                "video/mp4"
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

            // Ensure it contains blogPostId
            if (!Request.Form.TryGetValue("blogPostId", out var blogPostIdValues))
            {
                return BadRequest(new { Error = "BlogPostId is required." });
            }

            // Convert blogPostId from StringValues to int
            if (!int.TryParse(blogPostIdValues, out int blogPostId))
            {
                return BadRequest(new { Error = "Invalid BlogPostId." });
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
                // If image type, compress and resize
                if (contentType.Contains("image", StringComparison.InvariantCultureIgnoreCase))
                {
                    fileStream = await CompressAndResizeImageAsync(fileStream, contentType);
                }

                // Upload file to S3 bucket
                string fileUrl = await UploadFileAsync(file.FileName, fileStream, contentType, blogPostId);



                return Ok(new { Url = fileUrl });
            }
            catch (Exception exp)
            {
                if (exp == null)
                {
                    return BadRequest(new { Error = "Internal server error." });
                }
                return BadRequest(new { Error = exp.Message });
            }
            finally
            {
                fileStream.Close();
            }
        }

        // Uploads file to S3 bucket
        private async Task<string> UploadFileAsync(string fileName, Stream fileStream, string contentType, int blogPostId)
        {
            // Generate unique key name
            var keyName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);

            var putRequest = new PutObjectRequest
            {
                BucketName = _configuration.GetValue<string>("EnvironmentVariables:BUCKETNAME"),
                Key = keyName,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead // Makes file public
            };

            var response = await _s3Client.PutObjectAsync(putRequest);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // Add image to database
                var blogMedia = new BlogMedia
                {
                    ContentType = contentType,
                    BlogPostId = blogPostId,
                    FilePath = keyName,
                    CreatedAt = DateTime.Now
                };
                _context.BlogMedia.Add(blogMedia);
                await _context.SaveChangesAsync();

                // Return file URL
                return $"{_configuration.GetValue<string>("EnvironmentVariables:CDNBASEPATH")}{keyName}";
            }
            else
            {
                throw new Exception("Failed to upload file.");
            }
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
