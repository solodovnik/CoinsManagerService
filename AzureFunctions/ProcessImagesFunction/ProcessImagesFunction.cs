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
using ExifTag = SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag;

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

                var correctedOrientationObverseImageBase64 = await CorrectImageOrientationAsync(data.ObverseImageBase64);
                var correctedOrientationReverseImageBase64 = await CorrectImageOrientationAsync(data.ReverseImageBase64);

                var obverseImageBytes = Convert.FromBase64String(correctedOrientationObverseImageBase64);
                var reverseImageBytes = Convert.FromBase64String(correctedOrientationReverseImageBase64);

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
            var image = Image.Load<Rgba32>(imageBytes);
            int targetWidth = 350;
            int targetHeight = 420;

            var bbox = await GetCoinBoundingBoxAsync(imageBytes, log);

            Image<Rgba32> cropped;

            if (bbox != null)
            {
                log.LogInformation("Applying crop at the coin location recognized by the AI.");
                int x = (int)(bbox.Left * image.Width);
                int y = (int)(bbox.Top * image.Height);
                int w = (int)(bbox.Width * image.Width);
                int h = (int)(bbox.Height * image.Height);

                // Set padding to 11% to zoom in
                int padX = (int)(w * 0.11);
                int padY = (int)(h * 0.11);

                int cropX = Math.Max(x - padX, 0);
                int cropY = Math.Max(y - padY, 0);
                int cropW = Math.Min(w + 2 * padX, image.Width - cropX);
                int cropH = Math.Min(h + 2 * padY, image.Height - cropY);

                cropped = image.Clone(ctx => ctx.Crop(new Rectangle(cropX, cropY, cropW, cropH)));
            }
            else
            {
                // Fallback: center square crop
                log.LogInformation("Applying center square crop since the AI couldn't recognize the coin.");
                int side = Math.Min(image.Width, image.Height);
                int cropX = (image.Width - side) / 2;
                int cropY = (image.Height - side) / 2;
                cropped = image.Clone(ctx => ctx.Crop(new Rectangle(cropX, cropY, side, side)));
            }

            // Resize with Crop mode to fill target area without black bars
            cropped.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Crop
            }));

            return cropped;
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
            image.Save(outputStream, new JpegEncoder
            {
                Quality = 75
            });
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
            catch (Exception ex)
            {
                log.LogWarning($"Bounding box of coin wasn't defined: {ex.Message}");
                return null;
            }
        }

        public static async Task<string> CorrectImageOrientationAsync(string base64Image)
        {
            var imageBytes = Convert.FromBase64String(base64Image);
            using var inputStream = new MemoryStream(imageBytes);
            inputStream.Position = 0;

            using var image = await Image.LoadAsync(inputStream);
            var exif = image.Metadata.ExifProfile;

            ushort orientation = 1;

            var orientationEntry = exif?.Values.FirstOrDefault(v => v.Tag == ExifTag.Orientation);
            if (orientationEntry != null)
            {
                var rawValue = orientationEntry.GetValue();
                if (rawValue is ushort parsed)
                {
                    orientation = parsed;
                }
            }

            switch (orientation)
            {
                case 2:
                    image.Mutate(x => x.Flip(FlipMode.Horizontal));
                    break;
                case 3:
                    image.Mutate(x => x.Rotate(RotateMode.Rotate180));
                    break;
                case 4:
                    image.Mutate(x => x.Flip(FlipMode.Vertical));
                    break;
                case 5:
                    image.Mutate(x => x.Rotate(RotateMode.Rotate90).Flip(FlipMode.Horizontal));
                    break;
                case 6:
                    image.Mutate(x => x.Rotate(RotateMode.Rotate90));
                    break;
                case 7:
                    image.Mutate(x => x.Rotate(RotateMode.Rotate270).Flip(FlipMode.Horizontal));
                    break;
                case 8:
                    image.Mutate(x => x.Rotate(RotateMode.Rotate270));
                    break;
            }

            exif?.RemoveValue(ExifTag.Orientation);

            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            return Convert.ToBase64String(outputStream.ToArray());
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
