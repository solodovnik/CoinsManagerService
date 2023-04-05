using CoinsManagerService.Data;
using CoinsManagerService.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoinsManagerService.Tests.Data
{
    public class Tests
    {
        private CoinsRepo _repo;
        private Mock<AppDbContext> _context;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _context = new Mock<AppDbContext>();

            var continentData = new List<Continent>
            {
                new Continent
                {
                    Id = 1
                }
            }.AsQueryable();

            var coinData = new List<Coin>
            {
                new Coin
                {
                    Id = 5,
                    Period = 6
                }
            }.AsQueryable();

            var countryData = new List<Country>
            {
                new Country
                {
                    Id = 7
                }
            }.AsQueryable();

            var periodData = new List<Period>
            {
                new Period
                {
                    Country = 8
                }
            }.AsQueryable();

            var dbSetContinentsMock = new Mock<DbSet<Continent>>();
            dbSetContinentsMock.As<IQueryable<Continent>>().Setup(m => m.Provider).Returns(continentData.Provider);
            dbSetContinentsMock.As<IQueryable<Continent>>().Setup(m => m.Expression).Returns(continentData.Expression);
            dbSetContinentsMock.As<IQueryable<Continent>>().Setup(m => m.GetEnumerator()).Returns(() => continentData.GetEnumerator());
            _context.Setup(x => x.Continents).Returns(dbSetContinentsMock.Object);

            var dbSetCoinsMock = new Mock<DbSet<Coin>>();
            dbSetCoinsMock.As<IQueryable<Coin>>().Setup(m => m.Provider).Returns(coinData.Provider);
            dbSetCoinsMock.As<IQueryable<Coin>>().Setup(m => m.Expression).Returns(coinData.Expression);
            dbSetCoinsMock.As<IQueryable<Coin>>().Setup(m => m.GetEnumerator()).Returns(() => coinData.GetEnumerator());
            _context.Setup(x => x.Coins).Returns(dbSetCoinsMock.Object);

            var dbSetCountriesMock = new Mock<DbSet<Country>>();
            dbSetCountriesMock.As<IQueryable<Country>>().Setup(m => m.Provider).Returns(countryData.Provider);
            dbSetCountriesMock.As<IQueryable<Country>>().Setup(m => m.Expression).Returns(countryData.Expression);
            dbSetCountriesMock.As<IQueryable<Country>>().Setup(m => m.GetEnumerator()).Returns(() => countryData.GetEnumerator());
            _context.Setup(x => x.Countries).Returns(dbSetCountriesMock.Object);

            var dbSetPeriodsMock = new Mock<DbSet<Period>>();
            dbSetPeriodsMock.As<IQueryable<Period>>().Setup(m => m.Provider).Returns(periodData.Provider);
            dbSetPeriodsMock.As<IQueryable<Period>>().Setup(m => m.Expression).Returns(periodData.Expression);
            dbSetPeriodsMock.As<IQueryable<Period>>().Setup(m => m.GetEnumerator()).Returns(() => periodData.GetEnumerator());
            _context.Setup(x => x.Periods).Returns(dbSetPeriodsMock.Object);

            _repo = new CoinsRepo(_context.Object);
        }

        [Test]
        public void GetAllContinentsShouldReturnExpectedData()
        {
            // Assign             

            // Act            
            var output = _repo.GetAllContinents();

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetCoinByIdShouldReturnExpectedData()
        {
            // Assign

            // Act            
            var output = _repo.GetCoinById(5);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(5));
        }

        [Test]
        public void GetCoinsByPeriodShouldReturnExpectedData()
        {
            // Assign

            // Act            
            var output = _repo.GetCoinsByPeriodId(6);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetContinentByIdShouldReturnExpectedData()
        {
            // Assign

            // Act            
            var output = _repo.GetContinentById(1);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(1));
        }

        [Test]
        public void GetCountryByIdShouldReturnExpectedData()
        {
            // Assign

            // Act            
            var output = _repo.GetCountryById(7);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Id, Is.EqualTo(7));
        }

        [Test]
        public void GetPeriodsByCountryIdShouldReturnExpectedData()
        {
            // Assign

            // Act            
            var output = _repo.GetPeriodsByCountryId(8);

            // Assert
            Assert.That(output, Is.Not.Null);
            Assert.That(output.Count, Is.EqualTo(1));
        }

        [Test]
        public void CreateCoinShouldAddCoinToContext()
        {
            // Assign
            Coin coin = new Coin
            {
                Id = 10
            }; 

            // Act            
            _repo.CreateCoin(coin);

            // Assert
            _context.Verify(x => x.Coins.Add(It.IsAny<Coin>()), Times.Once);
        }

        [Test]
        public void CreateCoinShouldThrowExceptionWhenCoinIsNull()
        {
            // Assign            

            // Act  

            // Assert            
            Assert.Throws<ArgumentNullException>(() => _repo.CreateCoin(null));
        }
    }
}