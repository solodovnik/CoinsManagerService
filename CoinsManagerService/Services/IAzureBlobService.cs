using System.IO;
using System.Threading.Tasks;

namespace CoinsManagerService.Services
{
    public interface IAzureBlobService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName);
    }
}
