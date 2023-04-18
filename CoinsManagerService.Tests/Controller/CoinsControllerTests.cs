using AutoMapper;
using CoinsManagerService.Controllers;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
