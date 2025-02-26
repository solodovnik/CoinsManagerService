using CoinsManagerService.Data;
using CoinsManagerService.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoinsManagerService.Tests.Data
{
    public class Tests
    {
        private CoinsRepo _repo;
        private AppDbContext _dbContext;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _dbContext = new AppDbContext(options);
            _dbContext.Continents.Add(new Continent { Id = 1, Name = "Africa" });
            _dbContext.Coins.Add(new Coin { Id = 5, Period = 6, CatalogId = "1", Currency = "USD", Year = "1999", Nominal = "1" });
            _dbContext.Countries.Add(new Country { Id = 7, Continent = 1, Name = "France" });
            _dbContext.Countries.Add(new Country { Id = 8, Continent = 1, Name = "Germany" });
            _dbContext.Periods.Add(new Period { Id = 1, Country = 8, Name = "2000-2020 Republic" });
            _dbContext.SaveChanges();

            _repo = new CoinsRepo(_dbContext);
        }

        [Test]
        public async Task GetAllContinents_ReturnsExpectedData()
        {
            // Assign             

            // Act            
            var output = await _repo.GetAllContinentsAsync();

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetCoinById_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetCoinByIdAsync(5);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(5));
        }

        [Test]
        public async Task GetCoinsByPeriod_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetCoinsByPeriodIdAsync(6);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetContinentByCountryId_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetContinentByCountryIdAsync(7);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(1));
        }

        [Test]
        public void GetContinentByCountryId_ThrowsException_WhenCountryNotFound()
        {
            // Assign

            // Act     

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _repo.GetContinentByCountryIdAsync(20));
        }

        [Test]
        public async Task GetContinentById_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetContinentByIdAsync(1);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task GetCountriesByContinentId_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetCountriesByContinentIdAsync(1);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetCountryById_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetCountryByIdAsync(7);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(7));
        }

        [Test]
        public async Task GetCountryByPeriodId_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetCountryByPeriodIdAsync(1);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(8));
        }

        [Test]
        public void GetCountryByPeriodId_ThrowsException_WhenPeriodNotFound()
        {
            // Assign

            // Act        

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _repo.GetCountryByPeriodIdAsync(20));
        }

        [Test]
        public async Task GetPeriodById_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetPeriodByIdAsync(1);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task GetPeriodsByCountryId_ReturnsExpectedData()
        {
            // Assign

            // Act            
            var output = await _repo.GetPeriodsByCountryIdAsync(8);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateCoin_AddsCoinToContext()
        {
            // Assign
            var coin = new Coin { Id = 10, CatalogId = "10", Nominal = "10", Year = "2010", Currency = "USD" };

            // Act            
            await _repo.CreateCoin(coin);

            // Assert: Check if coin exists in the in-memory database
            var savedCoin = _dbContext.Coins.Find(10);
            Assert.That(savedCoin, Is.Not.Null);
        }


        [Test]
        public async Task CreateCoin_ThrowsException_WhenCoinIsNull()
        {
            // Assign            

            // Act  

            // Assert            
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _repo.CreateCoin(null));
        }

        [Test]
        public async Task RemoveCoin_RemovesCoinFromContext()
        {
            // Assign
            var coin = new Coin { Id = 20, CatalogId = "20", Nominal = "20", Year = "2020", Currency = "USD" };

            // Act
            await _repo.CreateCoin(coin);
            await _repo.RemoveCoin(coin);

            // Assert
            var removedCoin = _dbContext.Coins.Find(20);
            Assert.That(removedCoin, Is.Null);
        }

        [Test]
        public async Task RemoveCoin_ThrowsException_WhenCoinIsNull()
        {
            // Assign            

            // Act  

            // Assert            
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _repo.RemoveCoin(null));
        }
    }
}