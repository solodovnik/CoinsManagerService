using CoinsManagerService.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoinsManagerService.Data
{
    public class CoinsRepo : ICoinsRepo
    {
        private readonly AppDbContext _context;

        public CoinsRepo(AppDbContext context)
        {
            _context = context;
        }

        public void CreateCoin(Coin coin)
        {
            if (coin == null)
            {
                throw new ArgumentNullException(nameof(coin));
            }

            _context.Coins.Add(coin);
        }

        public IEnumerable<Continent> GetAllContinents()
        {
            return _context.Continents;
        }

        public Coin GetCoinById(int id)
        {
            return _context.Coins.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<Coin> GetCoinsByPeriodId(int periodId)
        {
            return _context.Coins.Where(x => x.Period == periodId);
        }

        public Continent GetContinentById(int id)
        {
            return _context.Continents.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<Country> GetCountriesByContinentId(int continentId)
        {
            return _context.Countries.Where(x => x.Continent == continentId);
        }

        public Country GetCountryById(int id)
        {
            return _context.Countries.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<Period> GetPeriodsByCountryId(int countryId)
        {
            return _context.Periods.Where(x => x.Country == countryId);
        }

        public void RemoveCoin(Coin coin)
        {
            if (coin == null)
            {
                throw new ArgumentNullException(nameof(coin));
            }

            _context.Coins.Remove(coin);
        }

        public bool SaveChanges()
        {
            return (_context.SaveChanges() >= 0);
        }
    }
}
