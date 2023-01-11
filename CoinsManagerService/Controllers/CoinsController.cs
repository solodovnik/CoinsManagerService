using AutoMapper;
using CoinsManagerService.Dtos;
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
        private readonly IMapper _mapper;

        public CoinsController(IMapper mapper)
        {
            dbContext = new CoinsCollectionContext();
            _mapper = mapper;
        }

        [HttpGet("GetCoinsByPeriod")]
        public IEnumerable<Coin> GetCoinsByPeriod(int periodId)
        {
           return (periodId == 0) ? dbContext.Coins : dbContext.Coins.Where(x => x.Period == periodId);
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

        [HttpGet("{id}")]
        public ActionResult<CoinReadDto> GetCoinById(int id)
        {
            var coinItem = dbContext.Coins.FirstOrDefault(x => x.Id == id);
            if (coinItem != null)
            {
                return Ok(_mapper.Map<CoinReadDto>(coinItem));
            }

            return NotFound();
        }

        [HttpPost]
        public ActionResult<CoinReadDto> CreateCoin(CoinCreateDto coinCreateDto)
        {
            var coinModel = _mapper.Map<Coin>(coinCreateDto);
            dbContext.Add(coinModel);
            dbContext.SaveChanges();

            var coinReadDto = _mapper.Map<CoinReadDto>(coinModel);

            return CreatedAtRoute(nameof(GetCoinById), new { Id = coinReadDto.Id }, coinReadDto);
        }
        
    }
}