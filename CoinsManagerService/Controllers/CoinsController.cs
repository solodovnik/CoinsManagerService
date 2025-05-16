using AutoMapper;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using CoinsManagerService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly IAzureFunctionService _azureFunctionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CoinsController> _logger;
        private const string _getCoinEndpointName = "GetCoinEndpoint";

        public CoinsController(
            IMapper mapper, 
            ICoinsRepo coinsRepo, 
            IAzureBlobService azureBlobService,
            IAzureFunctionService azureFunctionService,
            IConfiguration configuration,
            ILogger<CoinsController> logger)
        {
            _mapper = mapper;
            _coinsRepo = coinsRepo;
            _azureBlobService = azureBlobService;
            _azureFunctionService = azureFunctionService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("continents")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public async Task<ActionResult> GetContinents()
        {
            var continents = await _coinsRepo.GetAllContinentsAsync();
            var continentDtos = _mapper.Map<IEnumerable<ContinentReadDto>>(continents);

            return Ok(continentDtos);
        }

        [HttpGet("continents/{continentId}")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
        public async Task<ActionResult<ContinentReadDto>> GetContinentByIdAsync(int continentId)
        {
            var continent = await _coinsRepo.GetContinentByIdAsync(continentId);

            if (continent != null)
            {
                return Ok(_mapper.Map<ContinentReadDto>(continent));
            }

            return NotFound();
        }

        [HttpGet("countries/{countryId}")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
        public async Task<ActionResult<CountryReadDto>> GetCountryById(int countryId)
        {
            var country = await _coinsRepo.GetCountryByIdAsync(countryId);

            if (country != null)
            {
                return Ok(_mapper.Map<CountryReadDto>(country));
            }

            return NotFound();
        }

        [HttpGet("coins/{id}", Name = _getCoinEndpointName)]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
        public async Task<ActionResult<CoinReadDto>> GetCoinById(int id)
        {
            var coin = await _coinsRepo.GetCoinByIdAsync(id);

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
        public async Task<ActionResult<IEnumerable<CountryReadDto>>> GetCountriesByContinent(int continentId)
        {            
            var countries = await _coinsRepo.GetCountriesByContinentIdAsync(continentId);
            var countriesDto = _mapper.Map<IEnumerable<CountryReadDto>>(countries);

            return Ok(countriesDto);
        }

        [HttpGet("countries/{countryId}/periods")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public async Task<ActionResult<IEnumerable<PeriodReadDto>>> GetPeriodsByCountry(int countryId)
        {
            var periods = await _coinsRepo.GetPeriodsByCountryIdAsync(countryId);
            var periodsDto = _mapper.Map<IEnumerable<PeriodReadDto>>(periods);

            return Ok(periodsDto);
        }

        [HttpGet("periods/{periodId}/coins")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public async Task<ActionResult<IEnumerable<CoinReadDto>>> GetCoinsByPeriod(int periodId)
        {
            var coins = await _coinsRepo.GetCoinsByPeriodIdAsync(periodId);
            var coinsDto = _mapper.Map<IEnumerable<CoinReadDto>>(coins);

            return Ok(coinsDto);
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
                _logger.LogWarning("Received null CoinCreateDto.");
                return BadRequest("Coin data cannot be null.");
            }

            if (!ValidateCreateCoinRequest(coinCreateDto, out var errorMessage))
            {
                _logger.LogWarning("Validation failed for CreateCoin request: {ErrorMessage}", errorMessage);
                return BadRequest(errorMessage);
            }

            try
            {
                var filePath = await GetFilePathAsync(coinCreateDto);
                var fileName = $"{coinCreateDto.CatalogId}_{coinCreateDto.Nominal}{coinCreateDto.Currency}_{coinCreateDto.Year}.jpg";

                var coinModel = _mapper.Map<Coin>(coinCreateDto);
                coinModel.PictPreviewPath = Path.Combine(filePath, fileName);

                // Convert images to Base64
                _logger.LogInformation("Converting obverse image to Base64.");
                var obverseBase64 = await ConvertImageToBase64Async(coinCreateDto.ObverseImage);

                _logger.LogInformation("Converting reverse image to Base64.");
                var reverseBase64 = await ConvertImageToBase64Async(coinCreateDto.ReverseImage);

                // Call image processing function
                _logger.LogInformation("Calling image processing function.");
                var mergedImageBase64 = await CallImageProcessingFunction(obverseBase64, reverseBase64);

                if (string.IsNullOrEmpty(mergedImageBase64))
                {
                    _logger.LogError("Image processing function returned an empty result.");
                    return StatusCode(500, "Failed to process images.");
                }

                // Convert merged image back to stream and upload
                var mergedImageBytes = Convert.FromBase64String(mergedImageBase64);
                using var stream = new MemoryStream(mergedImageBytes);

                _logger.LogInformation("Uploading processed image to Azure Blob Storage.");
                await _azureBlobService.UploadFileAsync(stream, coinModel.PictPreviewPath, containerName);

                // Save coin to repository
                _logger.LogInformation("Creating coin in repository.");
                await _coinsRepo.CreateCoin(coinModel);

                var coinReadDto = _mapper.Map<CoinReadDto>(coinModel);

                _logger.LogInformation("Coin successfully created with ID: {CoinId}", coinReadDto.Id);
                return CreatedAtRoute(_getCoinEndpointName, new { Id = coinReadDto.Id }, coinReadDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a coin.");
                return StatusCode(500, "An error occurred while creating a coin.");
            }
        }


        [HttpPut("coins/{coinId}")]
        [SwaggerResponse(204)]
        [SwaggerResponse(400)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
        public async Task<ActionResult> UpdateCoin(int coinId, CoinUpdateDto coinUpdateDto)
        {
            if (coinUpdateDto == null)
            {
                return BadRequest();
            }

            var coinToUpdate = await _coinsRepo.GetCoinByIdAsync(coinId);

            if (coinToUpdate == null)
            {
                return NotFound($"Coin with id {coinId} was not found");
            }

            _mapper.Map(coinUpdateDto, coinToUpdate);
            await _coinsRepo.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("coins/{coinId}")]
        [SwaggerResponse(204)]
        [SwaggerResponse(400)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
        public async Task<ActionResult> PartiallyUpdateCoin(int coinId, JsonPatchDocument<CoinUpdateDto> patchDocument)
        {
            var coinEntity = await _coinsRepo.GetCoinByIdAsync(coinId);

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
            await _coinsRepo.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("coins/{coinId}")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(404)]
        [SwaggerResponse(500)]
        public async Task<ActionResult<CoinReadDto>> DeleteCoin(int coinId)
        {
            try
            {
                var coinToDelete = await _coinsRepo.GetCoinByIdAsync(coinId);

                if (coinToDelete == null)
                {
                    return NotFound($"Coin with id {coinId} was not found");
                }

                await _coinsRepo.RemoveCoin(coinToDelete);
                await _azureBlobService.DeleteFileAsync(coinToDelete.PictPreviewPath, _configuration["ImagesContainerName"]);
                await _azureBlobService.DeleteFileAsync(coinToDelete.PictPreviewPath, _configuration["ThumbnailsContainerName"]);
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error occured while trying to delete data");
            }
        }

        private async Task<string> GetFilePathAsync(CoinCreateDto coinCreateDto)
        {
            var period = await _coinsRepo.GetPeriodByIdAsync(coinCreateDto.Period ?? 0);
            var country = await _coinsRepo.GetCountryByPeriodIdAsync(coinCreateDto.Period ?? 0);            
            var continent = await _coinsRepo.GetContinentByCountryIdAsync(country.Id);
            return $"/{Path.Combine(continent.Name, country.Name, period.Name)}";
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

            var requestPayload = new { ObverseImageBase64 = obverseBase64, ReverseImageBase64 = reverseBase64 };            
            var response = await _azureFunctionService.CallFunctionAsync(functionUrl, functionKey, requestPayload);

            if (!response.IsSuccessStatusCode) return null;
          
            var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
            return responseContent.GetProperty("mergedImageBase64").GetString();
        }
    }
}