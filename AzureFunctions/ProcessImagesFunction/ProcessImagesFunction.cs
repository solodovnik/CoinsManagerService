using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Azure.Functions.Worker;
using ImageMagick;

namespace AzureFunctions
{
    public static class ProcessImagesFunction
    {
        [Function("ProcessImages")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            FunctionContext context)
        {
            var log = context.GetLogger("ProcessImages");
            log.LogInformation("Processing images.");

            try
            {
                var data = await ParseRequestBody(req);
                var obverseImageBytes = Convert.FromBase64String(data.ObverseImageBase64);
                var reverseImageBytes = Convert.FromBase64String(data.ReverseImageBase64);

                // Ensure images are in PNG format
                var obversePngBytes = ConvertToPngIfNeeded(obverseImageBytes, log);
                var reversePngBytes = ConvertToPngIfNeeded(reverseImageBytes, log);

                // Process images: resize, crop, and merge
                using var obverseImage = LoadAndProcessImage(obversePngBytes);
                using var reverseImage = LoadAndProcessImage(reversePngBytes);

                var mergedImage = MergeImagesSideBySide(obverseImage, reverseImage);

                var mergedImageBase64 = ConvertImageToBase64(mergedImage);
                return new OkObjectResult(new { MergedImageBase64 = mergedImageBase64 });
            }
            catch (Exception ex)
            {
                log.LogError($"Error processing images: {ex.Message}");
                return new BadRequestObjectResult($"Error: {ex.Message}");
            }
        }

        private static async Task<ImageRequest> ParseRequestBody(HttpRequest req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var imageRequest = JsonConvert.DeserializeObject<ImageRequest>(requestBody);
            if (imageRequest == null)
            {
                throw new InvalidOperationException("Failed to deserialize request body.");
            }
            return imageRequest;

        }

        private static byte[] ConvertToPngIfNeeded(byte[] imageBytes, ILogger log)
        {
            using var image = new MagickImage(imageBytes);

            if (image.Format == MagickFormat.Heic || image.Format == MagickFormat.Heif)
            {
                log.LogInformation("Converting image from HEIC to PNG.");
                return image.ToByteArray(MagickFormat.Png);
            }

            log.LogInformation($"Image format is {image.Format}, no conversion needed.");
            return imageBytes;
        }

        private static Image<Rgba32> LoadAndProcessImage(byte[] imageBytes)
        {
            var image = Image.Load<Rgba32>(imageBytes);
            int targetWidth = 350; // Half of merged image width
            int targetHeight = 420;

            // Resize before cropping
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth * 2, targetHeight * 2),
                Mode = ResizeMode.Max
            }));

            // Center crop
            int cropX = (image.Width - targetWidth) / 2;
            int cropY = (image.Height - targetHeight) / 2;

            image.Mutate(x => x.Crop(new Rectangle(cropX, cropY, targetWidth, targetHeight)));
            return image;
        }

        private static Image<Rgba32> MergeImagesSideBySide(Image<Rgba32> leftImage, Image<Rgba32> rightImage)
        {
            int width = leftImage.Width + rightImage.Width;
            int height = Math.Max(leftImage.Height, rightImage.Height);

            var mergedImage = new Image<Rgba32>(width, height);
            mergedImage.Mutate(x =>
            {
                x.DrawImage(leftImage, new Point(0, 0), 1f);
                x.DrawImage(rightImage, new Point(leftImage.Width, 0), 1f);
            });

            return mergedImage;
        }

        private static string ConvertImageToBase64(Image<Rgba32> image)
        {
            using var outputStream = new MemoryStream();
            image.Save(outputStream, new JpegEncoder());
            return Convert.ToBase64String(outputStream.ToArray());
        }

        public class ImageRequest
        {
            public required string ObverseImageBase64 { get; set; }
            public required string ReverseImageBase64 { get; set; }
        }
    }
}
