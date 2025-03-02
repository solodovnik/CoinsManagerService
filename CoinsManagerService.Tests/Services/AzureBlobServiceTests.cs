using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using NUnit.Framework;
using CoinsManagerService.Services;

namespace CoinsManagerService.Tests.Services
{
    [TestFixture]
    public class AzureBlobServiceTests
    {
        private Mock<BlobServiceClient> _mockBlobServiceClient;
        private Mock<BlobContainerClient> _mockBlobContainerClient;
        private Mock<BlobClient> _mockBlobClient;
        private AzureBlobService _azureBlobService;

        [SetUp]
        public void Setup()
        {
            _mockBlobServiceClient = new Mock<BlobServiceClient>();
            _mockBlobContainerClient = new Mock<BlobContainerClient>();
            _mockBlobClient = new Mock<BlobClient>();

            _mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_mockBlobContainerClient.Object);

            _mockBlobContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_mockBlobClient.Object);

            _azureBlobService = new AzureBlobService(_mockBlobServiceClient.Object);
        }

        [Test]
        public async Task UploadFileAsync_ShouldUploadFileAndReturnUri()
        {
            // Arrange
            string containerName = "test-container";
            string fileName = "test.jpg";
            string expectedUri = $"https://fakeaccount.blob.core.windows.net/{containerName}/{fileName}";

            using var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });

           
            var mockResponse = new Mock<Response<BlobContainerInfo>>();
            _mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(),
                                                    It.IsAny<IDictionary<string, string>>(),
                                                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                                                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

          
            var mockUploadResponse = new Mock<Response<BlobContentInfo>>();
            _mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(),
                                          It.IsAny<BlobUploadOptions>(),
                                          It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUploadResponse.Object);

     
            _mockBlobClient
                .Setup(x => x.Uri)
                .Returns(new Uri(expectedUri));

            // Act
            var result = await _azureBlobService.UploadFileAsync(fileStream, fileName, containerName);

            // Assert
            Assert.That(result, Is.EqualTo(expectedUri));
            _mockBlobContainerClient.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(),
                                                                          It.IsAny<IDictionary<string, string>>(),
                                                                          It.IsAny<BlobContainerEncryptionScopeOptions>(),
                                                                          It.IsAny<CancellationToken>()), Times.Once);
            _mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(),
                                                      It.IsAny<BlobUploadOptions>(),
                                                      It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
