using System.Xml.Linq;
using Alerting.Shared.Configuration;
using Alerting.Shared.Extensions;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alerts.Api.Controllers;

[ApiController]
[Route("alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly IKafkaEventPublisher _publisher;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IKafkaEventPublisher publisher,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<AlertsController> logger)
    {
        _publisher = publisher;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("application/xml", "text/xml")]
    public async Task<IActionResult> CreateAlert(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var xmlPayload = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(xmlPayload))
        {
            return BadRequest(new { error = "Payload XML vazio." });
        }

        if (!TryParseCap(xmlPayload, out var capAlert, out var error))
        {
            return BadRequest(new { error });
        }

        var eventId = Guid.NewGuid();
        var targetUserId = ResolveTargetUserId();
        var isBroadcast = IsBroadcast();
        var createdEvent = new AlertCreatedEvent(
            EventId: eventId,
            Source: "CAP",
            Message: capAlert!.Description,
            Priority: (capAlert.Urgency, capAlert.Severity).ToPriority(),
            TimestampUtc: DateTime.UtcNow,
            OriginalFormat: "CAP",
            CapIdentifier: capAlert.Identifier,
            Sender: capAlert.Sender,
            Urgency: capAlert.Urgency,
            Severity: capAlert.Severity,
            TargetUserId: targetUserId,
            IsBroadcast: isBroadcast);

        using (_logger.BeginScope(new Dictionary<string, object> { ["eventId"] = eventId }))
        {
            _logger.LogInformation(
                "CAP alert accepted with identifier {Identifier} from sender {Sender} for target {TargetUserId} broadcast {IsBroadcast}",
                capAlert.Identifier,
                capAlert.Sender,
                targetUserId,
                isBroadcast);

            await _publisher.PublishAsync(
                _kafkaOptions.AlertsCreatedTopic,
                createdEvent.EventId.ToString(),
                createdEvent,
                cancellationToken);
        }

        return Accepted(new
        {
            eventId = createdEvent.EventId,
            status = "queued",
            priority = createdEvent.Priority.ToString().ToUpperInvariant(),
            topic = _kafkaOptions.AlertsCreatedTopic,
            targetUserId,
            isBroadcast
        });
    }

    private string ResolveTargetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var headerValues))
        {
            var userId = headerValues.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return userId;
            }
        }

        return "user-123";
    }

    private bool IsBroadcast()
    {
        if (Request.Headers.TryGetValue("X-Broadcast", out var values) &&
            bool.TryParse(values.ToString(), out var isBroadcast))
        {
            return isBroadcast;
        }

        return false;
    }

    private static bool TryParseCap(string xmlPayload, out CapAlertData? capAlert, out string? error)
    {
        capAlert = null;
        error = null;

        try
        {
            var document = XDocument.Parse(xmlPayload, LoadOptions.PreserveWhitespace);
            var root = document.Root;
            var info = root?.Element("info");

            if (root?.Name.LocalName != "alert" || info is null)
            {
                error = "XML invalido: estrutura CAP basica nao encontrada.";
                return false;
            }

            var identifier = root.Element("identifier")?.Value.Trim();
            var sender = root.Element("sender")?.Value.Trim();
            var urgency = info.Element("urgency")?.Value.Trim();
            var severity = info.Element("severity")?.Value.Trim();
            var description = info.Element("description")?.Value.Trim();

            if (string.IsNullOrWhiteSpace(identifier) ||
                string.IsNullOrWhiteSpace(sender) ||
                string.IsNullOrWhiteSpace(urgency) ||
                string.IsNullOrWhiteSpace(severity) ||
                string.IsNullOrWhiteSpace(description))
            {
                error = "XML invalido: campos obrigatorios em falta.";
                return false;
            }

            capAlert = new CapAlertData(identifier, sender, urgency, severity, description);
            return true;
        }
        catch (Exception)
        {
            error = "XML invalido: nao foi possivel analisar o documento.";
            return false;
        }
    }
}
