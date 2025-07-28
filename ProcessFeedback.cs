using Azure.Data.Tables;
using Azure.Storage.Blobs;
using FeedbackProcessor.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class ProcessFeedback
{

    [Function("ProcessFeedback")]
    public async Task Run([QueueTrigger("feedback-queue", Connection = "AzureWebJobsStorage")] string queueMessage, FunctionContext context)
    {
        var logger = context.GetLogger("ProcessFeedback");
        logger.LogInformation($"ProcessFeedback triggered. Message: {queueMessage}");

        if (string.IsNullOrWhiteSpace(queueMessage))
        {
            logger.LogError("Queue message is empty or null.");
            return;
        }

        var request = JsonSerializer.Deserialize<FeedbackRequest>(queueMessage);
        try
        {

            if (request != null)
            {
                var blobClient = new BlobContainerClient("UseDevelopmentStorage=true", request.ContainerName);
                var blob = blobClient.GetBlobClient(request.FileName);

                if (!await blob.ExistsAsync())
                {
                    logger.LogError($"Blob not found: {request.FileName}");
                    return;
                }

                var download = await blob.DownloadContentAsync();
                string content = download.Value.Content.ToString();

                logger.LogInformation($"Blob content: {content}");

                var tableClient = new TableClient("UseDevelopmentStorage=true", "FeedbackTable");
                await tableClient.CreateIfNotExistsAsync();

                var entity = new FeedbackEntity
                {
                    PartitionKey = "feedback",
                    RowKey = Guid.NewGuid().ToString(),
                    FileName = request.FileName,
                    ContainerName = request.ContainerName,
                    Content = content
                };
                await tableClient.AddEntityAsync(entity);
                logger.LogInformation("Saved feedback to table.");
            }
        }
        catch (Exception)
        {

            throw;
        }
    }
}