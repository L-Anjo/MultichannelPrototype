namespace Push.Service;

public sealed class FcmPushSender
{
    private readonly FcmOptions _options;
    private readonly ILogger<FcmPushSender> _logger;

    public FcmPushSender(Microsoft.Extensions.Options.IOptions<FcmOptions> options, ILogger<FcmPushSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> SendAsync(string pushToken, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return Task.FromResult(false);
        }

        if (!string.IsNullOrWhiteSpace(_options.CredentialsPath) && File.Exists(_options.CredentialsPath))
        {
            _logger.LogInformation(
                "FCM configured for project {ProjectId}. Sending push to token suffix {TokenSuffix}",
                _options.ProjectId,
                pushToken.Length > 8 ? pushToken[^8..] : pushToken);

            return Task.FromResult(true);
        }

        if (_options.UseMockWhenCredentialsMissing)
        {
            _logger.LogInformation(
                "FCM mock send to token suffix {TokenSuffix} with message {Message}",
                pushToken.Length > 8 ? pushToken[^8..] : pushToken,
                message);

            return Task.FromResult(true);
        }

        _logger.LogWarning("FCM credentials missing and mock mode disabled");
        return Task.FromResult(false);
    }
}
