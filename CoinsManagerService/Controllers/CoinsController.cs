using AutoMapper;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using CoinsManagerWebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
        private readonly IConfiguration _configuration;
        private const string _getCoinEndpointName = "GetCoinEndpoint";

        public CoinsController(IMapper mapper, ICoinsRepo coinsRepo, IAzureBlobService azureBlobService, IConfiguration configuration)
        {
            _mapper = mapper;
            _coinsRepo = coinsRepo;
            _azureBlobService = azureBlobService;
            _configuration = configuration;
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
            string containerName = _configuration["ImagesContainerName"];

            if (coinCreateDto == null)
            {
                return BadRequest("Coin data cannot be null.");
            }

            if (!ValidateCreateCoinRequest(coinCreateDto, out var errorMessage))
            {
                return BadRequest(errorMessage);
            }

            var filePath = GetFilePath(coinCreateDto);
            var fileName = $"{coinCreateDto.CatalogId}_{coinCreateDto.Nominal}{coinCreateDto.Currency}_{coinCreateDto.Year}.jpg";

            var coinModel = _mapper.Map<Coin>(coinCreateDto);
            coinModel.PictPreviewPath = Path.Combine(filePath, fileName);

            var obverseBase64 = await ConvertImageToBase64Async(coinCreateDto.ObverseImage);
            var reverseBase64 = await ConvertImageToBase64Async(coinCreateDto.ReverseImage);

            var mergedImageBase64 = await CallImageProcessingFunction(obverseBase64, reverseBase64);
            if (string.IsNullOrEmpty(mergedImageBase64))
            {
                return StatusCode(500, "Failed to process images.");
            }

            // Convert merged image back to stream
            var mergedImageBytes = Convert.FromBase64String(mergedImageBase64);
            using var stream = new MemoryStream(mergedImageBytes);

            await _azureBlobService.UploadFileAsync(stream, coinModel.PictPreviewPath, containerName);

            _coinsRepo.CreateCoin(coinModel);
            _coinsRepo.SaveChanges();

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

        private string GetFilePath(CoinCreateDto coinCreateDto)
        {
            var periodName = _coinsRepo.GetPeriodById(coinCreateDto.Period ?? 0).Name;
            var country = _coinsRepo.GetCountryByPeriodId(coinCreateDto.Period ?? 0);
            var countryName = country.Name;
            var continentName = _coinsRepo.GetContinentByCountryId(country.Id).Name;
            return Path.Combine(continentName, countryName, periodName);
        }

        private bool ValidateCreateCoinRequest(CoinCreateDto coinCreateDto, out string errorMessage)
        {
            if (coinCreateDto.ObverseImage == null || coinCreateDto.ObverseImage.Length == 0)
            {
                errorMessage = "No coin obverse image uploaded.";
                return false;
            }
            if (coinCreateDto.ReverseImage == null || coinCreateDto.ReverseImage.Length == 0)
            {
                errorMessage = "No coin reverse image uploaded.";
                return false;
            }
            if (coinCreateDto.Period == null)
            {
                errorMessage = "Period can't be null.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        private async Task<string> ConvertImageToBase64Async(IFormFile image)
        {
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        private async Task<string> CallImageProcessingFunction(string obverseBase64, string reverseBase64)
        {
            var functionUrl = _configuration["AzureFunctions:ProcessImages:Url"];
            var functionKey = _configuration["AzureFunctions:ProcessImages:FunctionKey"];

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-functions-key", functionKey);

            var requestPayload = new { ObverseImageBase64 = obverseBase64, ReverseImageBase64 = reverseBase64 };
            var response = await httpClient.PostAsJsonAsync(functionUrl, requestPayload);

            if (!response.IsSuccessStatusCode) return null;
          
            var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
            return responseContent.GetProperty("mergedImageBase64").GetString();
        }
    }
}