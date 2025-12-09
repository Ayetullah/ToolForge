namespace UtilityTools.Application.Common.Options;

/// <summary>
/// Stripe payment configuration settings
/// </summary>
public class StripeSettings
{
    public const string SectionName = "Stripe";
    
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public PriceIdsSettings PriceIds { get; set; } = new();
}

public class PriceIdsSettings
{
    public string? Basic { get; set; }
    public string? Pro { get; set; }
    public string? Enterprise { get; set; }
}

