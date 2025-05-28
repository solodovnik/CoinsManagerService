using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System;
using System.Threading.Tasks;
using ImageMagick;
using SixLabors.ImageSharp.Processing;
using System.Linq;
using ExifTag = SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace CoinsManagerService.Services
{
    public class ImageProcessingService : IImageProcessingService
    {

        private readonly ILogger<ImageProcessingService> _logger;
        private readonly IConfiguration _configuration;
        public ImageProcessingService(ILogger<ImageProcessingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public string ConvertToBase64(Image<Rgba32> image)
        {
            using var outputStream = new MemoryStream();
            image.Save(outputStream, new JpegEncoder
            {
                Quality = 75
            });
            return Convert.ToBase64String(outputStream.ToArray());
        }

        public byte[] ConvertToPng(byte[] imageBytes)
        {
            using var image = new MagickImage(imageBytes);

            if (image.Format == MagickFormat.Heic || image.Format == MagickFormat.Heif)
            {
                _logger.LogInformation("Converting image from HEIC to PNG.");
                return image.ToByteArray(MagickFormat.Png);
            }

            _logger.LogInformation($"Image format is {image.Format}, no conversion needed.");
            return imageBytes;
        }

        public async Task<string> CorrectImageOrientationAsync(string base64Image)
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

        public async Task<Image<Rgba32>> CropAsync(byte[] imageBytes)
        {
            var image = Image.Load<Rgba32>(imageBytes);
            int targetWidth = 350;
            int targetHeight = 420;

            var correctedImage = await CorrectImageOrientationAsync(ConvertToBase64(image));

            var bbox = await GetCoinBoundingBoxAsync(Convert.FromBase64String(correctedImage));
            byte[] correctedImageBytes = Convert.FromBase64String(correctedImage);
            var correctedImageRgba32 = Image.Load<Rgba32>(correctedImageBytes);

            Image<Rgba32> cropped;

            if (bbox != null)
            {
                _logger.LogInformation("Applying crop at the coin location recognized by the AI.");
                cropped = CropWithAI(correctedImageRgba32, bbox);
            }
            else
            {
                // Fallback: center square crop
                _logger.LogInformation("Applying center square crop since the AI couldn't recognize the coin.");
                cropped = CropImageCenter(image);
            }

            // Resize with Crop mode to fill target area without black bars
            cropped.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Crop
            }));

            //var debug = ConvertToBase64(cropped);

            return cropped;
        }

        private Image<Rgba32> CropImageCenter(Image<Rgba32> image)
        {
            _logger.LogInformation("Applying center square crop since the AI couldn't recognize the coin.");
            int side = Math.Min(image.Width, image.Height);
            int cropX = (image.Width - side) / 2;
            int cropY = (image.Height - side) / 2;
            return image.Clone(ctx => ctx.Crop(new Rectangle(cropX, cropY, side, side)));
        }

        private Image<Rgba32> CropWithAI(Image<Rgba32> image, BoundingBoxPercent bbox)
        {
            int x = (int)(bbox.Left * image.Width);
            int y = (int)(bbox.Top * image.Height);
            int w = (int)(bbox.Width * image.Width);
            int h = (int)(bbox.Height * image.Height);

            // Set padding to 11% to zoom in
            int padX = (int)(w * 0.05);
            int padY = (int)(h * 0.05);

            int cropX = Math.Max(x - padX, 0);
            int cropY = Math.Max(y - padY, 0);
            int cropW = Math.Min(w + 2 * padX, image.Width - cropX);
            int cropH = Math.Min(h + 2 * padY, image.Height - cropY);

            return image.Clone(ctx => ctx.Crop(new Rectangle(cropX, cropY, cropW, cropH)));
        }

        private async Task<BoundingBoxPercent> GetCoinBoundingBoxAsync(byte[] imageBytes)
        {
            try
            {
                var predictionKey = _configuration["CustomVisionPredictionKey"];
                var endpoint = _configuration["CustomVisionPredictionEndpoint"];
                var projectId = _configuration["CustomVisionProjectId"];
                var publishedName = _configuration["CustomVisionPublishedModelName"];

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
                _logger.LogWarning($"Bounding box of coin wasn't defined: {ex.Message}");
                return null;
            }
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
