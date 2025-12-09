using UtilityTools.Domain.Common;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.ValueObjects;

namespace UtilityTools.Domain.Entities;

/// <summary>
/// User entity representing application users with subscription tiers
/// </summary>
public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public SubscriptionTier SubscriptionTier { get; private set; } = SubscriptionTier.Free;
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    public ICollection<Role> Roles { get; private set; } = new List<Role>();
    public ICollection<UsageRecord> UsageRecords { get; private set; } = new List<UsageRecord>();
    public ICollection<Job> Jobs { get; private set; } = new List<Job>();

    private User() { }

    public User(string email, string passwordHash, string firstName, string lastName)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        UpdateTimestamp();
    }

    public void UpdateSubscription(SubscriptionTier tier, string? stripeCustomerId, string? stripeSubscriptionId, DateTime? expiresAt)
    {
        SubscriptionTier = tier;
        StripeCustomerId = stripeCustomerId;
        StripeSubscriptionId = stripeSubscriptionId;
        SubscriptionExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        UpdateTimestamp();
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
        EmailVerificationToken = null;
        UpdateTimestamp();
    }

    public void SetEmailVerificationToken(string token)
    {
        EmailVerificationToken = token;
        UpdateTimestamp();
    }

    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        UpdateTimestamp();
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdateTimestamp();
    }

    public bool CanUseFeature(string featureName, int requiredTier)
    {
        if (SubscriptionTier == SubscriptionTier.Admin) return true;
        return (int)SubscriptionTier >= requiredTier;
    }
}

