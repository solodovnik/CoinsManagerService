using AutoMapper;
using CoinsManagerService.Controllers;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace CoinsManagerService.Tests.Controller
{
    public class CoinsControllerTests
    {
        private Mock<ICoinsRepo> _mockRepo;
        private Mock<ILogger<CoinsController>> _mockLogger;
        private Mock<IConfiguration> _mockConfig;
        private Mock<IMapper> _mockMapper;
        private CoinsController _controller;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<ICoinsRepo>();
            _mockLogger = new Mock<ILogger<CoinsController>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockMapper = new Mock<IMapper>();
            _controller = new CoinsController(_mockMapper.Object, _mockRepo.Object, null, _mockConfig.Object, _mockLogger.Object);
        }

        [Test]
        public void Continents_ReturnsListOfContinents()
        {
            // Arrange
            var continents = new List<Continent> { new Continent { Id = 1, Name = "Europe" } };
            _mockRepo.Setup(repo => repo.GetAllContinents()).Returns(continents);
            var expectedContinentsDto = new List<ContinentReadDto> { new ContinentReadDto { Id = 1, Name = "Europe" } };
            _mockMapper
                .Setup(m => m.Map<IEnumerable<ContinentReadDto>>(continents))
                .Returns(expectedContinentsDto);

            // Act
            var result = _controller.GetContinents();
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<ContinentReadDto>>());
            var actualContinents = okResult.Value as IEnumerable<ContinentReadDto>;
            Assert.That(actualContinents, Is.EquivalentTo(expectedContinentsDto));
            _mockRepo.Verify(repo => repo.GetAllContinents(), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<ContinentReadDto>>(continents), Times.Once);
        }

        [Test]
        public void GetContinentById_ExistingId_ReturnsContinent()
        {
            // Arrange
            int continentId = 1;
            var continent = new Continent { Id = continentId, Name = "Europe" };
            _mockRepo.Setup(repo => repo.GetContinentById(continentId)).Returns(continent);
            var expectedContinentDto = new ContinentReadDto { Id = continentId, Name = "Europe" };
            _mockMapper
                .Setup(m => m.Map<ContinentReadDto>(continent))
                .Returns(expectedContinentDto);

            // Act
            var result = _controller.GetContinentById(continentId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<ContinentReadDto>());
            var actualContinent = okResult.Value as ContinentReadDto;
            Assert.That(actualContinent, Is.EqualTo(expectedContinentDto));
            _mockRepo.Verify(repo => repo.GetContinentById(continentId), Times.Once);
            _mockMapper.Verify(m => m.Map<ContinentReadDto>(continent), Times.Once);
        }

        [Test]
        public void GetContinentById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int continentId = 999;
            _mockRepo.Setup(repo => repo.GetContinentById(continentId)).Returns((Continent)null);

            // Act
            var result = _controller.GetContinentById(continentId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
            _mockRepo.Verify(repo => repo.GetContinentById(continentId), Times.Once);
        }

        [Test]
        public void GetCountryById_ExistingId_ReturnsCountry()
        {
            // Arrange
            int countryId = 1;
            var country = new Country { Id = countryId, Name = "France" };
            _mockRepo.Setup(repo => repo.GetCountryById(countryId)).Returns(country);
            var expectedCountryDto = new CountryReadDto { Id = countryId, Name = "France" };
            _mockMapper
                .Setup(m => m.Map<CountryReadDto>(country))
                .Returns(expectedCountryDto);

            // Act
            var result = _controller.GetCountryById(countryId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<CountryReadDto>());
            var actualCountry = okResult.Value as CountryReadDto;
            Assert.That(actualCountry, Is.EqualTo(expectedCountryDto));
            _mockRepo.Verify(repo => repo.GetCountryById(countryId), Times.Once);
            _mockMapper.Verify(m => m.Map<CountryReadDto>(country), Times.Once);
        }

        [Test]
        public void GetCountryById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int countryId = 999; // ID that doesn't exist
            _mockRepo.Setup(repo => repo.GetCountryById(countryId)).Returns((Country)null);

            // Act
            var result = _controller.GetCountryById(countryId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
            _mockRepo.Verify(repo => repo.GetCountryById(countryId), Times.Once);
        }

        [Test]
        public void GetCoinById_ExistingId_ReturnsCoin()
        {
            // Arrange
            int coinId = 1;
            var coin = new Coin { Id = coinId };
            _mockRepo.Setup(repo => repo.GetCoinById(coinId)).Returns(coin);
            var expectedCoinDto = new CoinReadDto { Id = coinId };
            _mockMapper
                .Setup(m => m.Map<CoinReadDto>(coin))
                .Returns(expectedCoinDto);

            // Act
            var result = _controller.GetCoinById(coinId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<CoinReadDto>());

            var actualCoin = okResult.Value as CoinReadDto;
            Assert.That(actualCoin, Is.EqualTo(expectedCoinDto));
            _mockRepo.Verify(repo => repo.GetCoinById(coinId), Times.Once);
            _mockMapper.Verify(m => m.Map<CoinReadDto>(coin), Times.Once);
        }

        [Test]
        public void GetCoinById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int coinId = 999; // ID that doesn't exist
            _mockRepo.Setup(repo => repo.GetCoinById(coinId)).Returns((Coin)null);

            // Act
            var result = _controller.GetCoinById(coinId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
            _mockRepo.Verify(repo => repo.GetCoinById(coinId), Times.Once);
        }

        [Test]
        public void GetCountriesByContinent_ExistingId_ReturnsCountries()
        {
            // Arrange
            int continentId = 1;
            var countries = new List<Country>
            {
                new Country { Id = 1, Name = "France", Continent = continentId },
                new Country { Id = 2, Name = "Germany", Continent = continentId }
            };

            _mockRepo.Setup(repo => repo.GetCountriesByContinentId(continentId)).Returns(countries);

            var expectedCountriesDto = new List<CountryReadDto>
            {
                new CountryReadDto { Id = 1, Name = "France" },
                new CountryReadDto { Id = 2, Name = "Germany" }
            };

            _mockMapper
                .Setup(m => m.Map<IEnumerable<CountryReadDto>>(countries))
                .Returns(expectedCountriesDto);

            // Act
            var result = _controller.GetCountriesByContinent(continentId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CountryReadDto>>());
            var actualCountries = okResult.Value as IEnumerable<CountryReadDto>;
            Assert.That(actualCountries, Is.EqualTo(expectedCountriesDto));
            // Verify mocks were used correctly
            _mockRepo.Verify(repo => repo.GetCountriesByContinentId(continentId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CountryReadDto>>(countries), Times.Once);
        }

        [Test]
        public void GetCountriesByContinent_ExistingId_NoCountries_ReturnsEmptyList()
        {
            // Arrange
            int continentId = 1;
            var countries = new List<Country>();
            _mockRepo.Setup(repo => repo.GetCountriesByContinentId(continentId)).Returns(countries);

            var expectedCountriesDto = new List<CountryReadDto>();
            _mockMapper
                .Setup(m => m.Map<IEnumerable<CountryReadDto>>(countries))
                .Returns(expectedCountriesDto);

            // Act
            var result = _controller.GetCountriesByContinent(continentId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CountryReadDto>>());
            var actualCountries = okResult.Value as IEnumerable<CountryReadDto>;
            Assert.That(actualCountries, Is.Empty);
            _mockRepo.Verify(repo => repo.GetCountriesByContinentId(continentId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CountryReadDto>>(countries), Times.Once);
        }

        [Test]
        public void GetPeriodsByCountry_ExistingId_ReturnsPeriods()
        {
            // Arrange
            int countryId = 1;
            var periods = new List<Period>
            {
                new Period { Id = 1, Name = "Medieval", Country = countryId },
                new Period { Id = 2, Name = "Renaissance", Country = countryId }
            };

            _mockRepo.Setup(repo => repo.GetPeriodsByCountryId(countryId)).Returns(periods);

            var expectedPeriodsDto = new List<PeriodReadDto>
            {
                new PeriodReadDto { Id = 1, Name = "Medieval" },
                new PeriodReadDto { Id = 2, Name = "Renaissance" }
            };

            _mockMapper
                .Setup(m => m.Map<IEnumerable<PeriodReadDto>>(periods))
                .Returns(expectedPeriodsDto);

            // Act
            var result = _controller.GetPeriodsByCountry(countryId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<PeriodReadDto>>());

            var actualPeriods = okResult.Value as IEnumerable<PeriodReadDto>;
            Assert.That(actualPeriods, Is.EqualTo(expectedPeriodsDto));
            _mockRepo.Verify(repo => repo.GetPeriodsByCountryId(countryId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<PeriodReadDto>>(periods), Times.Once);
        }

        [Test]
        public void GetPeriodsByCountry_ExistingId_NoPeriods_ReturnsEmptyList()
        {
            // Arrange
            int countryId = 1;
            var periods = new List<Period>();
            _mockRepo.Setup(repo => repo.GetPeriodsByCountryId(countryId)).Returns(periods);

            var expectedPeriodsDto = new List<PeriodReadDto>();
            _mockMapper
                .Setup(m => m.Map<IEnumerable<PeriodReadDto>>(periods))
                .Returns(expectedPeriodsDto);

            // Act
            var result = _controller.GetPeriodsByCountry(countryId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<PeriodReadDto>>());
            var actualPeriods = okResult.Value as IEnumerable<PeriodReadDto>;
            Assert.That(actualPeriods, Is.Empty);
            _mockRepo.Verify(repo => repo.GetPeriodsByCountryId(countryId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<PeriodReadDto>>(periods), Times.Once);
        }

        [Test]
        public void GetCoinsByPeriod_ExistingId_ReturnsCoins()
        {
            // Arrange
            int periodId = 1;
            var coins = new List<Coin>
            {
                new Coin { Id = 1, Period = periodId },
                new Coin { Id = 2, Period = periodId }
            };

            _mockRepo.Setup(repo => repo.GetCoinsByPeriodId(periodId)).Returns(coins);

            var expectedCoinsDto = new List<CoinReadDto>
            {
                new CoinReadDto { Id = 1 },
                new CoinReadDto { Id = 2 }
            };

            _mockMapper
                .Setup(m => m.Map<IEnumerable<CoinReadDto>>(coins))
                .Returns(expectedCoinsDto);

            // Act
            var result = _controller.GetCoinsByPeriod(periodId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CoinReadDto>>());
            var actualCoins = okResult.Value as IEnumerable<CoinReadDto>;
            Assert.That(actualCoins, Is.EqualTo(expectedCoinsDto));
            _mockRepo.Verify(repo => repo.GetCoinsByPeriodId(periodId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CoinReadDto>>(coins), Times.Once);
        }

        [Test]
        public void GetCoinsByPeriod_ExistingId_NoCoins_ReturnsEmptyList()
        {
            // Arrange
            int periodId = 1;
            var coins = new List<Coin>();
            _mockRepo.Setup(repo => repo.GetCoinsByPeriodId(periodId)).Returns(coins);

            var expectedCoinsDto = new List<CoinReadDto>();
            _mockMapper
                .Setup(m => m.Map<IEnumerable<CoinReadDto>>(coins))
                .Returns(expectedCoinsDto);

            // Act
            var result = _controller.GetCoinsByPeriod(periodId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CoinReadDto>>());
            var actualCoins = okResult.Value as IEnumerable<CoinReadDto>;
            Assert.That(actualCoins, Is.Empty);
            _mockRepo.Verify(repo => repo.GetCoinsByPeriodId(periodId), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<CoinReadDto>>(coins), Times.Once);
        }
    }
}
