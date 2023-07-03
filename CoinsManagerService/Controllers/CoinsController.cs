﻿using AutoMapper;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace CoinsManagerService.Controllers
{
    [Authorize(Roles = "Api.ReadWrite")]
    [ApiController]
    [Route("api")]
    public class CoinsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ICoinsRepo _coinsRepo;
        private const string _getCoinEndpointName = "GetCoinEndpoint";

        public CoinsController(IMapper mapper, ICoinsRepo coinsRepo)
        {
            _mapper = mapper;
            _coinsRepo = coinsRepo;
        }

        [HttpGet("continents")]
        public ActionResult<IEnumerable<Continent>> Continents()
        {
            return Ok(_coinsRepo.GetAllContinents());
        }

        [HttpGet("continents/{continentId}")]
        public ActionResult<Continent> GetContinentById(int continentId)
        {
            var continent = _coinsRepo.GetContinentById(continentId);

            if (continent != null)
            {
                return Ok(continent);
            }

            return NotFound();
        }

        [HttpGet("countries/{countryId}")]
        public ActionResult<Country> GetCountryById(int countryId)
        {
            var country = _coinsRepo.GetCountryById(countryId);

            if (country != null)
            {
                return Ok(country);
            }

            return NotFound();
        }

        [HttpGet("coins/{id}", Name = _getCoinEndpointName)]
        public ActionResult<CoinReadDto> GetCoinById(int id)
        {
            var coin = _coinsRepo.GetCoinById(id);

            if (coin != null)
            {
                return Ok(_mapper.Map<CoinReadDto>(coin));
            }

            return NotFound();
        }

        [HttpGet("continents/{continentId}/countries")]
        public ActionResult<IEnumerable<Country>> GetCountriesByContinent(int continentId)
        {
            return Ok(_coinsRepo.GetCountriesByContinentId(continentId));
        }

        [HttpGet("countries/{countryId}/periods")]
        public ActionResult<IEnumerable<Period>> GetPeriodsByCountry(int countryId)
        {
            return Ok(_coinsRepo.GetPeriodsByCountryId(countryId));
        }

        [HttpGet("periods/{periodId}/coins")]
        public ActionResult<IEnumerable<CoinReadDto>> GetCoinsByPeriod(int periodId)
        {
            return Ok(_mapper.Map<IEnumerable<CoinReadDto>>(_coinsRepo.GetCoinsByPeriodId(periodId)));
        }  

        [HttpPost("coins")]
        public ActionResult<CoinReadDto> CreateCoin(CoinCreateDto coinCreateDto)
        {
            if (coinCreateDto == null)
            {
                return BadRequest();
            }

            var coinModel = _mapper.Map<Coin>(coinCreateDto);

            _coinsRepo.CreateCoin(coinModel);
            _coinsRepo.SaveChanges();

            var coinReadDto = _mapper.Map<CoinReadDto>(coinModel);

            return CreatedAtRoute(_getCoinEndpointName, new { Id = coinReadDto.Id }, coinReadDto);
        }

        [HttpPut("coins/{coinId}")]
        public ActionResult UpdateCoin(int coinId, CoinUpdateDto coinUpdateDto)
        {
            if (coinUpdateDto == null)
            {
                return BadRequest();
            }

            var coinToUpdate = _coinsRepo.GetCoinById(coinId);

            if (coinToUpdate == null)
            {
                return NotFound($"Coin with id {coinId} was not found");
            }

            _mapper.Map(coinUpdateDto, coinToUpdate);
            _coinsRepo.SaveChanges();

            return NoContent();
        }

        [HttpPatch("coins/{coinId}")]
        public ActionResult PartiallyUpdateCoin(int coinId, JsonPatchDocument<CoinUpdateDto> patchDocument)
        {
            var coinEntity = _coinsRepo.GetCoinById(coinId);

            if (coinEntity == null)
            {
                return NotFound($"Coin with id {coinId} was not found");
            }

            var coinToPatch = _mapper.Map<CoinUpdateDto>(coinEntity);
            patchDocument.ApplyTo(coinToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryValidateModel(coinToPatch))
            {
                return BadRequest(ModelState);
            }

            _mapper.Map(coinToPatch, coinEntity);
            _coinsRepo.SaveChanges();

            return NoContent();
        }

        [HttpDelete("coins/{coinId}")]
        public ActionResult<CoinReadDto> DeleteCoin(int coinId)
        {
            try
            {
                var coinToDelete = _coinsRepo.GetCoinById(coinId);

                if (coinToDelete == null)
                {
                    return NotFound($"Coin with id {coinId} was not found");
                }

                _coinsRepo.RemoveCoin(coinToDelete);
                _coinsRepo.SaveChanges();
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error occured while trying to delete data");
            }
        }
    }
}