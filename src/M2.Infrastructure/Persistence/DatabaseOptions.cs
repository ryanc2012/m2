namespace M2.Infrastructure.Persistence;

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
}
