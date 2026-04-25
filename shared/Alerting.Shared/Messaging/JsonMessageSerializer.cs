using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alerting.Shared.Messaging;

public static class JsonMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T message) => JsonSerializer.Serialize(message, Options);

    public static T? Deserialize<T>(string payload) => JsonSerializer.Deserialize<T>(payload, Options);
}
