using Amazon.EventBridge.Model;
using Amazon.EventBridge;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AmazonEventBridge
{
    public class PublishEventFunction
    {
        private readonly ILogger<PublishEventFunction> _logger;
        private readonly IAmazonEventBridge _eventBridgeClient;
        private const string EVENT_BUS_NAME = "StudentEventBus"; // Replace with actual EventBridge Bus Name

        public PublishEventFunction(ILogger<PublishEventFunction> logger, IAmazonEventBridge eventBridgeClient)
        {
            _logger = logger;
            _eventBridgeClient = eventBridgeClient;
        }

        [Function("PublishEvent1")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Azure Function triggered.");

            try
            {
                // Generate 20 random student events
                var students = GenerateRandomStudents(20);

                // Shuffle the list to make it non-chronological
                students = students.OrderBy(_ => Guid.NewGuid()).ToList();

                // Create event entries
                var eventEntries = students.Select(student => new PutEventsRequestEntry
                {
                    Source = "com.student.registration",
                    DetailType = "StudentRegistered",
                    Detail = JsonConvert.SerializeObject(student),
                    EventBusName = EVENT_BUS_NAME
                }).ToList();

                // Send all events in a single batch
                var putEventRequest = new PutEventsRequest { Entries = eventEntries };
                var response = await _eventBridgeClient.PutEventsAsync(putEventRequest);

                if (response.FailedEntryCount > 0)
                {
                    _logger.LogError("Failed to publish some events.");
                    var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await errorResponse.WriteStringAsync("Failed to publish some events.");
                    return errorResponse;
                }

                _logger.LogInformation("All events published successfully.");
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteStringAsync("20 events published successfully.");
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

        private List<Student> GenerateRandomStudents(int count)
        {
            var random = new Random();
            var students = new List<Student>();

            for (int i = 0; i < count; i++)
            {
                students.Add(new Student
                {
                    StudentID = random.Next(100000, 999999).ToString(),
                    Firstname = "Student" + i,
                    Lastname = "Test" + i,
                    DateOfBirth = new DateTime(random.Next(1990, 2010), random.Next(1, 12), random.Next(1, 28)).ToString("yyyy-MM-dd")
                });
            }

            return students;
        }
    }
}
