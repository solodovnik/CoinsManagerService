using CoinsManagerService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace CoinsManagerService.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class CoinsController : ControllerBase
    {
        private readonly CoinsCollectionContext dbContext;
        public CoinsController()
        {
            dbContext = new CoinsCollectionContext();
        }

        [HttpGet("{periodId}")]
        public IEnumerable<Coin> GetCoinsByPeriod(int periodId)
        {
           return (periodId == 0) ? dbContext.Coins : dbContext.Coins.Where(x => x.Period == periodId);
        }

        [HttpGet]
        public IEnumerable<Coin> GetAllCoins()
        {
            return dbContext.Coins;
        }

        [HttpGet("GetAllContinents")]
        public IEnumerable<Continent> GetAllContinents()
        {
            return dbContext.Continents;
        }

        [HttpGet("GetCountriesByContinent")]
        public IEnumerable<Country> GetCountriesByContinent(int continentId)
        {
            return dbContext.Countries.Where(x => x.Continent == continentId);
        }

        [HttpGet("Countries/{countryId}")]
        public Country GetCountryById(int countryId)
        {
            return dbContext.Countries.FirstOrDefault(x => x.Id == countryId);
        }

        [HttpGet("Continents/{continentId}")]
        public Continent GetContinentById(int continentId)
        {
            return dbContext.Continents.FirstOrDefault(x => x.Id == continentId);
        }

        [HttpGet("GetPeriodsByCountry")]
        public IEnumerable<Period> GetPeriodsByCountry(int countryId)
        {
            return dbContext.Periods.Where(x => x.Country == countryId);
        }

        //public IActionResult GetAllCoins()
        //{
        //    var coins = dbContext.Coins;
        //    if (!coins.Any())
        //        return new NoContentResult();

        //    return new ObjectResult(coins);
        //}
    }
}