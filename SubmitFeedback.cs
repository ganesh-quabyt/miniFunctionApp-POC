using Azure.Storage.Queues; 
using FeedbackProcessor.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;


public class SubmitFeedback
{
    private readonly ILogger _logger;
    private readonly QueueClient _queueClient;

    public SubmitFeedback(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SubmitFeedback>();

        _queueClient = new QueueClient(
            "UseDevelopmentStorage=true",
            "feedback-queue"
        );

        _queueClient.CreateIfNotExists();

        if (!_queueClient.Exists())
        {
            _logger.LogError("Queue 'feedback-queue' does not exist.");
        }
        else
        {
            _logger.LogInformation("Queue 'feedback-queue' is ready.");
        }
    }

    [Function("SubmitFeedback")]
    public async Task<ObjectResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("SubmitFeedback function triggered.");

        var body = await JsonSerializer.DeserializeAsync<FeedbackRequest>(req.Body);

        if (body == null || string.IsNullOrWhiteSpace(body.FileName) || string.IsNullOrWhiteSpace(body.BlobPath))
        {
            return new BadRequestObjectResult("Invalid request body. 'FileName' and 'BlobPath' are required.");
        }

        var messageJson = JsonSerializer.Serialize(body);

        try
        {
            await _queueClient.SendMessageAsync(messageJson);
            _logger.LogInformation("Message successfully sent to feedback-queue.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message: {ex.Message}");
        }

        
        return new OkObjectResult("Message added to queue");
       

    }
}

