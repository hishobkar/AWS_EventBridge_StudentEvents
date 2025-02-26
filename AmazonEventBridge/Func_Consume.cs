using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AmazonEventBridge
{
    public class Func_Consume
    {
        private readonly ILogger<Func_Consume> _logger;
        private readonly AmazonSQSClient _sqsClient;
        private const string QUEUE_URL = "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/StudentEventQueue";

        public Func_Consume(ILogger<Func_Consume> logger)
        {
            _logger = logger;

            // Initialize AWS SQS client with LocalStack endpoint
            var config = new AmazonSQSConfig
            {
                ServiceURL = "http://localhost:4566" // LocalStack URL
            };

            _sqsClient = new AmazonSQSClient("fake", "fake", config);
        }

        [Function("Func_Consume")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"Azure Function triggered at: {DateTime.UtcNow}");

            try
            {
                // Poll messages from the SQS queue
                var receiveMessageRequest = new ReceiveMessageRequest
                {
                    QueueUrl = QUEUE_URL,
                    MaxNumberOfMessages = 10, // Fetch up to 10 messages per run
                    WaitTimeSeconds = 2
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest);

                if (response.Messages.Count == 0)
                {
                    _logger.LogInformation("No new events in the queue.");
                    return;
                }


                // Deserialize and extract timestamp
                var events = response.Messages
                    .Select(m => new
                    {
                        Message = m,
                        Event = JsonConvert.DeserializeObject<StudentRegisteredEvent>(m.Body),
                        Timestamp = JsonConvert.DeserializeObject<StudentRegisteredEvent>(m.Body).Time // Ensure Timestamp field exists in Student class
                    })
                    .OrderBy(e => e.Timestamp) // Sort chronologically
                    .ToList();

                foreach (var item in events)
                {
                    _logger.LogInformation($"Processing event: {item.Message.Body}");
                    _logger.LogInformation($"Student ID: {item.Event.Detail.StudentID}, Name: {item.Event.Detail.Firstname} {item.Event.Detail.Lastname}, DOB: {item.Event.Detail.DateOfBirth}, Timestamp: {item.Timestamp}");

                    // Delete processed message
                    var deleteRequest = new DeleteMessageRequest
                    {
                        QueueUrl = QUEUE_URL,
                        ReceiptHandle = item.Message.ReceiptHandle
                    };
                    await _sqsClient.DeleteMessageAsync(deleteRequest);
                }

                _logger.LogInformation("All events processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing events: {ex.Message}");
            }
        }
    }
}
