# Email Service Setup Guide

## Overview

The UtilityTools platform includes a complete email service implementation for:
- Email verification
- Password reset
- Welcome emails

## Configuration

### SMTP Configuration

Add the following to your `appsettings.json`:

```json
{
  "Email": {
    "Provider": "SMTP",
    "SMTP": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "EnableSSL": true
    },
    "FromAddress": "noreply@utilitytools.com",
    "FromName": "UtilityTools"
  },
  "BaseUrl": "http://localhost:5000"
}
```

### Gmail Setup

1. Enable 2-Step Verification on your Google account
2. Generate an App Password:
   - Go to Google Account settings
   - Security → 2-Step Verification → App passwords
   - Generate a password for "Mail"
3. Use the generated password in `Email:SMTP:Password`

### Other SMTP Providers

#### Outlook/Hotmail
```json
{
  "Email": {
    "SMTP": {
      "Host": "smtp-mail.outlook.com",
      "Port": 587,
      "EnableSSL": true
    }
  }
}
```

#### SendGrid (Future)
```json
{
  "Email": {
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "SG.your-api-key"
    }
  }
}
```

#### Mailgun (Future)
```json
{
  "Email": {
    "Provider": "Mailgun",
    "Mailgun": {
      "ApiKey": "your-api-key",
      "Domain": "your-domain.com"
    }
  }
}
```

## API Endpoints

### 1. Forgot Password
```http
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "message": "If an account with that email exists, a password reset link has been sent."
}
```

### 2. Reset Password
```http
POST /api/auth/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "reset-token-from-email",
  "newPassword": "NewSecurePassword123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password has been reset successfully."
}
```

### 3. Verify Email
```http
POST /api/auth/verify-email
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "verification-token-from-email"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Email verified successfully."
}
```

### 4. Resend Verification
```http
POST /api/auth/resend-verification
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "message": "If an account with that email exists and is not verified, a verification email has been sent."
}
```

## Email Templates

The email service includes HTML email templates for:
- **Email Verification**: Welcome message with verification link
- **Password Reset**: Security notice with reset link
- **Welcome Email**: Sent after successful email verification

All templates are responsive and include:
- Professional styling
- Clear call-to-action buttons
- Security notices
- Expiration information

## Security Features

1. **Token Expiration**:
   - Email verification: 24 hours
   - Password reset: 1 hour

2. **Secure Token Generation**:
   - Uses `RandomNumberGenerator` for cryptographically secure tokens
   - Base64Url encoding for URL-safe tokens

3. **Email Enumeration Prevention**:
   - Always returns success message (even if email doesn't exist)
   - Prevents attackers from discovering valid email addresses

4. **Token Validation**:
   - Validates token format
   - Checks expiration
   - One-time use (cleared after successful use)

## Database Migration

After adding password reset fields to User entity, create a migration:

```bash
dotnet ef migrations add AddPasswordResetFields --project src/UtilityTools.Infrastructure --startup-project src/UtilityTools.Api
dotnet ef database update --project src/UtilityTools.Infrastructure --startup-project src/UtilityTools.Api
```

## Testing

### Local Development

For local development without actual email sending, you can:
1. Use a service like [Mailtrap](https://mailtrap.io/) for testing
2. Check application logs for email content
3. Use a mock email service in development

### Email Testing Tools

- **Mailtrap**: SMTP testing service
- **MailHog**: Local email testing server
- **Ethereal Email**: Temporary email addresses

## Troubleshooting

### Email Not Sending

1. Check SMTP credentials
2. Verify firewall/network allows SMTP connections
3. Check application logs for errors
4. Verify `BaseUrl` is correctly configured

### Token Not Working

1. Check token expiration (1 hour for password reset, 24 hours for verification)
2. Verify token format (Base64Url)
3. Check database for token storage

### Gmail Issues

1. Enable "Less secure app access" (not recommended)
2. Use App Password instead (recommended)
3. Check if account has 2-Step Verification enabled

## Future Enhancements

- [ ] SendGrid adapter
- [ ] Mailgun adapter
- [ ] Email queue (Hangfire)
- [ ] Email templates (Razor/Handlebars)
- [ ] Email analytics
- [ ] Bounce handling
- [ ] Unsubscribe functionality

