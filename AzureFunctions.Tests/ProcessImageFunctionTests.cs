using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using ImageMagick;

namespace AzureFunctions.Tests
{
    [TestFixture, Parallelizable(ParallelScope.None)]
    public class ProcessImagesFunctionTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<HttpRequest> _mockRequest;
        private string _base64String;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockRequest = new Mock<HttpRequest>();

            _base64String = GenerateBase64TestImage();
        }

        [Test]
        public async Task Run_ValidImages_ReturnsOk()
        {
            // Arrange
            var functionContext = new Mock<FunctionContext>();

            var services = new ServiceCollection();
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();
            functionContext.Setup(ctx => ctx.InstanceServices).Returns(serviceProvider);

            var validRequest = new ProcessImagesFunction.ImageRequest
            {
                ObverseImageBase64 = _base64String,
                ReverseImageBase64 = _base64String
            };
            var requestBody = JsonConvert.SerializeObject(validRequest);
            _mockRequest.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestBody)));

            // Act
            var result = await ProcessImagesFunction.Run(_mockRequest.Object, functionContext.Object);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.IsNotNull(okResult.Value);
            Assert.IsNotNull(okResult.Value.ToString());
            Assert.IsTrue(okResult.Value.ToString().Contains("MergedImageBase64"));
        }

        private string GenerateBase64TestImage()
        {
            using var image = new MagickImage(MagickColors.Red, 700, 420);
            image.Format = MagickFormat.Png;

            var imageBytes = image.ToByteArray();
            return Convert.ToBase64String(imageBytes);
        }
    }
}