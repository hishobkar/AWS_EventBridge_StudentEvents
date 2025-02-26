
## Install AWS SDK for .NET

dotnet add package AWSSDK.EventBridge
dotnet add package AWSSDK.SQS


## Create an SQS Queue and Subscribe It to EventBridge

Since EventBridge does not support direct polling, set up AWS SQS as a subscriber:

1. Create an SQS queue in LocalStack:
````cmd
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name StudentEventQueue
````

2. Get the Queue ARN:
````cmd
aws --endpoint-url=http://localhost:4566 sqs get-queue-attributes \
    --queue-url http://localhost:4566/000000000000/StudentEventQueue \
    --attribute-name QueueArn
````
Copy the ARN (e.g., arn:aws:sqs:us-east-1:000000000000:StudentEventQueue)


3. Subscribe the queue to EventBridge:
````cmd
aws --endpoint-url=http://localhost:4566 events put-targets \
    --event-bus-name StudentEventBus \
    --rule StudentRule \
    --targets "[{\"Id\":\"1\",\"Arn\":\"arn:aws:sqs:us-east-1:000000000000:StudentEventQueue\"}]"
````

4. Allow EventBridge to send messages to SQS:
````cmd
aws --endpoint-url=http://localhost:4566 sqs set-queue-attributes \
    --queue-url http://localhost:4566/000000000000/StudentEventQueue \
    --attributes '{"Policy": "{\"Version\": \"2012-10-17\", \"Statement\": [{ \"Effect\": \"Allow\", \"Principal\": \"*\", \"Action\": \"sqs:SendMessage\", \"Resource\": \"arn:aws:sqs:us-east-1:000000000000:StudentEventQueue\"}]}"}'
````

## How to Test the Function

### 1. Start LocalStack and Ensure EventBridge & SQS Are Set Up

If you haven't already, start LocalStack:
````cmd
docker run -d --name localstack -p 4566:4566 -p 4510-4559:4510-4559 localstack/localstack
````

### Then, create the EventBridge event bus and SQS queue:
```cmd
aws --endpoint-url=http://localhost:4566 events create-event-bus --name StudentEventBus
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name StudentEventQueue
```

### Attach SQS to EventBridge:
```cmd
aws --endpoint-url=http://localhost:4566 events put-targets \
    --event-bus-name StudentEventBus \
    --rule StudentRule \
    --targets "[{\"Id\":\"1\",\"Arn\":\"arn:aws:sqs:us-east-1:000000000000:StudentEventQueue\"}]"
```

### Give EventBridge permissions to write to SQS:
```cmd
aws --endpoint-url=http://localhost:4566 sqs set-queue-attributes \
    --queue-url http://localhost:4566/000000000000/StudentEventQueue \
    --attributes '{"Policy": "{\"Version\": \"2012-10-17\", \"Statement\": [{ \"Effect\": \"Allow\", \"Principal\": \"*\", \"Action\": \"sqs:SendMessage\", \"Resource\": \"arn:aws:sqs:us-east-1:000000000000:StudentEventQueue\"}]}"}'
```

## 2. Start the Azure Function Locally
In your Azure Function App, run:

```cmd
func start
```

## 3. Publish a Test Event (Trigger the Publisher Function)
From your Publish Function, send an event:

```cmd
curl -X POST "http://localhost:7071/api/PublishEvent" \
     -H "Content-Type: application/json" \
     -d '{
           "StudentID": "12345",
           "Firstname": "John",
           "Lastname": "Doe",
           "DateOfBirth": "2000-01-01"
         }'
```

Verify the event is in SQS:
```cmd
aws --endpoint-url=http://localhost:4566 sqs receive-message \
    --queue-url http://localhost:4566/000000000000/StudentEventQueue
```


## 4. Wait for the Timer-Triggered Function to Run
The function runs every 5 minutes. You can also manually trigger it by setting the cron schedule to "*/1 * * * * *" (every second) temporarily.

## 5. Check Logs
If events were consumed successfully, you should see logs like:
```sql
Azure Function triggered at: 2025-02-26T14:00:00Z
Processing event: {"StudentID":"12345","Firstname":"John","Lastname":"Doe","DateOfBirth":"2000-01-01"}
Student ID: 12345, Name: John Doe, DOB: 2000-01-01
All events processed successfully.
```


# Final Summary

✔ Step 1: Created an Azure Function Timer Trigger (0 */5 * * * *)

✔ Step 2: Connected to AWS SQS (subscribed to EventBridge)

✔ Step 3: Implemented polling, event processing, and message deletion

✔ Step 4: Tested by sending events and checking logs

Now, your Azure Function in .NET Core automatically consumes EventBridge events in chronological order via SQS, running every 5 minutes! 🎯🚀