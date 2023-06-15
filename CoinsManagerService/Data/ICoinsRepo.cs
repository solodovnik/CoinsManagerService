using CoinsManagerService.Models;
using System.Collections.Generic;

namespace CoinsManagerService.Data
{
    public interface ICoinsRepo
    {
        bool SaveChanges();
        
        IEnumerable<Continent> GetAllContinents();
        Country GetCountryById(int id);
        Continent GetContinentById(int id);
        Coin GetCoinById(int id);
        IEnumerable<Coin> GetCoinsByPeriodId(int periodId);
        IEnumerable<Country> GetCountriesByContinentId(int continentId);
        IEnumerable<Period> GetPeriodsByCountryId(int countryId);       

        void CreateCoin(Coin coin);
        void RemoveCoin(Coin coin);
    }
}
