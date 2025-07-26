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
        var parts = request.BlobPath.Trim('/').Split('/', 2);
        string containerName = parts[0];
        string blobName = parts[1];

        var blobClient = new BlobContainerClient("UseDevelopmentStorage=true", containerName);
        var blob = blobClient.GetBlobClient(blobName);

        if (!await blob.ExistsAsync())
        {
            _logger.LogError($"Blob not found: {blobName}");
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
            BlobPath = request.BlobPath,
            Content = content
        };

        await tableClient.AddEntityAsync(entity);
        _logger.LogInformation("Saved feedback to table.");
    }
}