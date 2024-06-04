using AutoMapper;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using CoinsManagerWebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CoinsManagerService.Controllers
{
    [Authorize(Roles = "Api.ReadWrite")]
    [ApiController]
    [Route("api")]
    public class CoinsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ICoinsRepo _coinsRepo;
        private readonly IAzureBlobService _azureBlobService;
        private const string _getCoinEndpointName = "GetCoinEndpoint";

        public CoinsController(IMapper mapper, ICoinsRepo coinsRepo, AzureBlobService azureBlobService)
        {
            _mapper = mapper;
            _coinsRepo = coinsRepo;
            _azureBlobService = azureBlobService;
        }

        [HttpGet("continents")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public ActionResult<IEnumerable<Continent>> Continents()
        {
            return Ok(_coinsRepo.GetAllContinents());
        }

        [HttpGet("continents/{continentId}")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
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
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
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
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
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
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public ActionResult<IEnumerable<Country>> GetCountriesByContinent(int continentId)
        {
            return Ok(_coinsRepo.GetCountriesByContinentId(continentId));
        }

        [HttpGet("countries/{countryId}/periods")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public ActionResult<IEnumerable<Period>> GetPeriodsByCountry(int countryId)
        {
            return Ok(_coinsRepo.GetPeriodsByCountryId(countryId));
        }

        [HttpGet("periods/{periodId}/coins")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public ActionResult<IEnumerable<CoinReadDto>> GetCoinsByPeriod(int periodId)
        {
            return Ok(_mapper.Map<IEnumerable<CoinReadDto>>(_coinsRepo.GetCoinsByPeriodId(periodId)));
        }  

        [HttpPost("coins")]
        [SwaggerResponse(201)]
        [SwaggerResponse(400)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public async Task<ActionResult<CoinReadDto>> CreateCoin([FromForm] CoinCreateDto coinCreateDto)
        {
            if (coinCreateDto == null)
            {
                return BadRequest();
            }

            if (coinCreateDto.File == null || coinCreateDto.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var periodName = _coinsRepo.GetPeriodById(coinCreateDto.Period).Period1;
            var country = _coinsRepo.GetCountryByPeriodId(coinCreateDto.Period);
            var countryName = country.Country1;
            var continentName = _coinsRepo.GetContinentByCountryId(country.Id).Continent1;

            var filePath = Path.Combine(continentName, countryName, periodName);
            var fileName = $"{coinCreateDto.CatalogId}_{coinCreateDto.Nominal}{coinCreateDto.Currency}_{coinCreateDto.Year}.jpg";

            var coinModel = _mapper.Map<Coin>(coinCreateDto);
            coinModel.PictPreviewPath = Path.Combine(filePath, fileName);

            _coinsRepo.CreateCoin(coinModel);
            _coinsRepo.SaveChanges();

            var containerName = "images";

            using (var stream = coinCreateDto.File.OpenReadStream())
            {
                var url = await _azureBlobService.UploadFileAsync(stream, coinModel.PictPreviewPath, containerName);
            }

            var coinReadDto = _mapper.Map<CoinReadDto>(coinModel);

            return CreatedAtRoute(_getCoinEndpointName, new { Id = coinReadDto.Id }, coinReadDto);
        }

        [HttpPut("coins/{coinId}")]
        [SwaggerResponse(204)]
        [SwaggerResponse(400)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
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
        [SwaggerResponse(204)]
        [SwaggerResponse(400)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
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
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
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