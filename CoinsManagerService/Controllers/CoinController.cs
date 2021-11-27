using CoinsManagerService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace CoinsManagerService.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class CoinController : ControllerBase
    {
        private readonly CoinsCollectionContext dbContext;
        public CoinController()
        {
            dbContext = new CoinsCollectionContext();
        }

        [HttpGet("GetCoinsByPeriod")]
        public IEnumerable<Coin> GetCoinsByPeriod([FromQuery]int periodId)
        {
           return (periodId == 0) ? dbContext.Coins : dbContext.Coins.Where(x => x.Period == periodId);
        }

        [HttpGet("GetAllCoins")]
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
        public IEnumerable<Country> GetCountriesByContinent([FromQuery] int continentId)
        {
            return dbContext.Countries.Where(x => x.Continent == continentId);
        }

        [HttpGet("GetPeriodsByCountry")]
        public IEnumerable<Period> GetPeriodsByCountry([FromQuery] int countryId)
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