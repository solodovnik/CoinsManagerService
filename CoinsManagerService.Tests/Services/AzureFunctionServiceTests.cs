using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using CoinsManagerService.Services;
using System.Threading;

namespace CoinsManagerService.Tests
{
    [TestFixture]
    public class AzureFunctionServiceTests
    {
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private AzureFunctionService _azureFunctionService;

        [SetUp]
        public void SetUp()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _azureFunctionService = new AzureFunctionService(_httpClient);
        }

        [Test]
        public async Task CallFunctionAsync_ShouldReturnHttpResponse_WhenCalled()
        {
            // Arrange
            var functionUrl = "https://example.com/function";
            var functionKey = "test-key";
            var requestPayload = new { Name = "Test" };
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _azureFunctionService.CallFunctionAsync(functionUrl, functionKey, requestPayload);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == functionUrl),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
