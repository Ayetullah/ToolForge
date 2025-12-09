# Stripe Payment Integration Setup

## ✅ Completed

### Payment Service
- ✅ **StripePaymentService** - Full Stripe API integration
  - Customer creation
  - Subscription management (create, update, cancel)
  - Webhook signature verification
  - Price ID to tier mapping

### Subscription Management
- ✅ **CreateSubscription** command
  - Create Stripe customer (if not exists)
  - Attach payment method
  - Create subscription
  - Update user subscription in database

- ✅ **CancelSubscription** command
  - Cancel Stripe subscription
  - Downgrade user to Free tier
  - Update database

### Webhook Handling
- ✅ **HandleWebhook** command
  - Signature verification
  - Event parsing
  - Subscription events (created, updated, deleted)
  - Payment events (succeeded, failed)
  - Automatic user subscription updates

### Premium Feature Checks
- ✅ **SubscriptionHelper** utility
  - Check user subscription tier
  - Check subscription expiration
  - Get required tier for tools
  - Background removal requires Pro tier

### API Endpoints
- ✅ `POST /api/payments/subscribe` - Create subscription
- ✅ `POST /api/payments/cancel` - Cancel subscription
- ✅ `POST /api/payments/webhook` - Stripe webhook handler

## 🔧 Configuration

### appsettings.json
```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_stripe_secret_key",
    "WebhookSecret": "whsec_your_webhook_secret",
    "PublishableKey": "pk_test_your_stripe_publishable_key",
    "PriceIds": {
      "Basic": "price_basic_monthly",
      "Pro": "price_pro_monthly",
      "Enterprise": "price_enterprise_monthly"
    }
  }
}
```

### Stripe Setup
1. Create Stripe account
2. Create Products and Prices in Stripe Dashboard
3. Configure webhook endpoint: `https://your-api.com/api/payments/webhook`
4. Add webhook events:
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`
5. Copy webhook signing secret to configuration

## 📝 Usage

### Create Subscription
```bash
curl -X POST http://localhost:5000/api/payments/subscribe \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tier": "Pro",
    "paymentMethodId": "pm_1234567890"
  }'
```

### Cancel Subscription
```bash
curl -X POST http://localhost:5000/api/payments/cancel \
  -H "Authorization: Bearer TOKEN"
```

### Webhook (Stripe calls this)
```bash
# Stripe automatically calls this endpoint
POST /api/payments/webhook
Headers:
  Stripe-Signature: t=timestamp,v1=signature
Body: Stripe event JSON
```

## 🔒 Premium Features

### Tool Tier Requirements
- **Free**: PDF Merge, PDF Split, Image Compress, Excel Clean, JSON Format, Regex Generate, AI Summarize
- **Basic**: Video Compress, Doc to PDF
- **Pro**: Image Background Removal
- **Enterprise**: All features + priority support

### Subscription Tiers
- **Free (0)**: Basic tools only
- **Basic (1)**: Free + Video/Doc tools
- **Pro (2)**: Basic + Background removal
- **Enterprise (3)**: All features
- **Admin (99)**: Full access

## 📊 Subscription Flow

1. User registers/login
2. User selects subscription tier
3. Frontend creates Stripe PaymentMethod
4. Backend creates Stripe Customer (if needed)
5. Backend creates Stripe Subscription
6. User confirms payment
7. Stripe sends webhook events
8. Backend updates user subscription in database

## 🔍 Webhook Events Handled

- `customer.subscription.created` - New subscription
- `customer.subscription.updated` - Subscription changed (tier, status)
- `customer.subscription.deleted` - Subscription cancelled
- `invoice.payment_succeeded` - Payment successful
- `invoice.payment_failed` - Payment failed

## 🚀 Next Steps

1. **Frontend Integration**
   - Stripe Elements for payment form
   - Subscription management UI
   - Payment method management

2. **Enhanced Features**
   - Usage-based billing
   - Invoice generation
   - Payment retry logic
   - Subscription upgrade/downgrade flows

3. **Testing**
   - Stripe test mode integration
   - Webhook testing with Stripe CLI
   - Subscription flow E2E tests

## 📚 References

- [Stripe .NET SDK](https://github.com/stripe/stripe-dotnet)
- [Stripe API Documentation](https://stripe.com/docs/api)
- [Stripe Webhooks](https://stripe.com/docs/webhooks)

