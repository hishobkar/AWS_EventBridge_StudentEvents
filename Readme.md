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






# AWS EventBridge to Azure - Pull and Push Model

This repository demonstrates two approaches to consuming events from **AWS EventBridge** in **Azure**:
1. **Polling Model (Pull-Based)**: Azure Timer Function polls messages from **AWS SQS**.
2. **Push Model (Event-Driven)**: AWS EventBridge pushes events to an **Azure HTTP-triggered Function**.

## Event Model (C# POCO)

```csharp
public class LearnerEvent
{
    public string EventType { get; set; }
    public string Version { get; set; }
    public Guid CorrelationId { get; set; }
    public string CustomerId { get; set; }
    public Guid UserId { get; set; }
    public LearnerData Data { get; set; }
    public DateTime Timestamp { get; set; }
}

public class LearnerData
{
    public string LearnerID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DOB { get; set; }
}
```

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

## 2. Polling Model (Pull-Based) - Azure Function Polls AWS SQS

- **AWS EventBridge** routes events to an **AWS SQS Queue**.
- **Azure Function** reads messages from SQS and processes them.

### **Step 1: Configure AWS SQS as an EventBridge Target**

- In **AWS EventBridge**, create a **Rule** that sends events to an **SQS Queue**.

### **Step 2: Azure Function to Poll AWS SQS**

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
        log.LogInformation($"Received event: {learnerEvent.Data.LearnerID}");
    }
}
```

## 3. Push Model (Event-Driven) - EventBridge Pushes to Azure Function

- **AWS EventBridge** pushes events directly to an **Azure HTTP-triggered Function**.
- Events are sent using **AWS Lambda** or **API Gateway**.

### **Step 1: AWS Lambda to Push to Azure**

- Create an **AWS Lambda function** triggered by EventBridge.
- The Lambda function sends events to an **Azure Function HTTP endpoint**.

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

### **Step 2: Azure HTTP-Triggered Function to Receive Events**

```csharp
using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public static class LearnerEventReceiver
{
    [FunctionName("LearnerEventReceiver")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var learnerEvent = JsonConvert.DeserializeObject<LearnerEvent>(requestBody);
        log.LogInformation($"Received event: {learnerEvent.Data.LearnerID}");
        return new OkResult();
    }
}
```

## 4. Deploy Azure Timer Function (Optional for Polling Model)

- If using **polling**, Azure Timer Function can trigger periodically to fetch messages from SQS.

```csharp
public static class TimerFunction
{
    [FunctionName("TimerFunction")]
    public static void Run(
        [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer,
        ILogger log)
    {
        log.LogInformation($"Azure Timer Function executed at: {DateTime.UtcNow}");
        // Fetch and process SQS messages here
    }
}
```

## Final Workflow

| Approach  | AWS Service | Azure Service |
|-----------|------------|---------------|
| **Polling Model** | AWS SQS | Azure Function polling from SQS |
| **Push Model** | AWS Lambda (or API Gateway) | Azure HTTP-Triggered Function |

## Next Steps

- Deploy the Azure Function and configure AWS IAM permissions for EventBridge.
- Monitor logs in AWS CloudWatch and Azure Application Insights.
- Test and scale based on requirements.

Reference : What is SQS Queue : https://www.youtube.com/watch?v=CyYZ3adwboc

---

**Author:** Your Name  
**License:** MIT  
**Last Updated:** YYYY-MM-DD







Here’s your **GitHub README.md** file with the full explanation and AWS CLI commands for setting up **Azure Function to consume AWS EventBridge events in chronological order**.

---

# **🚀 Consuming AWS EventBridge Events in Azure Function (Chronologically)**
This guide explains how to **directly consume AWS EventBridge events in an Azure Function** while maintaining **chronological order**.  

### **📌 Two Approaches**
1. **Recommended ✅** → **EventBridge → API Destination → Azure Function (Webhook)**
   - Ensures event ordering.
   - No extra AWS services needed.
2. **Alternative** → **EventBridge → SNS → Azure Function**
   - Easier setup but **event order is not guaranteed**.

---

## **1️⃣ Approach 1: EventBridge → API Destination → Azure Function (Best for Chronological Order ✅)**
This approach **directly sends EventBridge events to an Azure Function via HTTP Webhook**.

### **Step 1: Create an Azure Function (HTTP Trigger)**
```csharp
[FunctionName("EventBridgeListener")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
{
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    var eventData = JsonConvert.DeserializeObject<LearnerEvent>(requestBody);

    log.LogInformation($"Received Event: {eventData.EventType} at {eventData.Timestamp}");

    var command = _commandFactory.GetCommand(eventData.EventType);
    if (command != null)
    {
        await command.ExecuteAsync(eventData, "your-jwt-token");
    }
    else
    {
        log.LogWarning("No command found for event type: {EventType}", eventData.EventType);
    }

    return new OkResult();
}
```
### **Step 2: Get Your Azure Function URL**
Deploy the function and copy the **Function URL**  
(e.g., `https://yourfunction.azurewebsites.net/api/EventBridgeListener`).

### **Step 3: Create an API Destination in AWS EventBridge**
#### **1️⃣ Create an API Destination for Azure Function**
```sh
aws events create-api-destination --name "AzureFunctionDestination" \
  --connection-arn "arn:aws:events:your-region:your-account-id:connection/AzureConnection" \
  --invocation-endpoint "https://yourfunction.azurewebsites.net/api/EventBridgeListener" \
  --http-method "POST"
```
✅ **Replace**:
- `your-region` → Your AWS region  
- `your-account-id` → Your AWS account ID  
- `yourfunction.azurewebsites.net` → Your Azure Function URL  

#### **2️⃣ Create an EventBridge Rule to Send Events to Azure**
```sh
aws events put-rule --name "SendToAzureFunction" --event-pattern '{
  "source": ["your.service"],
  "detail-type": ["LearnerRegistered", "LearnerUpdated"]
}' --state ENABLED
```
#### **3️⃣ Attach API Destination as Target**
```sh
aws events put-targets --rule "SendToAzureFunction" --targets '[{
  "Id": "1",
  "Arn": "arn:aws:events:your-region:your-account-id:api-destination/AzureFunctionDestination",
  "RoleArn": "arn:aws:iam::your-account-id:role/EventBridgeToAzureRole"
}]'
```
✅ Now, **EventBridge will send events directly to your Azure Function in order**!

---

## **2️⃣ Approach 2: EventBridge → SNS → Azure Function (Alternative)**
This approach **uses SNS to deliver events**, but **does not guarantee order**.

### **Step 1: Create an SNS Topic**
```sh
aws sns create-topic --name EventBridgeTopic
```
### **Step 2: Subscribe Azure Function to SNS**
```sh
aws sns subscribe --topic-arn "arn:aws:sns:your-region:your-account-id:EventBridgeTopic" \
  --protocol "https" --notification-endpoint "https://yourfunction.azurewebsites.net/api/EventBridgeListener"
```
### **Step 3: Configure EventBridge to Publish to SNS**
```sh
aws events put-targets --rule "SendToSNS" --targets '[{
  "Id": "1",
  "Arn": "arn:aws:sns:your-region:your-account-id:EventBridgeTopic"
}]'
```
✅ Now, **EventBridge will send events to SNS, which will forward them to your Azure Function**.  
🚨 **BUT SNS does NOT guarantee chronological order!** If ordering is important, use **API Destination (Approach 1).**

---

## **🎯 Which Approach Should You Use?**
| Approach | Order Guaranteed? | Complexity | Best For |
|----------|----------------|------------|------------|
| **EventBridge → API Destination → Azure Function (Recommended ✅)** | ✅ Yes | 🔹 Low | Strict ordering needed |
| **EventBridge → SNS → Azure Function** | ❌ No | 🔸 Medium | Basic event forwarding |

---

## **✅ Summary**
- If **ordering matters**, use **EventBridge → API Destination → Azure Function** (Approach 1).  
- If you **only need event forwarding**, use **SNS** (Approach 2).  
- **Do NOT use SNS if you need strict ordering**.

🔥 **Now your Azure Function can consume AWS EventBridge events in order!** 🚀  
Let me know if you need any modifications!

