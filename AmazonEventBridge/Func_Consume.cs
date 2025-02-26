using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AmazonEventBridge
{
    public class Func_Consume
    {
        private readonly ILogger<Func_Consume> _logger;
        private readonly AmazonSQSClient _sqsClient;
        private const string QUEUE_URL = "http://localhost:4566/000000000000/StudentEventQueue";

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

                foreach (var message in response.Messages)
                {
                    _logger.LogInformation($"Processing event: {message.Body}");

                    // Deserialize event message
                    var studentEvent = JsonSerializer.Deserialize<Student>(message.Body);

                    // Log event details (Replace with actual processing logic)
                    _logger.LogInformation($"Student ID: {studentEvent.StudentID}, Name: {studentEvent.Firstname} {studentEvent.Lastname}, DOB: {studentEvent.DateOfBirth}");

                    // Delete the message after processing
                    var deleteRequest = new DeleteMessageRequest
                    {
                        QueueUrl = QUEUE_URL,
                        ReceiptHandle = message.ReceiptHandle
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
