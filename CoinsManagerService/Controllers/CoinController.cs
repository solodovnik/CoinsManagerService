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

        [HttpGet]
        public IEnumerable<Coin> GetByPeriod([FromQuery]int periodId)
        {
           return (periodId == 0) ? dbContext.Coins : dbContext.Coins.Where(x => x.Period == periodId);
        }

        [HttpGet("GetAll")]
        public IEnumerable<Coin> GetAll()
        {
            return dbContext.Coins;
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