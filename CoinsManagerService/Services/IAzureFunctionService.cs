using System.Net.Http;
using System.Threading.Tasks;

namespace CoinsManagerService.Services
{
    public interface IAzureFunctionService
    {
        Task<HttpResponseMessage> CallFunctionAsync(string functionUrl, string functionKey, object requestPayload);
    }
}
