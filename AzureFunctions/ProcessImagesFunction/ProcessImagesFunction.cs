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
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

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
                using var obverseImage = await LoadAndProcessImage(obversePngBytes, log);
                using var reverseImage = await LoadAndProcessImage(reversePngBytes, log);

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

        private static async Task<Image<Rgba32>> LoadAndProcessImage(byte[] imageBytes, ILogger log)
        {
            int cropX, cropY;

            var image = Image.Load<Rgba32>(imageBytes);
            int targetWidth = 350; // Half of merged image width
            int targetHeight = 420;

            // Resize before cropping
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth * 2, targetHeight * 2),
                Mode = ResizeMode.Max
            }));

            var bbox = await GetCoinBoundingBoxAsync(imageBytes, log);

            if (bbox != null)
            {
                var actualBox = bbox;

                // Convert relative coords to pixels
                int x = (int)(actualBox.Left * image.Width);
                int y = (int)(actualBox.Top * image.Height);
                int w = (int)(actualBox.Width * image.Width);
                int h = (int)(actualBox.Height * image.Height);

                cropX = Math.Max(x + (w - targetWidth) / 2, 0);
                cropY = Math.Max(y + (h - targetHeight) / 2, 0);

                cropX = Math.Min(cropX, image.Width - targetWidth);
                cropY = Math.Min(cropY, image.Height - targetHeight);
            }
            else
            {
                // Center crop
                cropX = (image.Width - targetWidth) / 2;
                cropY = (image.Height - targetHeight) / 2;
            }

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

        private static async Task<BoundingBoxPercent?> GetCoinBoundingBoxAsync(byte[] imageBytes, ILogger log)
        {
            try
            {
                var predictionKey = Environment.GetEnvironmentVariable("CustomVisionPredictionKey");
                var endpoint = Environment.GetEnvironmentVariable("CustomVisionPredictionEndpoint");
                var projectId = Environment.GetEnvironmentVariable("CustomVisionProjectId");
                var publishedName = Environment.GetEnvironmentVariable("CustomVisionPublishedModelName");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);

                using var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var url = $"{endpoint}/customvision/v3.0/Prediction/{projectId}/detect/iterations/{publishedName}/image";
                var response = await client.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(result);
                var prediction = json["predictions"]?.FirstOrDefault(p => p?["probability"] != null && p["probability"]!.Value<double>() > 0.6);
                if (prediction == null) return null;

                var bbox = prediction["boundingBox"];
                double left = bbox?["left"]?.Value<double>() ?? 0.0;
                double top = bbox?["top"]?.Value<double>() ?? 0.0;
                double width = bbox?["width"]?.Value<double>() ?? 0.0;
                double height = bbox?["height"]?.Value<double>() ?? 0.0;

                return new BoundingBoxPercent
                {
                    Left = (float)left,
                    Top = (float)top,
                    Width = (float)width,
                    Height = (float)height
                };
            }
            catch(Exception ex)
            {
                log.LogWarning($"Bounding box of coin wasn't defined: {ex.Message}");
                return null;
            }
        }


        public class ImageRequest
        {
            public required string ObverseImageBase64 { get; set; }
            public required string ReverseImageBase64 { get; set; }
        }

        private class BoundingBoxPercent
        {
            public float Left { get; set; }
            public float Top { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }

        }
    }
}
