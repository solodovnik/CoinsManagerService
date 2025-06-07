using AutoMapper;
using Azure.Storage.Blobs.Models;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<CoinsController> _logger;
        private readonly IImageProcessingService _imageProcessingService;
        private const string _getCoinEndpointName = "GetCoinEndpoint";

        public CoinsController(
            IMapper mapper, 
            ICoinsRepo coinsRepo, 
            IAzureBlobService azureBlobService,
            IAzureFunctionService azureFunctionService,
            IConfiguration configuration,
            IImageProcessingService imageProcessingService,
            ILogger<CoinsController> logger)
        {
            _mapper = mapper;
            _coinsRepo = coinsRepo;
            _azureBlobService = azureBlobService;
            _configuration = configuration;
            _imageProcessingService = imageProcessingService;
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

        [HttpGet("cointypes")]
        [SwaggerResponse(200)]
        [SwaggerResponse(401)]
        [SwaggerResponse(500)]
        public async Task<ActionResult> GetCoinTypes()
        {
            var coinTypes = await _coinsRepo.GetCoinTypesAsync();
            var coinTypeDtos = _mapper.Map<IEnumerable<CoinTypeReadDto>>(coinTypes);

            return Ok(coinTypeDtos);
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
            string imagesContainerName = _configuration["ImagesContainerName"];
            string thumbnailsContainerName = _configuration["ThumbnailsContainerName"];

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
                coinModel.PictPreviewPath = string.Join("/", filePath, fileName);

                using var obverseStream = coinCreateDto.ObverseImage.OpenReadStream();
                using var reverseStream = coinCreateDto.ReverseImage.OpenReadStream();

                var croppedObverseTask = _imageProcessingService.CropAsync(obverseStream);
                var croppedReverseTask = _imageProcessingService.CropAsync(reverseStream);

                await Task.WhenAll(croppedObverseTask, croppedReverseTask);

                var croppedObverse = croppedObverseTask.Result;
                var croppedReverse = croppedReverseTask.Result;

                using var obverseStreamOut = new MemoryStream();
                using var reverseStreamOut = new MemoryStream();

                await croppedObverse.SaveAsJpegAsync(obverseStreamOut);
                await croppedReverse.SaveAsJpegAsync(reverseStreamOut);

                obverseStreamOut.Position = 0;
                reverseStreamOut.Position = 0;

                var thumbnailImage = _imageProcessingService.CreateThumbnail(croppedObverse, croppedReverse);

                using var thumbnailImageStream = new MemoryStream();
                thumbnailImage.Save(thumbnailImageStream, new JpegEncoder());
                thumbnailImageStream.Position = 0;

                _logger.LogInformation("Uploading processed images to Azure Blob Storage.");
                await _azureBlobService.UploadFileAsync(thumbnailImageStream, coinModel.PictPreviewPath, thumbnailsContainerName);

                string directory = Path.GetDirectoryName(coinModel.PictPreviewPath) ?? ""; ;
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(coinModel.PictPreviewPath);
                string extension = Path.GetExtension(coinModel.PictPreviewPath);

                await _azureBlobService.UploadFileAsync(obverseStreamOut, $"{directory}/{nameWithoutExtension}_obverse{extension}", imagesContainerName);
                await _azureBlobService.UploadFileAsync(reverseStreamOut, $"{directory}/{nameWithoutExtension}_reverse{extension}", imagesContainerName);

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
                var embeddingsToDelete = await _coinsRepo.GetCoinEmbeddingsByCoinId(coinId);
                if (embeddingsToDelete != null)
                {
                    await _coinsRepo.RemoveCoinEmbeddings(embeddingsToDelete);
                }

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
            return $"/{string.Join("/", continent.Name, country.Name, period.Name)}";
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
    }
}