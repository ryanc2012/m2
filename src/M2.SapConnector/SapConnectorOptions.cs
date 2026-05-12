namespace M2.SapConnector;

public sealed class SapConnectorOptions
{
    public const string SectionName = "SapConnector";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
