using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading.Tasks;

namespace CoinsManagerService.Services
{
    public interface IImageProcessingService
    {
        byte[] ConvertToPng(byte[] imageBytes);
        string ConvertToBase64(Image<Rgba32> image);
        Task<Image<Rgba32>> CropAsync(byte[] imageBytes);
        Task<string> CorrectImageOrientationAsync(string base64Image);
    }
}
