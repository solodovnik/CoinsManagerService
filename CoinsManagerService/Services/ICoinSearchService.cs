using CoinsManagerService.Dtos;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CoinsManagerService.Services
{
    public interface ICoinSearchService
    {
        Task<CoinReadDto> FindMatchAsync(IFormFile obverse, IFormFile reverse);
    }
}
