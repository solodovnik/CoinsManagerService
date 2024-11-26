using AutoMapper;
using CoinsManagerService.Controllers;
using CoinsManagerService.Data;
using CoinsManagerService.Dtos;
using CoinsManagerWebUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

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
            var azureBlobServiceMock = new Mock<IAzureBlobService>();
            var configurationMock = new Mock<IConfiguration>();
            var controller = new CoinsController(mapperMock.Object, coinsRepoMock.Object, azureBlobServiceMock.Object, configurationMock.Object);

            // Act
            var result = controller.GetCoinsByPeriod(1);

            // Assert
            Assert.That(result, Is.InstanceOf<ActionResult<IEnumerable<CoinReadDto>>>());
        }
    }
}
