using Amazon.EventBridge.Model;
using Amazon.EventBridge;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AmazonEventBridge
{
    public class Func_Publish
    {
        private readonly ILogger<Func_Publish> _logger;
        private readonly AmazonEventBridgeClient _eventBridgeClient;
        private const string EVENT_BUS_NAME = "StudentEventBus";

        public Func_Publish(ILogger<Func_Publish> logger)
        {
            _logger = logger;

            // Initialize AWS EventBridge client with LocalStack endpoint
            var config = new AmazonEventBridgeConfig
            {
                ServiceURL = "http://localhost:4566" // LocalStack URL
            };

            _eventBridgeClient = new AmazonEventBridgeClient("fake", "fake", config);
        }

        [Function("PublishEvent")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Azure Function triggered.");

            try
            {
                // Read the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var student = JsonSerializer.Deserialize<Student>(requestBody);

                if (student == null || string.IsNullOrEmpty(student.StudentID))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid student data.");
                    return badResponse;
                }

                // Create an EventBridge event
                var eventEntry = new PutEventsRequestEntry
                {
                    Source = "com.student.registration",
                    DetailType = "StudentRegistered",
                    Detail = JsonSerializer.Serialize(student),
                    EventBusName = EVENT_BUS_NAME
                };

                var putEventRequest = new PutEventsRequest
                {
                    Entries = new List<PutEventsRequestEntry> { eventEntry }
                };

                var response = await _eventBridgeClient.PutEventsAsync(putEventRequest);

                if (response.FailedEntryCount > 0)
                {
                    _logger.LogError("Failed to publish event.");
                    var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await errorResponse.WriteStringAsync("Failed to publish event.");
                    return errorResponse;
                }

                _logger.LogInformation("Event published successfully.");
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteStringAsync("Event published successfully.");
                return successResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal Server Error.");
                return errorResponse;
            }
        }

    }
}
