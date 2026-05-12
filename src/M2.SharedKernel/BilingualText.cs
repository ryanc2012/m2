namespace M2.SharedKernel;

/// <summary>
/// Bilingual value object returned by all API responses (ADR-022).
/// Supported languages: EN and ZHT (Traditional Chinese).
/// </summary>
public sealed record BilingualText(string En, string Zht)
{
    public static BilingualText Empty => new(string.Empty, string.Empty);

    public static BilingualText From(string en, string zht) => new(en, zht);
}
