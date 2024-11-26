using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CoinsManagerCreateThumbnailFunction
{
    public class Function
    {
        private readonly ILogger<Function> _logger;

        public Function(ILogger<Function> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function))]
        public async Task Run([BlobTrigger("%ImagesContainerName%/{name}", Connection = "BlobStorageTrigger")] Stream stream, string name)
        {
            _logger.LogInformation($"Processing blob: {name}");

            try
            { 
                string thumbnailsContainerName = Environment.GetEnvironmentVariable("ThumbnailsContainerName") ?? string.Empty;
                string thumbnailPath = $"{thumbnailsContainerName}/{name}";

                // Resize the image
                using (var image = Image.Load(stream))
                {
                    int newWidth = 300;
                    int newHeight = (int)(image.Height * (newWidth / (float)image.Width));
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                    _logger.LogInformation($"Blob {name} is processed, trying to upload");
                    // Save the resized image to a memory stream
                    using (var outputStream = new MemoryStream())
                    {
                        image.SaveAsJpeg(outputStream);
                        outputStream.Position = 0;

                        // Upload the thumbnail to the thumbnails container
                        var blobClient = new BlobClient(
                            Environment.GetEnvironmentVariable("BlobStorageTrigger"),
                            thumbnailsContainerName,
                            name);

                        await blobClient.UploadAsync(outputStream, overwrite: true);
                    }
                }

                _logger.LogInformation($"Thumbnail created: {thumbnailPath}");
            }
            catch (SixLabors.ImageSharp.UnknownImageFormatException uex)
            {
                _logger.LogError($"Failed to decode image: {name}. {uex.Message}");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to process blob: {name}. Error: {ex.Message}");
            }
        }
    }
}
