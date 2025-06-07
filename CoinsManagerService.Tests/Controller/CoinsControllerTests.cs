using AutoMapper;
using CoinsManagerService.Controllers;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using CoinsManagerService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CoinsManagerService.Tests.Controller
{
    public class CoinsControllerTests
    {
        private Mock<ICoinsRepo> _mockRepo;
        private Mock<ILogger<CoinsController>> _mockLogger;
        private Mock<IConfiguration> _mockConfig;
        private Mock<IMapper> _mockMapper;
        private Mock<IAzureBlobService> _mockAzureBlobStorage;
        private Mock<IAzureFunctionService> _mockAzureFunctionService;
        private Mock<IImageProcessingService> _mockImageProcessingService;
        private Mock<CoinsController> _mockController;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<ICoinsRepo>();
            _mockLogger = new Mock<ILogger<CoinsController>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockMapper = new Mock<IMapper>();
            _mockAzureBlobStorage = new Mock<IAzureBlobService>();
            _mockAzureFunctionService = new Mock<IAzureFunctionService>();
            _mockImageProcessingService = new Mock<IImageProcessingService>();
            _mockController = new Mock<CoinsController>(
                _mockMapper.Object,
                _mockRepo.Object,
                _mockAzureBlobStorage.Object,
                _mockAzureFunctionService.Object,                
                _mockConfig.Object,
                _mockImageProcessingService.Object,
                _mockLogger.Object) { CallBase = true }; ;
            _mockController.Setup(c => c.TryValidateModel(It.IsAny<object>())).Returns(true);
        }

        [Test]
        public void Continents_ReturnsListOfContinents()
        {
            // Arrange
            var continents = new List<Continent> { new Continent { Id = 1, Name = "Europe" } };
            _mockRepo.Setup(repo => repo.GetAllContinentsAsync()).ReturnsAsync(continents);
            var expectedContinentsDto = new List<ContinentReadDto> { new ContinentReadDto { Id = 1, Name = "Europe" } };
            _mockMapper
                .Setup(m => m.Map<IEnumerable<ContinentReadDto>>(continents))
                .Returns(expectedContinentsDto);

            // Act
            var result = _mockController.Object.GetContinents();
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<ContinentReadDto>>());
            var actualContinents = okResult.Value as IEnumerable<ContinentReadDto>;
            Assert.That(actualContinents, Is.EquivalentTo(expectedContinentsDto));
            _mockRepo.Verify(repo => repo.GetAllContinentsAsync(), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<ContinentReadDto>>(continents), Times.Once);
        }

        [Test]
        public async Task GetContinentById_ExistingId_ReturnsContinent()
        {
            // Arrange
            int continentId = 1;
            var continent = new Continent { Id = continentId, Name = "Europe" };
            _mockRepo.Setup(repo => repo.GetContinentByIdAsync(continentId)).ReturnsAsync(continent);
            var expectedContinentDto = new ContinentReadDto { Id = continentId, Name = "Europe" };
            _mockMapper
                .Setup(m => m.Map<ContinentReadDto>(continent))
                .Returns(expectedContinentDto);

            // Act
            var result = await _mockController.Object.GetContinentByIdAsync(continentId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<ContinentReadDto>());
            var actualContinent = okResult.Value as ContinentReadDto;
            Assert.That(actualContinent, Is.EqualTo(expectedContinentDto));
            _mockRepo.Verify(repo => repo.GetContinentByIdAsync(continentId), Times.Once);
            _mockMapper.Verify(m => m.Map<ContinentReadDto>(continent), Times.Once);
        }

        [Test]
        public async Task GetContinentById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int continentId = 999;
            _mockRepo.Setup(repo => repo.GetContinentByIdAsync(continentId)).ReturnsAsync(null as Continent);

            // Act
            var result = await _mockController.Object.GetContinentByIdAsync(continentId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
            _mockRepo.Verify(repo => repo.GetContinentByIdAsync(continentId), Times.Once);
        }

        [Test]
        public async Task GetCountryById_ExistingId_ReturnsCountry()
        {
            // Arrange
            int countryId = 1;
            var country = new Country { Id = countryId, Name = "France" };
            _mockRepo.Setup(repo => repo.GetCountryByIdAsync(countryId)).ReturnsAsync(country);
            var expectedCountryDto = new CountryReadDto { Id = countryId, Name = "France", Continent = 1 };
            _mockMapper
                .Setup(m => m.Map<CountryReadDto>(country))
                .Returns(expectedCountryDto);

            // Act
            var result = await _mockController.Object.GetCountryById(countryId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<CountryReadDto>());
            var actualCountry = okResult.Value as CountryReadDto;
            Assert.That(actualCountry, Is.EqualTo(expectedCountryDto));
            _mockRepo.Verify(repo => repo.GetCountryByIdAsync(countryId), Times.Once);
            _mockMapper.Verify(m => m.Map<CountryReadDto>(country), Times.Once);
        }

        [Test]
        public async Task GetCountryById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int countryId = 999; // ID that doesn't exist
            _mockRepo.Setup(repo => repo.GetCountryByIdAsync(countryId)).ReturnsAsync(null as Country);

            // Act
            var result = await _mockController.Object.GetCountryById(countryId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
            _mockRepo.Verify(repo => repo.GetCountryByIdAsync(countryId), Times.Once);
        }

        [Test]
        public async Task GetCoinById_ExistingId_ReturnsCoin()
        {
            // Arrange
            int coinId = 1;
            var coin = new Coin { Id = coinId };
            _mockRepo.Setup(repo => repo.GetCoinByIdAsync(coinId)).ReturnsAsync(coin);
            var expectedCoinDto = new CoinReadDto { Id = coinId };
            _mockMapper
                .Setup(m => m.Map<CoinReadDto>(coin))
                .Returns(expectedCoinDto);

            // Act
            var result = await _mockController.Object.GetCoinById(coinId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<CoinReadDto>());

            var actualCoin = okResult.Value as CoinReadDto;
            Assert.That(actualCoin, Is.EqualTo(expectedCoinDto));
            _mockRepo.Verify(repo => repo.GetCoinByIdAsync(coinId), Times.Once);
            _mockMapper.Verify(m => m.Map<CoinReadDto>(coin), Times.Once);
        }

        [Test]
        public async Task GetCoinById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int coinId = 999;
            _mockRepo.Setup(repo => repo.GetCoinByIdAsync(coinId)).ReturnsAsync(null as Coin);

            // Act
            var result = await _mockController.Object.GetCoinById(coinId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
            _mockRepo.Verify(repo => repo.GetCoinByIdAsync(coinId), Times.Once);
        }

        [Test]
        public async Task GetCountriesByContinent_ExistingId_ReturnsCountries()
        {
            // Arrange
            int continentId = 1;
            var countries = new List<Country>
            {
                new Country { Id = 1, Name = "France", Continent = continentId },
                new Country { Id = 2, Name = "Germany", Continent = continentId }
            };

            _mockRepo.Setup(repo => repo.GetCountriesByContinentIdAsync(continentId)).ReturnsAsync(countries);

            var expectedCountriesDto = new List<CountryReadDto>
            {
                new CountryReadDto { Id = 1, Name = "France" },
                new CountryReadDto { Id = 2, Name = "Germany" }
            };

            _mockMapper
                .Setup(m => m.Map<IEnumerable<CountryReadDto>>(countries))
                .Returns(expectedCountriesDto);

            // Act
            var result = await _mockController.Object.GetCountriesByContinent(continentId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CountryReadDto>>());
            var actualCountries = okResult.Value as IEnumerable<CountryReadDto>;
            Assert.That(actualCountries, Is.EqualTo(expectedCountriesDto));
            _mockRepo.Verify(repo => repo.GetCountriesByContinentIdAsync(continentId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CountryReadDto>>(countries), Times.Once);
        }

        [Test]
        public async Task GetCountriesByContinent_ExistingId_NoCountries_ReturnsEmptyList()
        {
            // Arrange
            int continentId = 1;
            var countries = new List<Country>();
            _mockRepo.Setup(repo => repo.GetCountriesByContinentIdAsync(continentId)).ReturnsAsync(countries);

            var expectedCountriesDto = new List<CountryReadDto>();
            _mockMapper
                .Setup(m => m.Map<IEnumerable<CountryReadDto>>(countries))
                .Returns(expectedCountriesDto);

            // Act
            var result = await _mockController.Object.GetCountriesByContinent(continentId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CountryReadDto>>());
            var actualCountries = okResult.Value as IEnumerable<CountryReadDto>;
            Assert.That(actualCountries, Is.Empty);
            _mockRepo.Verify(repo => repo.GetCountriesByContinentIdAsync(continentId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CountryReadDto>>(countries), Times.Once);
        }

        [Test]
        public async Task GetPeriodsByCountry_ExistingId_ReturnsPeriods()
        {
            // Arrange
            int countryId = 1;
            var periods = new List<Period>
            {
                new Period { Id = 1, Name = "Medieval", Country = countryId },
                new Period { Id = 2, Name = "Renaissance", Country = countryId }
            };

            _mockRepo.Setup(repo => repo.GetPeriodsByCountryIdAsync(countryId)).ReturnsAsync(periods);

            var expectedPeriodsDto = new List<PeriodReadDto>
            {
                new PeriodReadDto { Id = 1, Name = "Medieval", Country = 5 },
                new PeriodReadDto { Id = 2, Name = "Renaissance", Country = 5 }
            };

            _mockMapper
                .Setup(m => m.Map<IEnumerable<PeriodReadDto>>(periods))
                .Returns(expectedPeriodsDto);

            // Act
            var result = await _mockController.Object.GetPeriodsByCountry(countryId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<PeriodReadDto>>());
            var actualPeriods = okResult.Value as IEnumerable<PeriodReadDto>;
            Assert.That(actualPeriods, Is.EqualTo(expectedPeriodsDto));
            _mockRepo.Verify(repo => repo.GetPeriodsByCountryIdAsync(countryId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<PeriodReadDto>>(periods), Times.Once);
        }

        [Test]
        public async Task GetPeriodsByCountry_ExistingId_NoPeriods_ReturnsEmptyList()
        {
            // Arrange
            int countryId = 1;
            var periods = new List<Period>();
            _mockRepo.Setup(repo => repo.GetPeriodsByCountryIdAsync(countryId)).ReturnsAsync(periods);

            var expectedPeriodsDto = new List<PeriodReadDto>();
            _mockMapper
                .Setup(m => m.Map<IEnumerable<PeriodReadDto>>(periods))
                .Returns(expectedPeriodsDto);

            // Act
            var result = await _mockController.Object.GetPeriodsByCountry(countryId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<PeriodReadDto>>());
            var actualPeriods = okResult.Value as IEnumerable<PeriodReadDto>;
            Assert.That(actualPeriods, Is.Empty);
            _mockRepo.Verify(repo => repo.GetPeriodsByCountryIdAsync(countryId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<PeriodReadDto>>(periods), Times.Once);
        }

        [Test]
        public async Task GetCoinsByPeriod_ExistingId_ReturnsCoins()
        {
            // Arrange
            int periodId = 1;
            var coins = new List<Coin>
            {
                new Coin { Id = 1, Period = periodId },
                new Coin { Id = 2, Period = periodId }
            };

            _mockRepo.Setup(repo => repo.GetCoinsByPeriodIdAsync(periodId)).ReturnsAsync(coins);

            var expectedCoinsDto = new List<CoinReadDto>
            {
                new CoinReadDto { Id = 1 },
                new CoinReadDto { Id = 2 }
            };

            _mockMapper
                .Setup(m => m.Map<IEnumerable<CoinReadDto>>(coins))
                .Returns(expectedCoinsDto);

            // Act
            var result = await _mockController.Object.GetCoinsByPeriod(periodId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CoinReadDto>>());
            var actualCoins = okResult.Value as IEnumerable<CoinReadDto>;
            Assert.That(actualCoins, Is.EqualTo(expectedCoinsDto));
            _mockRepo.Verify(repo => repo.GetCoinsByPeriodIdAsync(periodId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CoinReadDto>>(coins), Times.Once);
        }

        [Test]
        public async Task GetCoinsByPeriod_ExistingId_NoCoins_ReturnsEmptyList()
        {
            // Arrange
            int periodId = 1;
            var coins = new List<Coin>();
            _mockRepo.Setup(repo => repo.GetCoinsByPeriodIdAsync(periodId)).ReturnsAsync(coins);

            var expectedCoinsDto = new List<CoinReadDto>();
            _mockMapper
                .Setup(m => m.Map<IEnumerable<CoinReadDto>>(coins))
                .Returns(expectedCoinsDto);

            // Act
            var result = await _mockController.Object.GetCoinsByPeriod(periodId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CoinReadDto>>());
            var actualCoins = okResult.Value as IEnumerable<CoinReadDto>;
            Assert.That(actualCoins, Is.Empty);
            _mockRepo.Verify(repo => repo.GetCoinsByPeriodIdAsync(periodId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CoinReadDto>>(coins), Times.Once);
        }

        [Test]
        [Ignore("Temporarily disabled - needs fixing")]
        public async Task CreatCoin_ReturnsCoin()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.Length).Returns(10);
            var coinCreateDto = new CoinCreateDto
            {
                ObverseImage = fileMock.Object,
                ReverseImage = fileMock.Object,
                Period = 1,
                Nominal = "1",
                Currency = "USD",
                Year = "1999",
                Type = 2,
                CommemorativeName = "Anniversary",
                CatalogId = "1234"
            };
            var coinReadDto = new CoinReadDto
            {
                Id = 1,
                Nominal = "1",
                Currency = "USD",
                Type = 2,
                CommemorativeName = "Anniversary",
                Period = 1,
                PictPreviewPath = "root"
            };
            var coinModel = new Coin { Id = 1, Nominal = "1" };

            var jsonResponse = """
{
    "mergedImageBase64": "ZGVjb2RlZHN0cmluZw=="
}
""";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockRepo.Setup(m => m.GetPeriodByIdAsync(1)).ReturnsAsync(new Period { Name = "1900-2000" });
            _mockRepo.Setup(m => m.GetCountryByPeriodIdAsync(1)).ReturnsAsync(new Country { Id = 1, Name = "France" });
            _mockRepo.Setup(m => m.GetContinentByCountryIdAsync(1)).ReturnsAsync(new Continent { Name = "Europe" });
            _mockMapper.Setup(m => m.Map<Coin>(coinCreateDto)).Returns(coinModel);
            _mockMapper.Setup(m => m.Map<CoinReadDto>(coinModel)).Returns(coinReadDto);
            _mockAzureFunctionService.Setup(m => m.CallFunctionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(httpResponse);


            // Act
            var okResult = await _mockController.Object.CreateCoin(coinCreateDto) as ActionResult<CoinReadDto>;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            _mockRepo.Verify(repo => repo.CreateCoin(coinModel), Times.Once);
            _mockMapper.Verify(m => m.Map<Coin>(coinCreateDto), Times.Once);
            _mockMapper.Verify(m => m.Map<CoinReadDto>(coinModel), Times.Once);
            var createdAtRouteResult = okResult.Result as CreatedAtRouteResult;
            Assert.That(createdAtRouteResult, Is.Not.Null);
            var actualCoin = createdAtRouteResult.Value as CoinReadDto;
            Assert.That(actualCoin, Is.Not.Null);
            Assert.That(actualCoin, Is.EqualTo(coinReadDto));
        }

        [Test]
        public async Task CreatCoin_NullCoin_ReturnsBadRequest()
        {
            // Arrange

            // Act
            var result = await _mockController.Object.CreateCoin(null);

            // Assert
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult!.Value, Is.EqualTo("Coin data cannot be null."));
        }

        [Test]
        public async Task CreateCoin_NUllObverseImage_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.Length).Returns(10);
            var coinCreateDto = new CoinCreateDto { ReverseImage = fileMock.Object };

            // Act
            var result = await _mockController.Object.CreateCoin(coinCreateDto);

            // Assert
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult!.Value, Is.EqualTo("No coin obverse image uploaded."));
        }

        [Test]
        public async Task CreateCoin_NUllReverseImage_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.Length).Returns(10);
            var coinCreateDto = new CoinCreateDto { ObverseImage = fileMock.Object };

            // Act
            var result = await _mockController.Object.CreateCoin(coinCreateDto);

            // Assert
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult!.Value, Is.EqualTo("No coin reverse image uploaded."));
        }

        [Test]
        public async Task CreateCoin_NUllPeriod_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.Length).Returns(10);
            var coinCreateDto = new CoinCreateDto { ObverseImage = fileMock.Object, ReverseImage = fileMock.Object };

            // Act
            var result = await _mockController.Object.CreateCoin(coinCreateDto);

            // Assert
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult!.Value, Is.EqualTo("Period can't be null."));
        }

        [Test]
        [Ignore("Temporarily disabled - needs fixing")]
        public async Task CreateCoin_FailedImageProcessing_ReturnsExpectedErrorMessage()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.Length).Returns(10);
            var coinCreateDto = new CoinCreateDto { ObverseImage = fileMock.Object, ReverseImage = fileMock.Object, Period = 1 };
            var coinModel = new Coin { Id = 1, Nominal = "1" };
            var jsonResponse = """
{
    "mergedImageBase64": ""
}
""";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockRepo.Setup(m => m.GetPeriodByIdAsync(1)).ReturnsAsync(new Period { Name = "1900-2000" });
            _mockRepo.Setup(m => m.GetCountryByPeriodIdAsync(1)).ReturnsAsync(new Country { Id = 1, Name = "France" });
            _mockRepo.Setup(m => m.GetContinentByCountryIdAsync(1)).ReturnsAsync(new Continent { Name = "Europe" });
            _mockMapper.Setup(m => m.Map<Coin>(coinCreateDto)).Returns(coinModel);
            _mockAzureFunctionService.Setup(m => m.CallFunctionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(httpResponse);

            // Act
            var result = await _mockController.Object.CreateCoin(coinCreateDto);

            // Assert
            Assert.That(result.Result, Is.TypeOf<ObjectResult>());
            var badRequestResult = result.Result as ObjectResult;
            Assert.That(badRequestResult!.Value, Is.EqualTo("Failed to process images."));
        }

        [Test]
        public async Task UpdateCoin_ExisitngId_CoinUpdated()
        {
            // Arrange
            var coin = new Coin { Id = 7 };
            var coinUpdateDto = new CoinUpdateDto 
            { 
                Nominal = "1",
                Currency = "USD",
                Year = "1999",
                Type = 2,
                CommemorativeName = "Name",
                Period = 5,
                PictPreviewPath = "root"
            };
            _mockRepo.Setup(m => m.GetCoinByIdAsync(7)).ReturnsAsync(coin);

            // Act
            var result = await _mockController.Object.UpdateCoin(7, coinUpdateDto);

            // Assert
            _mockMapper.Verify(m => m.Map(coinUpdateDto, coin), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task UpdateCoin_CoinNotFound_ReturnsNotFound()
        {
            // Arrange
            int coinId = 1;
            _mockRepo.Setup(r => r.GetCoinByIdAsync(coinId)).ReturnsAsync(null as Coin);

            // Act
            var result = await _mockController.Object.UpdateCoin(coinId, new CoinUpdateDto());

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task UpdateCoin_NullCoinUpdateDto_ReturnsBadRequest()
        {
            // Arrange

            // Act
            var result = await _mockController.Object.UpdateCoin(1, null);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }


        [Test]
        public async Task PartiallyUpdateCoin_CoinNotFound_ReturnsNotFound()
        {
            // Arrange
            int coinId = 1;
            _mockRepo.Setup(r => r.GetCoinByIdAsync(coinId)).ReturnsAsync(null as Coin);

            var patchDoc = new JsonPatchDocument<CoinUpdateDto>();

            // Act
            var result = await _mockController.Object.PartiallyUpdateCoin(coinId, patchDoc);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task PartiallyUpdateCoin_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            int coinId = 1;
            var existingCoin = new Coin();
            var coinUpdateDto = new CoinUpdateDto();
            var patchDoc = new JsonPatchDocument<CoinUpdateDto>();
            patchDoc.Replace(c => c.Year, "New Value");

            _mockRepo.Setup(r => r.GetCoinByIdAsync(coinId)).ReturnsAsync(existingCoin);
            _mockMapper.Setup(m => m.Map<CoinUpdateDto>(existingCoin)).Returns(coinUpdateDto);

            _mockController.Object.ModelState.AddModelError("SomeProperty", "Invalid value");

            // Act
            var result = await _mockController.Object.PartiallyUpdateCoin(coinId, patchDoc);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PartiallyUpdateCoin_ValidPatch_UpdatesCoinAndReturnsNoContent()
        {
            // Arrange
            int coinId = 1;
            var existingCoin = new Coin();
            var coinUpdateDto = new CoinUpdateDto
            {
                Nominal = "1",
                Currency = "USD",
                Year = "1999",
                Type = 2,
                CommemorativeName = "Name",
                Period = 5,
                PictPreviewPath = "root"
            };
            var patchDoc = new JsonPatchDocument<CoinUpdateDto>();
            patchDoc.Replace(c => c.Year, "Updated Value");

            _mockRepo.Setup(r => r.GetCoinByIdAsync(coinId)).ReturnsAsync(existingCoin);
            _mockMapper.Setup(m => m.Map<CoinUpdateDto>(existingCoin)).Returns(coinUpdateDto);
            _mockMapper.Setup(m => m.Map(coinUpdateDto, existingCoin)); 

            // Act
            var result = await _mockController.Object.PartiallyUpdateCoin(coinId, patchDoc);

            // Assert
            _mockMapper.Verify(m => m.Map(coinUpdateDto, existingCoin), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task DeleteCoin_CoinNotFound_ReturnsNotFound()
        {
            // Arrange
            int coinId = 1;
            _mockRepo.Setup(r => r.GetCoinByIdAsync(coinId)).ReturnsAsync(null as Coin);

            // Act
            var result = await _mockController.Object.DeleteCoin(coinId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));            
        }

        [Test]
        public async Task DeleteCoin_ValidCoin_ReturnsOk()
        {
            // Arrange
            int coinId = 1;
            var existingCoin = new Coin();
            _mockRepo.Setup(r => r.GetCoinByIdAsync(coinId)).ReturnsAsync(existingCoin);
            _mockRepo.Setup(r => r.RemoveCoin(existingCoin)).Returns(Task.CompletedTask);

            // Act
            var result = await _mockController.Object.DeleteCoin(coinId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkResult>());
            _mockRepo.Verify(r => r.RemoveCoin(existingCoin), Times.Once);
        }

        [Test]
        public async Task DeleteCoin_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            int coinId = 1;
            _mockRepo.Setup(r => r.GetCoinByIdAsync(coinId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _mockController.Object.DeleteCoin(coinId);

            // Assert
            var objectResult = result.Result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }
    }
}
