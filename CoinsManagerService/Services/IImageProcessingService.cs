using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Threading.Tasks;

namespace CoinsManagerService.Services
{
    public interface IImageProcessingService
    {
        Stream ConvertToPng(Stream imageBytes);
        Task<Image<Rgba32>> CropAsync(Stream imageStream);
        Task<Stream> CorrectImageOrientationAsync(Stream imageStream);
        Image<Rgba32> CreateThumbnail(Image<Rgba32> leftImage, Image<Rgba32> rightImage);
    }
}
