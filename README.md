# Dapr Session Bug Repro

This project demonstrates a potential bug in Dapr's handling of Azure Service Bus sessions, where messages with the same session ID are being processed concurrently despite session support being enabled.

## Prerequisites

- .NET 8.0 SDK
- Dapr CLI
- Azure Service Bus namespace with a topic and subscription configured for sessions

## Setup

1. Create the required Azure Service Bus resources:
   - Create a topic named `session-test-topic`
   - Create a subscription named `session-test-subscriber` under the topic
   - Enable session support on the subscription
   - Make sure the subscription has the following settings:
     - `RequiresSession`: true

2. Update the `components/servicebus-pubsub.yaml` file with your Azure Service Bus connection string.

3. Run the application with Dapr:
```bash
dapr run --app-id session-test --app-port 5250 --dapr-http-port 3500 --components-path ./components dotnet run
```

## How to Test

1. Once the application is running, send a POST request to the publish endpoint:
```bash
curl -X POST http://localhost:5250/message/publish
```

This endpoint will:
- Generate a new session ID
- Publish two messages with the same session ID to the "session-test-topic"
- Log the publishing of both messages

2. Watch the application logs. You should see warning messages if messages with the same session ID are being processed concurrently.

## Expected Behavior

With session support enabled, messages with the same session ID should be processed sequentially. The application logs will show warnings if concurrent processing is detected.

## Logging

The application logs:
- First message received for a session
- Concurrent processing of messages with the same session ID
- Time elapsed since the last message was processed for each session 