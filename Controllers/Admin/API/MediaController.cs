/*
    MediaUploadController.cs, handles uploading media files to an S3 bucket when creating or editing blogs.
    Image files are compressed and resized based on conditions where videos are uploaded as is.

    Supported file types: JPEG, PNG, MP4

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
using Amazon.S3;
using Amazon.S3.Model;
using devjamin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            // Try to get BlogPostId from the form data; it's now optional
            Request.Form.TryGetValue("blogPostId", out var blogPostIdValues);
            int? blogPostId = null;

            // If a BlogPostId is provided, try to parse it
            if (!string.IsNullOrWhiteSpace(blogPostIdValues))
            {
                if (!int.TryParse(blogPostIdValues, out int parsedBlogPostId))
                {
                    return BadRequest(new { Error = "Invalid BlogPostId." });
                }
                blogPostId = parsedBlogPostId;
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


        // Uploads file to S3 bucket
        private class UploadResult
        {
            public int MediaId { get; set; }
            public string FilePath { get; set; } = "";
        }
        private async Task<UploadResult> UploadFileAsync(string fileName, Stream fileStream, string contentType, int? blogPostId)
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
                    CreatedAt = DateTime.UtcNow
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
            else
            {
                throw new Exception("Failed to upload file.");
            }
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

            // Delete file from S3 bucket
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _configuration.GetValue<string>("EnvironmentVariables:BUCKETNAME"),
                Key = mediaItem.FilePath
            };

            try
            {
                var response = await _s3Client.DeleteObjectAsync(deleteObjectRequest);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    // If response is not NoContent, deletion wasn't successful
                    throw new Exception("Failed to delete media from S3 bucket.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error deleting media from storage: {ex.Message}" });
            }

            // Proceed with deletion from database if successful S3 deletion
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
