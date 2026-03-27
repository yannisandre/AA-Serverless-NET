using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace resizefunctiontp
{
    public static class ResizeHttpTrigger
    {
        private const int MaxDimension = 4000;
        private const long MaxPayloadBytes = 10 * 1024 * 1024;

        [FunctionName("ResizeHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("ResizeHttpTrigger request received.");

            if (!int.TryParse(req.Query["w"], out var w) || w <= 0)
            {
                return new BadRequestObjectResult("Invalid query parameter 'w'. Provide a positive integer.");
            }

            if (!int.TryParse(req.Query["h"], out var h) || h <= 0)
            {
                return new BadRequestObjectResult("Invalid query parameter 'h'. Provide a positive integer.");
            }

            if (w > MaxDimension || h > MaxDimension)
            {
                return new BadRequestObjectResult($"Invalid target size. 'w' and 'h' must be <= {MaxDimension}.");
            }

            if (req.Body == null || !req.Body.CanRead)
            {
                return new BadRequestObjectResult("Request body is empty. Send image bytes in the POST body.");
            }

            if (!string.IsNullOrWhiteSpace(req.ContentType) && !req.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return new UnsupportedMediaTypeResult();
            }

            if (req.ContentLength.HasValue && req.ContentLength.Value > MaxPayloadBytes)
            {
                return new ObjectResult("Image too large. Max size is 10 MB.")
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge
                };
            }

            try
            {
                byte[] targetImageBytes;
                using (var msInput = new MemoryStream())
                {
                    await req.Body.CopyToAsync(msInput);

                    if (msInput.Length == 0)
                    {
                        return new BadRequestObjectResult("Request body is empty. Send image bytes in the POST body.");
                    }

                    if (msInput.Length > MaxPayloadBytes)
                    {
                        return new ObjectResult("Image too large. Max size is 10 MB.")
                        {
                            StatusCode = StatusCodes.Status413PayloadTooLarge
                        };
                    }

                    msInput.Position = 0;

                    using (var image = Image.Load(msInput))
                    {
                        image.Mutate(x => x.Resize(w, h));

                        using (var msOutput = new MemoryStream())
                        {
                            image.SaveAsJpeg(msOutput);
                            targetImageBytes = msOutput.ToArray();
                        }
                    }
                }

                return new FileContentResult(targetImageBytes, "image/jpeg");
            }
            catch (UnknownImageFormatException)
            {
                return new ObjectResult("Unsupported or invalid image format.")
                {
                    StatusCode = StatusCodes.Status415UnsupportedMediaType
                };
            }
            catch (InvalidImageContentException)
            {
                return new BadRequestObjectResult("Invalid image content.");
            }
            catch (ArgumentOutOfRangeException)
            {
                return new BadRequestObjectResult("Invalid resize dimensions.");
            }
            catch (IOException)
            {
                return new ObjectResult("Unable to read image payload.")
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity
                };
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Request rejected due to unsupported or invalid processing state.");
                return new ObjectResult("Unable to process the image with the provided input.")
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity
                };
            }
        }
    }
}
