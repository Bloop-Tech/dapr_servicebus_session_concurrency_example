using DaprSessionBugRepro.Models;
using Microsoft.AspNetCore.Mvc;
using Dapr;
using Dapr.Client;

namespace DaprSessionBugRepro.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class MessageController : ControllerBase
{
    private readonly ILogger<MessageController> _logger;
    private readonly DaprClient _daprClient;

    public MessageController(
        ILogger<MessageController> logger, 
        DaprClient daprClient)
    {
        _logger = logger;
        _daprClient = daprClient;
    }


    [Topic("servicebus-pubsub", "session-test-topic", false)]  
    [TopicMetadata("requireSessions", "true")]
    [HttpPost("process")]
    public async Task<IActionResult> ProcessMessage([FromBody] Message message)
    {
        var currentTime = DateTime.UtcNow;
        var sessionId = HttpContext.Request.Headers["Metadata.sessionid"];

        _logger.LogInformation(
            "Message received for session {SessionId} at {CurrentTime}. Waiting 5 seconds before processing..." +
            "Message content: {Content}",
            sessionId,
            currentTime,
            message.Content);

        // Simulate some processing time
        await Task.Delay(5000);

        _logger.LogInformation(
            "Message completed for session {SessionId} at {CurrentTime}. " +
            "Message content: {Content}",
            sessionId,
            currentTime,
            message.Content);

        return NoContent();
    }

    [HttpPost("publish")]
    public async Task<IActionResult> PublishMessage()
    {
        try
        {
            await PublishMessages("SessionA");
            await PublishMessages("SessionB");

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing messages");
            return StatusCode(500, "Error publishing messages");
        }
    }

    private async Task<string> PublishMessages(string sessionId)
    {
        // First message - will start processing
        var firstMessage = new Message
        {
            Content = $"{sessionId}, message 1",
            Timestamp = DateTime.UtcNow
        };

        var metadata = new Dictionary<string, string>
            {
                { "SessionId", sessionId }
            };

        await _daprClient.PublishEventAsync("servicebus-pubsub", "session-test-topic", firstMessage, metadata);
        _logger.LogInformation(
            "Published first message for session {SessionId} at {CurrentTime}. " +
            "Message content: {Content}",
            sessionId,
            DateTime.UtcNow,
            firstMessage.Content);

        // Second message - should wait for first message to complete
        var secondMessage = new Message
        {
            Content = $"{sessionId}, message 2",
            Timestamp = DateTime.UtcNow
        };

        await _daprClient.PublishEventAsync("servicebus-pubsub", "session-test-topic", secondMessage, metadata);
        _logger.LogInformation(
            "Published second message for session {SessionId} at {CurrentTime}. " +
            "Message content: {Content}",
            sessionId,
            DateTime.UtcNow,
            secondMessage.Content);
        return sessionId;
    }
} 