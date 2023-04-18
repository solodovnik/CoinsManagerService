using AutoMapper;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CoinsManagerService.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class CoinsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ICoinsRepo _coinsRepo;

        public CoinsController(IMapper mapper, ICoinsRepo coinsRepo)
        {
            _mapper = mapper;
            _coinsRepo = coinsRepo;
        }

        [HttpGet("GetCoinsByPeriod")]
        public ActionResult<IEnumerable<CoinReadDto>> GetCoinsByPeriod(int periodId)
        {
            return Ok(_mapper.Map<IEnumerable<CoinReadDto>>(_coinsRepo.GetCoinsByPeriodId(periodId)));
        }

        [HttpGet("GetAllContinents")]
        public ActionResult<IEnumerable<Continent>> GetAllContinents()
        {
            return Ok(_coinsRepo.GetAllContinents());
        }

        [HttpGet("GetCountriesByContinent")]
        public ActionResult<IEnumerable<Country>> GetCountriesByContinent(int continentId)
        {
            return Ok(_coinsRepo.GetCountriesByContinentId(continentId));
        }

        [HttpGet("Countries/{countryId}")]
        public ActionResult<Country> GetCountryById(int countryId)
        {
            var country = _coinsRepo.GetCountryById(countryId);

            if (country != null)
            {
                return Ok(country);
            }

            return NotFound();
        }

        [HttpGet("Continents/{continentId}")]
        public ActionResult<Continent> GetContinentById(int continentId)
        {
            var continent = _coinsRepo.GetContinentById(continentId);

            if (continent != null)
            {
                return Ok(continent);
            }

            return NotFound();
        }

        [HttpGet("GetPeriodsByCountry")]
        public ActionResult<IEnumerable<Period>> GetPeriodsByCountry(int countryId)
        {
            return Ok(_coinsRepo.GetPeriodsByCountryId(countryId));
        }

        [HttpGet("{id}")]
        public ActionResult<CoinReadDto> GetCoinById(int id)
        {
            var coin = _coinsRepo.GetCoinById(id);

            if (coin != null)
            {
                return Ok(_mapper.Map<CoinReadDto>(coin));
            }

            return NotFound();
        }

        [HttpPost]
        public ActionResult<CoinReadDto> CreateCoin(CoinCreateDto coinCreateDto)
        {
            var coinModel = _mapper.Map<Coin>(coinCreateDto);

            _coinsRepo.CreateCoin(coinModel);
            _coinsRepo.SaveChanges();

            var coinReadDto = _mapper.Map<CoinReadDto>(coinModel);

            return CreatedAtRoute(nameof(GetCoinById), new { Id = coinReadDto.Id }, coinReadDto);
        }

    }
}