namespace Alerting.Shared.Models;

public sealed record CapAlertData(
    string Identifier,
    string Sender,
    string Urgency,
    string Severity,
    string Description);
