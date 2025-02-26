# Azure Functions with AWS EventBridge in LocalStack

This guide provides a step-by-step approach to consuming events in a **chronological manner** using **Azure Functions**, **AWS EventBridge**, and **LocalStack**.

## Prerequisites

- Docker installed on your machine
- AWS CLI and LocalStack CLI installed
- Azure Functions Core Tools installed

---

## Step 1: Start LocalStack Container

Run LocalStack in a Docker container:

```sh
docker run -d --name localstack -p 4566:4566 -p 4510-4559:4510-4559 localstack/localstack
```

This ensures all AWS services (SQS, EventBridge, etc.) are available locally.

---

## Step 2: Install AWS CLI and LocalStack CLI

If not installed, install the AWS CLI and LocalStack CLI:

```sh
pip install awscli-local
```

Configure LocalStack:

```sh
aws configure set aws_access_key_id fake
aws configure set aws_secret_access_key fake
aws configure set region us-east-1
```

Set environment variables:

```sh
export AWS_ACCESS_KEY_ID=fake
export AWS_SECRET_ACCESS_KEY=fake
export AWS_DEFAULT_REGION=us-east-1
```

---

## Step 3: Create AWS Resources in LocalStack

### 1. Create an SQS Queue

```sh
awslocal sqs create-queue --queue-name StudentEventQueue
```

### 2. Create an EventBridge Event Bus

```sh
awslocal events create-event-bus --name StudentEventBus
```

### 3. Create an EventBridge Rule to Send Events to SQS

```sh
awslocal events put-rule --name StudentEventRule --event-bus-name StudentEventBus --event-pattern '{"source": ["com.student.registration"]}'
```

### 4. Create an SQS Target for the EventBridge Rule

```sh
awslocal events put-targets --rule StudentEventRule --event-bus-name StudentEventBus --targets "[{\"Id\":\"1\", \"Arn\":\"$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/StudentEventQueue --attribute-name QueueArn --query Attributes.QueueArn --output text)\"}]"
```

### 5. Give EventBridge Permissions to Write to SQS

```sh
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/StudentEventQueue --attributes '{"Policy":"{\"Version\": \"2012-10-17\", \"Statement\": [{ \"Effect\": \"Allow\", \"Principal\": \"*\", \"Action\": \"sqs:SendMessage\", \"Resource\": \"*\"}]}"}'
```

---

## Step 4: Deploy and Run the Azure Functions

### 1. Run Azure Functions Locally

Ensure Azure Functions Core Tools are installed, then start the function:

```sh
func start
```

### 2. Test the Publish Function

Send a test event:

```sh
curl -X POST "http://localhost:7071/api/PublishEvent" -H "Content-Type: application/json" -d '{"StudentID": "123", "Firstname": "John", "Lastname": "Doe", "DateOfBirth": "2000-01-01"}'
```

---

## Step 5: Verify Message Processing

Check if the event is received in SQS:

```sh
awslocal sqs receive-message --queue-url http://localhost:4566/000000000000/StudentEventQueue
```

If successful, Azure Function `Func_Consume` will process and delete it.

---

## Step 6: Monitor Logs

Check LocalStack logs:

```sh
docker logs -f localstack
```

Check Azure Function logs:

```sh
func logs
```

Ensure messages are consumed in the correct order.

---

## Step 7: Cleanup

To stop and remove LocalStack:

```sh
docker stop localstack && docker rm localstack
```

---

## Summary

This step-by-step guide helps you set up an **Azure Function** that publishes and consumes events from **AWS EventBridge** via **SQS in LocalStack**, ensuring chronological order of processing.





## Install AWS SDK for .NET

dotnet add package AWSSDK.EventBridge

dotnet add package AWSSDK.SQS


## a. Start LocalStack with EventBridge
```cmd
docker run -d --name localstack -p 4566:4566 -p 4510-4559:4510-4559 localstack/localstack
```

## b. Ensure that EventBridge is running by executing:
```cmd
aws --endpoint-url=http://localhost:4566 events list-rules
```

## c. Configure AWS CLI with Dummy Credentials
Run the following command to set up AWS CLI with fake credentials:
```cmd
aws configure set aws_access_key_id test
```

```cmd
aws configure set aws_secret_access_key test
```

```cmd
aws configure set default.region us-east-1
```

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


If you get error in above command the create the rule
Create the StudentRule before adding the target:
```cmd
aws --endpoint-url=http://localhost:4566 events put-rule \
    --name StudentRule \
    --event-bus-name StudentEventBus \
    --event-pattern '{ "source": ["student.events"] }' \
    --state ENABLED
```

Confirm that the rule was created:
```cmd
aws --endpoint-url=http://localhost:4566 events list-rules --event-bus-name StudentEventBus
```



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