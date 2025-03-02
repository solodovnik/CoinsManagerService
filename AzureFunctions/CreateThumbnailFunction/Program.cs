using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton(provider =>
{
    var storageConnectionString = Environment.GetEnvironmentVariable("BlobStorageTrigger")!;
    return new BlobServiceClient(storageConnectionString);
});

builder.Services.AddSingleton(provider =>
{
    var blobServiceClient = provider.GetRequiredService<BlobServiceClient>();
    var thumbnailsContainerName = Environment.GetEnvironmentVariable("ThumbnailsContainerName")!;
    return blobServiceClient.GetBlobContainerClient(thumbnailsContainerName);
});

builder.Build().Run();
