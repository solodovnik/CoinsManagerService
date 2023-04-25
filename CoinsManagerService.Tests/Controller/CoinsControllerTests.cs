using AutoMapper;
using CoinsManagerService.Controllers;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace CoinsManagerService.Tests.Controller
{
    public class CoinsControllerTests
    {
        [Test]
        public void GetCoinsByPeriodShouldReturnActionResultOfExpectedType()
        {
            // Assign
            var mapperMock = new Mock<IMapper>();
            var coinsRepoMock = new Mock<ICoinsRepo>();
            var controller = new CoinsController(mapperMock.Object, coinsRepoMock.Object);

            // Act
            var result = controller.GetCoinsByPeriod(1);

            // Assert
            Assert.IsInstanceOf<ActionResult<IEnumerable<CoinReadDto>>>(result);
        }
    }
}
