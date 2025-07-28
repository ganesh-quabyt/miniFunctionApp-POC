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

    [Function("SubmitFeedback")]
    public async Task<ObjectResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext context)
    {
        var logger = context.GetLogger("SubmitFeedback");
        logger.LogInformation("SubmitFeedback function triggered.");

        var payload = await req.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<FeedbackRequest>(payload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


        if (body == null || string.IsNullOrWhiteSpace(body.FileName) || string.IsNullOrWhiteSpace(body.ContainerName))
        {
            return new BadRequestObjectResult("Invalid request body. 'FileName' and 'ContainerName' are required.");
        }

        try
        {
            var queueClient = new QueueClient(
            "UseDevelopmentStorage=true",
            "feedback-queue"
        );
            queueClient.CreateIfNotExists();
            var bytes = Encoding.UTF8.GetBytes(payload);

            var response = await queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
            logger.LogInformation("Message successfully sent to feedback-queue.");
            return new OkObjectResult("Message added to queue");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error sending message: {ex.Message}");
            throw new Exception("Failed to send message to the queue.", ex);
        }
    }
}
