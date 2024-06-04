using CoinsManagerService.Models;
using System.Collections.Generic;

namespace CoinsManagerService.Data
{
    public interface ICoinsRepo
    {
        bool SaveChanges();
        
        IEnumerable<Continent> GetAllContinents();
        Country GetCountryById(int id);
        Country GetCountryByPeriodId(int periodId);
        Period GetPeriodById(int id);
        Continent GetContinentById(int id);
        Continent GetContinentByCountryId(int countryId);
        Coin GetCoinById(int id);
        IEnumerable<Coin> GetCoinsByPeriodId(int periodId);
        IEnumerable<Country> GetCountriesByContinentId(int continentId);
        IEnumerable<Period> GetPeriodsByCountryId(int countryId);       

        void CreateCoin(Coin coin);
        void RemoveCoin(Coin coin);
    }
}
