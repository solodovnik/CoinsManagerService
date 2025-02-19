using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoinsManagerService.Services
{
    public class AzureFunctionService : IAzureFunctionService
    {
        HttpClient _httpClient;

        public AzureFunctionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<HttpResponseMessage> CallFunctionAsync(string functionUrl, string functionKey, object requestPayload)
        {
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", functionKey);
            var response = await _httpClient.PostAsJsonAsync(functionUrl, requestPayload);
            return response;
        }
    }
}
