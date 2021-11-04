using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CoinsManager.Web.Extensions;
using CoinsManager.Web.Models;

namespace CoinsManager.Web.Services
{
    public class CoinCatalogService : ICoinCatalogService
    {
        private readonly HttpClient _client;

        public CoinCatalogService(HttpClient client)
        {
            _client = client;
        }
        public async Task<IEnumerable<Coin>> GetCoinsByPeriod(int periodId)
        {
            var response = await _client.GetAsync("/v1/coin?periodId=1");
            return await response.ReadContentAs<List<Coin>>();
        }
    }
}
