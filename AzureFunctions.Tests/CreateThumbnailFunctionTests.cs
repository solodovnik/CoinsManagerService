using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace AzureFunctions.Tests
{
    [TestFixture]
    public class CreateThumbnailFunctionTests
    {
        private Mock<ILogger<CreateThumbnailFunction>> _loggerMock;
        private Mock<BlobContainerClient> _blobContainerClientMock;
        private Mock<BlobClient> _blobClientMock;
        private CreateThumbnailFunction _function;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<CreateThumbnailFunction>>();
            _blobContainerClientMock = new Mock<BlobContainerClient>();
            _blobClientMock = new Mock<BlobClient>();
      
            _blobContainerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _function = new CreateThumbnailFunction(_loggerMock.Object, _blobContainerClientMock.Object);
        }

        [Test]
        public async Task Run_ValidImage_CreatesThumbnail()
        {
            // Arrange
            var image = new Image<Rgba32>(800, 600);
            using var imageStream = new MemoryStream();
            image.SaveAsJpeg(imageStream);
            imageStream.Position = 0;

            var name = "test-image.jpg";

            _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, default))
                .ReturnsAsync(Mock.Of<Azure.Response<Azure.Storage.Blobs.Models.BlobContentInfo>>());

            // Act
            await _function.Run(imageStream, name);

            // Assert
            _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<MemoryStream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
