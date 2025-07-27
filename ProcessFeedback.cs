using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using FeedbackProcessor.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

public class ProcessFeedback
{
    private readonly ILogger _logger;

    public ProcessFeedback(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ProcessFeedback>();
    }

    [Function("ProcessFeedback")]
    public async Task Run([QueueTrigger("feedback-queue", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation($"ProcessFeedback triggered. Message: {queueMessage}");

        var request = JsonSerializer.Deserialize<FeedbackRequest>(queueMessage);
        if (request == null)
        {
            _logger.LogError("Invalid queue message format.");
            return;
        }

        // Extract container and blob name
        var containerName = request.ContainerName;


        var blobClient = new BlobContainerClient("UseDevelopmentStorage=true", containerName);
        var blob = blobClient.GetBlobClient(request.FileName);

        if (!await blob.ExistsAsync())
        {
            _logger.LogError($"Blob not found: {request.FileName}");
            return;
        }

        var download = await blob.DownloadContentAsync();
        string content = download.Value.Content.ToString();

        _logger.LogInformation($"Blob content: {content}");

        var tableClient = new TableClient("UseDevelopmentStorage=true", "FeedbackTable");
        await tableClient.CreateIfNotExistsAsync();

        var entity = new FeedbackEntity
        {
            PartitionKey = "feedback",
            RowKey = Guid.NewGuid().ToString(),
            FileName = request.FileName,
            ContainerName= request.ContainerName,
            Content = content
        };

        await tableClient.AddEntityAsync(entity);
        _logger.LogInformation("Saved feedback to table.");
    }
}