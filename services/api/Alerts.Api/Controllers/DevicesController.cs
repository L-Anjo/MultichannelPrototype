using Alerting.Shared.Configuration;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alerts.Api.Controllers;

[ApiController]
[Route("devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly IKafkaEventPublisher _publisher;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        IKafkaEventPublisher publisher,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<DevicesController> logger)
    {
        _publisher = publisher;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] DeviceRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId) ||
            string.IsNullOrWhiteSpace(request.UserId) ||
            request.Channels.Count == 0 ||
            string.IsNullOrWhiteSpace(request.NetworkType))
        {
            return BadRequest(new { error = "Payload de device invalido." });
        }

        var deviceEvent = new DeviceRegisteredEvent(
            DeviceId: request.DeviceId,
            UserId: request.UserId,
            Channels: request.Channels,
            PushToken: request.PushToken,
            IsOnline: request.IsOnline,
            NetworkType: request.NetworkType,
            TimestampUtc: DateTime.UtcNow);

        using (_logger.BeginScope(new Dictionary<string, object> { ["deviceId"] = request.DeviceId, ["userId"] = request.UserId }))
        {
            _logger.LogInformation(
                "Device registered for user {UserId} with channels {Channels}",
                request.UserId,
                string.Join(",", request.Channels));

            await _publisher.PublishAsync(
                _kafkaOptions.DevicesRegisteredTopic,
                request.DeviceId,
                deviceEvent,
                cancellationToken);
        }

        return Accepted(new
        {
            deviceId = request.DeviceId,
            userId = request.UserId,
            topic = _kafkaOptions.DevicesRegisteredTopic
        });
    }

    [HttpPut("{deviceId}/status")]
    public async Task<IActionResult> UpdateStatus(
        string deviceId,
        [FromBody] DeviceStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(request.NetworkType))
        {
            return BadRequest(new { error = "Payload de status invalido." });
        }

        var statusEvent = new DeviceStatusUpdatedEvent(
            DeviceId: deviceId,
            IsOnline: request.IsOnline,
            NetworkType: request.NetworkType,
            TimestampUtc: DateTime.UtcNow);

        using (_logger.BeginScope(new Dictionary<string, object> { ["deviceId"] = deviceId }))
        {
            _logger.LogInformation(
                "Device status updated. Online={IsOnline} NetworkType={NetworkType}",
                request.IsOnline,
                request.NetworkType);

            await _publisher.PublishAsync(
                _kafkaOptions.DevicesStatusUpdatedTopic,
                deviceId,
                statusEvent,
                cancellationToken);
        }

        return Accepted(new
        {
            deviceId,
            topic = _kafkaOptions.DevicesStatusUpdatedTopic,
            request.IsOnline,
            request.NetworkType
        });
    }
}
