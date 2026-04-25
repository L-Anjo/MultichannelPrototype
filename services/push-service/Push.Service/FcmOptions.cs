namespace Push.Service;

public sealed class FcmOptions
{
    public const string SectionName = "Fcm";

    public string ProjectId { get; set; } = "demo-project";
    public bool UseMockWhenCredentialsMissing { get; set; } = true;
    public string? CredentialsPath { get; set; }
}
