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

/// <summary>
/// Function crops images of coin obverse and reverse and merges it into one image
/// </summary>
public static class Function
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
            // Parse the request
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ImageRequest>(requestBody);

            // Decode the images from base64
            var obverseImageBytes = Convert.FromBase64String(data.ObverseImageBase64);
            var reverseImageBytes = Convert.FromBase64String(data.ReverseImageBase64);

            // Process the images
            var obversePngBytes = ConvertToPngIfNeeded(obverseImageBytes, log);
            var reversePngBytes = ConvertToPngIfNeeded(reverseImageBytes, log);

            // Process the converted images with SixLabors.ImageSharp
            using var obverseImage = SixLabors.ImageSharp.Image.Load(obversePngBytes);
            using var reverseImage = SixLabors.ImageSharp.Image.Load(reversePngBytes);

            // Target dimensions for the merged image
            int targetWidth = 700;
            int halfTargetWidth = targetWidth / 2;
            int targetHeight = 420;

            // Resize images to 200% of the crop area dimensions to make the coin appear larger
            int enlargedWidth = (int)(halfTargetWidth * 2);
            int enlargedHeight = (int)(targetHeight * 2);

            obverseImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(enlargedWidth, enlargedHeight),
                Mode = ResizeMode.Max
            }));

            reverseImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(enlargedWidth, enlargedHeight),
                Mode = ResizeMode.Max
            }));

            // Calculate crop rectangle based on resized dimensions
            int obverseCropWidth = Math.Min(obverseImage.Width, halfTargetWidth);
            int obverseCropHeight = Math.Min(obverseImage.Height, targetHeight);
            int obverseCropX = (obverseImage.Width - obverseCropWidth) / 2;
            int obverseCropY = (obverseImage.Height - obverseCropHeight) / 2;
            obverseImage.Mutate(x => x.Crop(new Rectangle(obverseCropX, obverseCropY, obverseCropWidth, obverseCropHeight)));

            int reverseCropWidth = Math.Min(reverseImage.Width, halfTargetWidth);
            int reverseCropHeight = Math.Min(reverseImage.Height, targetHeight);
            int reverseCropX = (reverseImage.Width - reverseCropWidth) / 2;
            int reverseCropY = (reverseImage.Height - reverseCropHeight) / 2;
            reverseImage.Mutate(x => x.Crop(new Rectangle(reverseCropX, reverseCropY, reverseCropWidth, reverseCropHeight)));

            // Merge images side-by-side
            using var mergedImage = new Image<Rgba32>(targetWidth, targetHeight);
            mergedImage.Mutate(x =>
            {
                x.DrawImage(obverseImage, new Point(0, 0), 1f);
                x.DrawImage(reverseImage, new Point(halfTargetWidth, 0), 1f);
            });

            // Convert to base64
            using var outputStream = new MemoryStream();
            mergedImage.Save(outputStream, new JpegEncoder());
            var mergedImageBase64 = Convert.ToBase64String(outputStream.ToArray());

            return new OkObjectResult(new { MergedImageBase64 = mergedImageBase64 });
        }
        catch (Exception ex)
        {
            log.LogError($"Error processing images: {ex.Message}");
            return new BadRequestObjectResult($"Error: {ex.Message}");
        }
    }

    private static byte[] ConvertToPngIfNeeded(byte[] imageBytes, ILogger log)
    {
        using var image = new MagickImage(imageBytes);

        // Check the format of the image
        if (image.Format == MagickFormat.Heic || image.Format == MagickFormat.Heif)
        {
            log.LogInformation($"Converting image from HEIC to PNG.");
            return image.ToByteArray(MagickFormat.Png); // Convert HEIC/HEIF to PNG
        }

        log.LogInformation($"Image format is {image.Format}, no conversion needed.");
        return imageBytes; // Return original bytes if no conversion is needed
    }

    public class ImageRequest
    {
        public string ObverseImageBase64 { get; set; }
        public string ReverseImageBase64 { get; set; }
    }
}
