using Azure.Storage.Queues;
using FeedbackProcessor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text;
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

        var body = await req.ReadFromJsonAsync<FeedbackRequest>();

        if (body == null || string.IsNullOrWhiteSpace(body.FileName) || string.IsNullOrWhiteSpace(body.ContainerName))
        {
            return new BadRequestObjectResult("Invalid request body. 'FileName' and 'ContainerName' are required.");
        }

        var messageJson = JsonSerializer.Serialize(body);

        try
        {
            var bytes = Encoding.UTF8.GetBytes(messageJson);
            var response = await _queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
            _logger.LogInformation("Message successfully sent to feedback-queue.");
            return new OkObjectResult("Message added to queue");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message: {ex.Message}");
            throw new Exception("Failed to send message to the queue.", ex);
        }
    }
}
