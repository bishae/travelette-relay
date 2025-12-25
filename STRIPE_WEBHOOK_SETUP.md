# Stripe Webhook API Version Configuration

## Current Setup
- **Stripe.net Library Version**: 47.1.0
- **Expected API Version**: 2024-10-28.acacia
- **Stripe CLI API Version**: 2025-12-15.clover (default)

## Solution Options

### Option 1: Configure Stripe CLI to Use Matching API Version (Recommended for Development)

When using Stripe CLI for local webhook testing, configure it to use the API version that matches your Stripe.net library:

```bash
# Set the API version for Stripe CLI
stripe listen --api-version 2024-10-28.acacia --forward-to localhost:7044/api/payments/webhook
```

Or add it to your Stripe CLI config file (`~/.config/stripe/config.toml`):
```toml
[default]
api_version = "2024-10-28.acacia"
```

### Option 2: Configure Production Webhook Endpoint

When creating a webhook endpoint in the Stripe Dashboard or via API, specify the API version:

**Via Stripe Dashboard:**
1. Go to Developers → Webhooks
2. Click "Add endpoint"
3. In the API version dropdown, select `2024-10-28.acacia`
4. Add your webhook URL

**Via Stripe API:**
```bash
curl https://api.stripe.com/v1/webhook_endpoints \
  -u sk_test_...: \
  -d url="https://your-domain.com/api/payments/webhook" \
  -d "enabled_events[]"="payment_intent.succeeded" \
  -d "enabled_events[]"="payment_intent.payment_failed" \
  -d api_version="2024-10-28.acacia"
```

### Option 3: Update Stripe.net Library (If Newer Version Available)

Check for the latest Stripe.net version that supports newer API versions:
```bash
dotnet add package Stripe.net --version <latest-version>
```

Then update the webhook endpoint to use the matching API version.

## Current Implementation

The webhook handler currently uses `throwOnApiVersionMismatch: false` to allow processing events from different API versions. This is acceptable for development but should be aligned in production.

## Verification

After configuring, verify the webhook events are using the correct API version by checking the logs:
- Look for `"api_version": "2024-10-28.acacia"` in webhook event JSON
- No API version mismatch errors in logs

