﻿using CoinsManagerService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoinsManagerService.Data
{
    public interface ICoinsRepo
    {
        Task<bool> SaveChangesAsync();
        
        Task<IEnumerable<Continent>> GetAllContinentsAsync();
        Task<IEnumerable<CoinType>> GetCoinTypesAsync();
        Task<IEnumerable<CoinEmbeddings>> GetCoinEmbeddingsAsync();
        Task<Country> GetCountryByIdAsync(int id);
        Task<List<Coin>> GetCoinsByIdsAsync(IEnumerable<int> ids);
        Task<Country> GetCountryByPeriodIdAsync(int periodId);
        Task<Period> GetPeriodByIdAsync(int id);
        Task<Continent> GetContinentByIdAsync(int id);
        Task<Continent> GetContinentByCountryIdAsync(int countryId);
        Task<Coin> GetCoinByIdAsync(int id);
        Task<IEnumerable<Coin>> GetCoinsByPeriodIdAsync(int periodId);
        Task<IEnumerable<Country>> GetCountriesByContinentIdAsync(int continentId);
        Task<IEnumerable<Period>> GetPeriodsByCountryIdAsync(int countryId);
        Task<CoinEmbeddings> GetCoinEmbeddingsByCoinId(int coinId);

        Task CreateCoin(Coin coin);
        Task RemoveCoin(Coin coin);
        Task RemoveCoinEmbeddings(CoinEmbeddings coinEmbeddings);
    }
}
