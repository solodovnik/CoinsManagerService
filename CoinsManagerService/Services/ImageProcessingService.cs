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
        private readonly HttpClient _httpClient;

        public ImageProcessingService(ILogger<ImageProcessingService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
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

        public Stream ConvertToPng(Stream imageStream)
        {
            using var image = new MagickImage(imageStream);

            if (image.Format == MagickFormat.Heic || image.Format == MagickFormat.Heif)
            {
                _logger.LogInformation("Converting image from HEIC to PNG.");

                var outputStream = new MemoryStream();
                image.Write(outputStream, MagickFormat.Png);
                outputStream.Position = 0;
                return outputStream;
            }

            _logger.LogInformation($"Image format is {image.Format}, no conversion needed.");

            // Return original stream copy to avoid returning disposed input
            var originalCopy = new MemoryStream();
            imageStream.Position = 0;
            imageStream.CopyTo(originalCopy);
            originalCopy.Position = 0;
            return originalCopy;
        }

        public async Task<Stream> CorrectImageOrientationAsync(Stream imageStream)
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync<Rgba32>(imageStream);

            var exif = image.Metadata.ExifProfile;
            ushort orientation = 1;

            var orientationEntry = exif?.Values.FirstOrDefault(v => v.Tag == ExifTag.Orientation);
            if (orientationEntry?.GetValue() is ushort parsed)
            {
                orientation = parsed;
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

            var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }

        public async Task<Image<Rgba32>> CropAsync(Stream imageStream)
        {
            const int targetWidth = 420;
            const int targetHeight = 420;

            imageStream.Position = 0;
  
            using var correctedStream = await CorrectImageOrientationAsync(imageStream);

            // Copy corrected stream into a buffer so we can both read the image and extract bytes
            using var bufferedStream = new MemoryStream();
            await correctedStream.CopyToAsync(bufferedStream);
            bufferedStream.Position = 0;
          
            using var correctedImage = await Image.LoadAsync<Rgba32>(bufferedStream);
         
            var imageBytes = bufferedStream.ToArray();

            var bbox = await GetCoinBoundingBoxAsync(imageBytes);
            if (bbox == null)
            {
                _logger.LogError("AI couldn't recognize the coin.");
                return null;
            }

            _logger.LogInformation("Applying crop at the coin location recognized by the AI.");
            var cropped = CropWithBoundingBox(correctedImage, bbox);

            cropped.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Crop
            }));

            return cropped;
        }

        public Image<Rgba32> MergeImagesSideBySide(Image<Rgba32> leftImage, Image<Rgba32> rightImage)
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

        private Image<Rgba32> CropWithBoundingBox(Image<Rgba32> image, BoundingBoxPercent bbox)
        {
            int x = (int)(bbox.Left * image.Width);
            int y = (int)(bbox.Top * image.Height);
            int w = (int)(bbox.Width * image.Width);
            int h = (int)(bbox.Height * image.Height);

            // Set padding to 5% to zoom in
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
                var url = $"{endpoint}/customvision/v3.0/Prediction/{projectId}/detect/iterations/{publishedName}/image";

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new ByteArrayContent(imageBytes)
                };
                request.Headers.Add("Prediction-Key", predictionKey);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                
                var response = await _httpClient.SendAsync(request);
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
