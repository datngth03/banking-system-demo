# Payment Integration Guide - Stripe Test Mode

## Overview

The Banking System integrates with **Stripe Test Mode** to process card payments in a safe, non-production environment. All payments in this mode use test card numbers that never charge real money.

This integration enables:
- ✅ Direct card charges via `/api/v1/payments/charge`
- ✅ Bill payments with cards via `/api/v1/payments/pay-bill`
- ✅ Full and partial refunds via `/api/v1/payments/{id}/refund`
- ✅ Webhook-based payment status updates
- ✅ Complete audit trail with Stripe transaction IDs

---

## Getting Started

### 1. Prerequisites

You need a **Stripe test account**. Create one at: https://dashboard.stripe.com/register

### 2. Environment Setup

Add your Stripe API keys to your environment:

**Windows (PowerShell):**
```powershell
$env:STRIPE_SECRET_KEY = "sk_test_YOUR_KEY_HERE"
$env:STRIPE_PUBLISHABLE_KEY = "pk_test_YOUR_KEY_HERE"  
$env:STRIPE_WEBHOOK_SECRET = "whsec_YOUR_SECRET_HERE"
```

**Linux/macOS (Bash):**
```bash
export STRIPE_SECRET_KEY="sk_test_YOUR_KEY_HERE"
export STRIPE_PUBLISHABLE_KEY="pk_test_YOUR_KEY_HERE"
export STRIPE_WEBHOOK_SECRET="whsec_YOUR_SECRET_HERE"
```

**Docker:**
```yaml
# docker-compose.yml
environment:
  STRIPE_SECRET_KEY: sk_test_YOUR_KEY_HERE
  STRIPE_PUBLISHABLE_KEY: pk_test_YOUR_KEY_HERE
  STRIPE_WEBHOOK_SECRET: whsec_YOUR_SECRET_HERE
```

### 3. Find Your Stripe Keys

1. Go to https://dashboard.stripe.com
2. Click **Developers** → **API Keys**
3. Copy your **Secret Key** (starts with `sk_test_`)
4. Copy your **Publishable Key** (starts with `pk_test_`)
5. For webhooks: **Developers** → **Webhooks** → Select endpoint → Copy signing secret

---

## API Endpoints

### Charge Card (`POST /api/v1/payments/charge`)

Direct card charge for any amount.

**Request:**
```json
{
  "paymentMethodId": "pm_card_visa",
  "amount": 10000,
  "currency": "USD",
  "description": "Payment for services",
  "receiptEmail": "customer@example.com"
}
```

**Response (Success):**
```json
{
  "transactionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "stripePaymentId": "pi_1234567890",
  "status": "succeeded",
  "amount": 10000,
  "currency": "USD",
  "paymentIntentStatus": "succeeded",
  "createdAt": "2026-01-28T12:00:00Z",
  "updatedAt": "2026-01-28T12:00:00Z"
}
```

**Response (Pending - 3D Secure):**
```json
{
  "status": "requires_action",
  "clientSecret": "pi_1234567890_secret_abc123...",
  "paymentIntentStatus": "requires_action"
}
```

---

### Pay Bill with Card (`POST /api/v1/payments/pay-bill`)

Pay a bill using a card payment method.

**Request:**
```json
{
  "billId": "b1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "paymentMethodId": "pm_card_visa",
  "receiptEmail": "customer@example.com"
}
```

**Response:**
```json
{
  "transactionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "stripePaymentId": "pi_1234567890",
  "status": "processing",
  "amount": 50000,
  "currency": "USD"
}
```

---

### Refund Payment (`POST /api/v1/payments/{transactionId}/refund`)

Refund a charge (full or partial).

**Request (Full Refund):**
```json
{
  "reason": "Customer requested refund"
}
```

**Request (Partial Refund):**
```json
{
  "amount": 5000,
  "reason": "Partial refund for discount"
}
```

**Response:**
```json
{
  "transactionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "stripePaymentId": "re_1234567890",
  "status": "refunded",
  "amount": 50000,
  "currency": "USD"
}
```

---

### Webhook Endpoint (`POST /api/v1/payments/webhook`)

Stripe sends payment updates here. Handled automatically - no manual calls needed.

**Events processed:**
- `charge.succeeded` - Payment completed
- `charge.failed` - Payment declined
- `charge.refunded` - Refund completed
- `charge.dispute.created` - Chargeback filed
- `payment_intent.succeeded` - PaymentIntent completed
- `payment_intent.payment_failed` - PaymentIntent failed

---

## Test Cards

Use these test card numbers in Stripe test mode. All require:
- **Expiry:** Any future date (e.g., 12/25)
- **CVC:** Any 3 digits (e.g., 123)
- **ZIP:** Any value (e.g., 12345)

### Success Cases
```
4242 4242 4242 4242  ✅ Always succeeds (no authentication)
4000 0000 0000 3220  ✅ Succeeds (with 3D Secure)
```

### Failure Cases
```
4000 0000 0000 0002  ❌ Always declines
4000 0000 0000 0069  ❌ Card expired
4000 0025 0000 0003  ❌ Insufficient funds
4000 0000 0000 0069  ❌ Lost card
4000 0000 0000 0127  ❌ Incorrect CVC
```

### Special Cases
```
4000 0000 0000 3238  ⚠️ Requires 3D Secure
4000 0000 0000 3246  ⚠️ Requires 3D Secure (async)
3782 822463 10005    🏧 American Express test card
```

---

## Webhook Configuration

To receive payment updates from Stripe:

1. **Go to Stripe Dashboard:**
   - Click **Developers** → **Webhooks**
   - Click **Add Endpoint**

2. **Configure Endpoint:**
   - **Endpoint URL:** `https://your-api.com/api/v1/payments/webhook`
   - **Events:** Select:
     - `charge.succeeded`
     - `charge.failed`
     - `charge.refunded`
     - `charge.dispute.created`
     - `payment_intent.succeeded`
     - `payment_intent.payment_failed`

3. **Copy Webhook Secret:**
   - After creating endpoint, copy the **Signing Secret**
   - Set as `STRIPE_WEBHOOK_SECRET` environment variable

4. **Test Webhook (Development):**
   ```bash
   # Use Stripe CLI to forward webhooks locally
   stripe listen --forward-to localhost:5000/api/v1/payments/webhook
   
   # In another terminal, trigger test event
   stripe trigger charge.succeeded
   ```

---

## Idempotency & Retries

All payment requests are **idempotent** using Stripe idempotency keys:
- Same request replayed = Same result (no duplicate charges)
- Idempotency key format: `{UserId}-{Amount}-{Currency}-{UnixTimestamp}`
- Retry strategy: Up to 3 attempts with exponential backoff (1s, 2s, 4s)

**Retryable errors:**
- Network timeout
- Rate limiting (429)
- Server error (500)
- Stripe temporarily unavailable

**Non-retryable errors:**
- Invalid card (400)
- Insufficient funds (402)
- Card declined (decline)

---

## Error Handling

All payment operations return structured error responses:

```json
{
  "status": "failed",
  "errorMessage": "Your card was declined",
  "errorCode": "card_declined",
  "createdAt": "2026-01-28T12:00:00Z"
}
```

### Common Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| `card_declined` | Card was rejected | Try different card |
| `expired_card` | Card expiration date passed | Use new card |
| `incorrect_cvc` | CVC check failed | Verify CVC |
| `insufficient_funds` | Insufficient balance | Top up card |
| `processing_error` | Stripe API error | Retry after 30s |
| `rate_limit_exceeded` | Too many requests | Retry after 1s |
| `authentication_error` | Invalid API key | Check keys |

---

## Transaction Status Lifecycle

All payments follow this status flow:

```
Pending → Processing → Succeeded ✅
       ↘ Failed ❌
```

**Status Details:**

| Status | Meaning | Next Steps |
|--------|---------|-----------|
| `pending` | Payment created, awaiting processing | Wait for webhook |
| `processing` | Processing at Stripe | Wait for webhook |
| `succeeded` | Payment completed successfully | Transaction confirmed |
| `failed` | Payment declined or error | Retry with different card |
| `refunded` | Payment refunded to customer | Funds returned in 3-5 business days |
| `disputed` | Chargeback filed | Investigate and respond |

---

## Payment Flow Diagram

```
Client Application
        ↓
    Charge Card Request
        ↓
PaymentsController.Charge()
        ↓
ChargeCardCommand
        ↓
IPaymentService.CreatePaymentIntentAsync()
        ↓
Stripe API (creates PaymentIntent)
        ↓
Response with clientSecret (if 3D Secure needed)
        ↓
Transaction saved to database
        ↓
PaymentInitiatedEvent published
        ↓
Frontend handles 3D Secure confirmation (if needed)
        ↓
Stripe webhook: charge.succeeded
        ↓
HandlePaymentWebhookCommand
        ↓
Transaction status updated
        ↓
PaymentProcessedEvent published
        ↓
Customer email sent
```

---

## Development Checklist

Before going to production:

- [ ] Stripe test account created and verified
- [ ] API keys added to environment variables
- [ ] Webhook endpoint configured and receiving events
- [ ] Test cards validated (4242-4242-4242-4242)
- [ ] Charge endpoint tested with success card
- [ ] Charge endpoint tested with decline card
- [ ] Refund endpoint tested
- [ ] Bill payment endpoint tested
- [ ] Webhook signature validation tested
- [ ] Email notifications working
- [ ] In-app notifications working
- [ ] Transaction history showing payments
- [ ] Error messages helpful and accurate

---

## Troubleshooting

### "Invalid API Key"
- ✅ Check `STRIPE_SECRET_KEY` is set correctly
- ✅ Ensure it starts with `sk_test_`
- ✅ Verify no extra spaces in key

### "Webhook signature verification failed"
- ✅ Check `STRIPE_WEBHOOK_SECRET` is set correctly
- ✅ Ensure it's the **signing secret**, not the API key
- ✅ Signature header uses timestamp-based replay attack prevention

### "Card was declined"
- ✅ Use test cards listed above
- ✅ For 4000-0000-0000-0002, it's designed to always fail
- ✅ Check card expiry and CVC are correct

### "Rate limit exceeded"
- ✅ Implement exponential backoff (built into SDK)
- ✅ Limit to 100 requests/second per account
- ✅ Use idempotency keys to safely retry

### Payment shows as "pending" forever
- ✅ Check webhook endpoint is configured correctly
- ✅ Verify webhook secret is correct
- ✅ Check application logs for webhook processing errors
- ✅ Test webhook delivery in Stripe dashboard

---

## Production Deployment

When moving to production:

1. **Get Live API Keys:**
   - Verify business information in Stripe dashboard
   - Switch from test to live mode
   - Copy live API keys (start with `sk_live_` and `pk_live_`)

2. **Update Configuration:**
   - Set `IsTestMode = false` in appsettings.Production.json
   - Update API keys to live keys
   - Update webhook secret to live signing secret

3. **Security Checklist:**
   - ✅ Never commit API keys to source control
   - ✅ Store keys in Azure Key Vault
   - ✅ Verify webhook signature validation is enabled
   - ✅ Enable rate limiting on payment endpoints
   - ✅ Enable request logging and auditing
   - ✅ Set up PCI-DSS compliance (Stripe handles this)

4. **Monitoring:**
   - Set up alerts for failed payments
   - Monitor webhook processing latency
   - Track refund rates
   - Monitor dispute rates

---

## API Reference

### Request Headers
```
Content-Type: application/json
Authorization: Bearer {JWT_TOKEN}
X-Idempotency-Key: {IDEMPOTENCY_KEY} (optional)
```

### Request/Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Invalid request |
| 401 | Unauthorized (missing/invalid token) |
| 402 | Payment required (card declined) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not found (bill, transaction) |
| 429 | Rate limited (too many requests) |
| 500 | Server error |
| 502 | Bad gateway (Stripe API down) |
| 503 | Service unavailable |

---

## Support

For issues:

1. **Check Stripe Dashboard:** https://dashboard.stripe.com
   - View all payment attempts and webhook deliveries
   - Test webhook events manually

2. **Review Application Logs:**
   - Search for transaction ID in logs
   - Check for error details

3. **Stripe Documentation:**
   - Payment Intents: https://stripe.com/docs/payments/payment-intents
   - Testing: https://stripe.com/docs/testing
   - Webhooks: https://stripe.com/docs/webhooks

---

**Last Updated:** January 2026  
**Version:** 1.0.0  
**Status:** Production Ready
