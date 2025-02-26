using CoinsManagerService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoinsManagerService.Data
{
    public class CoinsRepo : ICoinsRepo
    {
        private readonly AppDbContext _context;

        public CoinsRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateCoin(Coin coin)
        {
            ArgumentNullException.ThrowIfNull(coin);
            _context.Coins.Add(coin);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Continent>> GetAllContinentsAsync()
        {
            return await _context.Continents.ToListAsync();
        }

        public async Task<Coin> GetCoinByIdAsync(int id)
        {
            return await _context.Coins.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Coin>> GetCoinsByPeriodIdAsync(int periodId)
        {
            return await _context.Coins.Where(x => x.Period == periodId).ToListAsync();
        }

        public async Task<Continent> GetContinentByCountryIdAsync(int countryId)
        {
            var country = await _context.Countries.FirstOrDefaultAsync(x => x.Id == countryId);
            if (country == null)
            {
                throw new InvalidOperationException($"Country with id = {countryId} not found.");
            }
            var continentId = country.Continent;
            return await GetContinentByIdAsync(continentId);
        }

        public async Task<Continent> GetContinentByIdAsync(int id)
        {
            return await _context.Continents.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Country>> GetCountriesByContinentIdAsync(int continentId)
        {
            return await _context.Countries.Where(x => x.Continent == continentId).ToListAsync();
        }

        public async Task<Country> GetCountryByIdAsync(int id)
        {
            return await _context.Countries.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Country> GetCountryByPeriodIdAsync(int periodId)
        {
            var period = await _context.Periods.FirstOrDefaultAsync(x => x.Id == periodId);
            if (period == null)
            {
                throw new InvalidOperationException($"Period with id = {periodId} not found.");
            }
            var countryId = period.Country;
            return await GetCountryByIdAsync(countryId);
        }

        public async Task<Period> GetPeriodByIdAsync(int id)
        {
            return await _context.Periods.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Period>> GetPeriodsByCountryIdAsync(int countryId)
        {
            return await _context.Periods.Where(x => x.Country == countryId).ToListAsync();
        }

        public async Task RemoveCoin(Coin coin)
        {
            ArgumentNullException.ThrowIfNull(coin);
            _context.Coins.Remove(coin);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() >= 0);
        }
    }
}
