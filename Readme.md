# AWS EventBridge to Azure Timer Function

This repository demonstrates how to publish events to **AWS EventBridge** and consume them using an **Azure Timer Function**.

## Architecture Overview

1. **Publish Events**: A C# application generates and publishes events to AWS EventBridge.
2. **Forward Events**: AWS EventBridge forwards events to either an **AWS Lambda function** or **AWS SQS**.
3. **Consume Events**: An Azure Timer Function processes the events periodically.

## 1. Publishing Events to AWS EventBridge

### **Step 1: Create an EventBridge Event Bus**

- In the AWS console, navigate to **Amazon EventBridge** and create a custom event bus (or use `default`).

### **Step 2: Publish Events from C# Application**

```csharp
using Amazon;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Newtonsoft.Json;

var client = new AmazonEventBridgeClient(RegionEndpoint.USEast1);

for (int i = 0; i < 20; i++)
{
    var learnerEvent = new LearnerEvent
    {
        EventType = "LearnerRegistered",
        Version = "1.0",
        CorrelationId = Guid.NewGuid(),
        CustomerId = "Customer123",
        UserId = Guid.NewGuid(),
        Data = new LearnerData
        {
            LearnerID = $"L-{i}",
            FirstName = "John",
            LastName = "Doe",
            DOB = DateTime.UtcNow.AddYears(-20)
        },
        Timestamp = DateTime.UtcNow
    };

    var request = new PutEventsRequest
    {
        Entries = new List<PutEventsRequestEntry>
        {
            new PutEventsRequestEntry
            {
                Source = "custom.myapp",
                DetailType = "LearnerEvent",
                Detail = JsonConvert.SerializeObject(learnerEvent),
                EventBusName = "default"
            }
        }
    };

    await client.PutEventsAsync(request);
}
```

## 2. Forward Events from AWS to Azure

### **Option 1: Using AWS Lambda to Push to Azure**

- Create an **AWS Lambda function** triggered by EventBridge.
- The Lambda function will send events to an **Azure Function HTTP trigger**.

#### **AWS Lambda (Python) Example**

```python
import json
import requests

def lambda_handler(event, context):
    azure_function_url = "https://your-azure-function.azurewebsites.net/api/LearnerEventReceiver"
    
    for record in event['Records']:
        response = requests.post(azure_function_url, json=record)
        print(response.status_code, response.text)
```

### **Option 2: Using AWS SQS and Azure Function**

- Set up an **SQS queue** as a target in EventBridge.
- Azure Function can pull messages from **AWS SQS** using `Microsoft.Azure.WebJobs.Extensions.AWS`.

#### **Azure Function to Consume AWS SQS**

```csharp
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public static class LearnerEventProcessor
{
    [FunctionName("ProcessLearnerEvent")]
    public static void Run(
        [ServiceBusTrigger("your-queue-name", Connection = "AWS_SQS_ConnectionString")]
        string myQueueItem,
        ILogger log)
    {
        var learnerEvent = JsonConvert.DeserializeObject<LearnerEvent>(myQueueItem);
        log.LogInformation($"Processed Learner Event: {learnerEvent.Data.LearnerID}");
    }
}
```

## 3. Deploy Azure Timer Function

Modify your **Azure Function** to process events periodically:

```csharp
public static class TimerFunction
{
    [FunctionName("TimerFunction")]
    public static void Run(
        [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer,
        ILogger log)
    {
        log.LogInformation($"Azure Function executed at: {DateTime.UtcNow}");
        // Fetch and process SQS messages here
    }
}
```

## Final Workflow

1. C# app publishes events → **AWS EventBridge**.
2. EventBridge forwards to **AWS Lambda** or **SQS**.
3. **Azure Function (Timer or HTTP)** consumes the events.

## Next Steps

- Deploy the Azure Function and configure AWS IAM permissions for EventBridge.
- Monitor logs in AWS CloudWatch and Azure Application Insights.
- Test and scale based on requirements.

---

**Author:** Your Name  
**License:** MIT  
**Last Updated:** YYYY-MM-DD

