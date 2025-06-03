using CoinsManagerService.Dtos;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoinsManagerService.Services
{
    public interface ICoinSearchService
    {
        Task<IEnumerable<CoinReadDto>> FindMatchesAsync(IFormFile obverse, IFormFile reverse, int topCount);
    }
}
