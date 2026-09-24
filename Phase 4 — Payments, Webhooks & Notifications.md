# Phase 4 — Payments, Webhooks \& Notifications

## Phase Goal

Phase 4 closes the loop between Moyasar and the store: initiating a payment, receiving and safely processing Moyasar's webhook, confirming/failing the Order, finalizing stock deduction for Online payments, and firing the outbound webhook to n8n for WhatsApp notification. This is the highest-risk phase for financial correctness, so idempotency, retries, and rate limiting are treated as first-class requirements rather than afterthoughts, following the exact pattern Moyasar itself recommends (webhook-driven state changes).[^1][^2]

## Features

- Initiate a Moyasar payment for an Online order.
- Receive and process Moyasar webhook (idempotent).
- Finalize stock deduction on payment success; release reservation on failure.
- Outbound webhook to n8n with full order details for WhatsApp notification.
- Automatic retry of outbound webhook on failure (Hangfire).
- Rate limiting on order/payment endpoints.
- Audit log completion (webhook-triggered status changes also recorded).


## Business Rules

**Payment initiation**

- Only applicable to Orders with `PaymentMethod = Online` and `PaymentStatus = Pending`. Attempting to initiate payment on a Cash order → 400 `PAYMENT_NOT_APPLICABLE`. Attempting on an already-paid order → 400 `ORDER_ALREADY_PAID`.
- The API creates a Moyasar payment session server-side (never trusts a client-supplied amount) using the Order's `TotalPrice` in halalas (SAR minor unit, `TotalPrice * 100`), and returns Moyasar's hosted payment fields/redirect data to the client.[^1]
- `Order.PaymentGateway = "Moyasar"`, `Order.PaymentId` stored immediately after Moyasar acknowledges the payment creation request (even before it's confirmed paid), so the webhook can later be matched back to this Order via `PaymentId`.

**Webhook processing (idempotency-first design)**

- Every incoming Moyasar webhook payload includes an event id and payment id; before touching any business logic, the handler attempts an INSERT into a dedicated `ProcessedWebhookEvents` table with a unique constraint on the Moyasar event id. If the insert violates the unique constraint (duplicate), the handler immediately returns 200 OK without re-running any logic — this is the exact pattern recommended for exactly-once webhook handling.[^3][^4][^5]
- Only after the idempotency row is successfully inserted does the handler load the Order by `PaymentId`, verify the webhook's `secret_token` matches the configured Moyasar webhook secret (rejecting mismatches with 401, since Moyasar signs webhooks with a shared secret rather than the amount alone), and then branch by event type: `payment_paid` → success path, `payment_failed` → failure path.[^1]
- Success path: verify `Order.PaymentStatus == Pending` (guards against acting twice even if the idempotency table were somehow bypassed — belt-and-suspenders); call `StockService.DeductStockAsync` for every OrderItem (converts the earlier `ReservedStock` reservation into a real deduction: `QuantityInStock -= qty`, `ReservedStock -= qty`), set `PaymentStatus = Paid`, `OrderStatus = Confirmed`, `PaidAt = now`, write `OrderStatusHistory` entry with `ChangedByUserId = null` (system-triggered) and `Reason = "Moyasar webhook payment_paid"`.
- Failure path: set `PaymentStatus = Failed`, `OrderStatus = Cancelled`, release the `ReservedStock` reservation back (never deducted, so only reservation needs releasing), write `OrderStatusHistory` entry with `Reason = "Moyasar webhook payment_failed"`.
- The entire success/failure branch runs inside one DB transaction so the Order update, stock change, and audit row are atomic — either all commit or all roll back.
- After committing the transaction, the handler enqueues the outbound n8n webhook as a fire-and-forget Hangfire background job — it must never block the response to Moyasar, since Moyasar expects a fast 200 OK and will itself retry if the response is slow or non-2xx.[^1]
- The webhook endpoint always returns 200 OK for any payload that was successfully parsed and matched to an order — even in edge cases like "order already paid" — because returning a non-2xx to Moyasar would trigger unnecessary retries; internal business-rule mismatches are logged via Serilog, not surfaced as HTTP errors to Moyasar.
- If the `PaymentId` in the webhook doesn't match any Order at all (should not normally happen), respond 200 OK anyway (to stop Moyasar retries) but log an Error-level Serilog entry for manual investigation — never throw an unhandled exception back to the gateway.

**Outbound webhook to n8n (store owner WhatsApp notification)**

- Triggered only on successful payment confirmation (`payment_paid` path) — never on failure, never on Cash orders directly here (Cash order creation could optionally also notify, but the explicit business rule ties this specifically to "after successful payment," so Online success is the primary trigger; Cash order creation notification, if desired, would reuse the same job but is out of this phase's strict scope since it's payment-specific).
- Payload sent to the n8n Webhook URL: `OrderNumber`, `CustomerName`, `CustomerPhone`, `Address`, `City`, `Items[]` (name, qty, price), `SubTotal`, `TotalPrice`, `PaymentMethod`, `PaymentStatus`, `PaidAt`.
- Sent via a Hangfire job with `[AutomaticRetry(Attempts = 5)]`, which retries with Hangfire's built-in exponential backoff automatically on unhandled exceptions (e.g. n8n unreachable, timeout, 5xx).[^6][^7][^8]
- If all retry attempts are exhausted, the job moves to Hangfire's `Failed` state (visible in Hangfire Dashboard) rather than disappearing silently — Admin can manually retrigger from the dashboard if n8n was down for an extended period.
- The job itself is idempotent too: it includes the `OrderNumber` as a natural key, so even if manually re-triggered, sending the same WhatsApp notification twice is a business-acceptable duplicate (not a financial error) — no additional dedup table needed here, unlike the incoming Moyasar webhook which affects money/stock.

**Rate limiting**

- Fixed-window rate limiter applied via the built-in `System.Threading.RateLimiting` middleware, partitioned by authenticated CustomerId (or IP for anonymous/public order-related calls):[^9][^10]
    - `POST /api/orders`: max 10 requests per minute per customer.
    - `POST /api/orders/{id}/pay`: max 10 requests per minute per customer.
    - `POST /api/webhooks/moyasar`: no per-caller limit (Moyasar's IPs must never be throttled), but a global ceiling (e.g. 500/min) protects against payload floods/abuse.
- Exceeding the limit returns 429 Too Many Requests with a `Retry-After` header.


## Domain

Entities touched: `Order` (payment fields populated), `Product` (final stock deduction), `OrderStatusHistory` (system-triggered entries).

New entity: `ProcessedWebhookEvent`

```
ProcessedWebhookEvent
- Id
- MoyasarEventId (unique)
- PaymentId
- EventType
- ProcessedAt
```

No new enums — `PaymentStatus`/`OrderStatus` enums already cover all needed states from Phase 3.

## Database

Migrations required:

- New table `ProcessedWebhookEvents`.
- Unique index on `ProcessedWebhookEvents.MoyasarEventId` — the core idempotency guarantee, enforced at the DB level, not just application-level checks, exactly matching the recommended unique-constraint pattern.[^5][^3]
- Index on `Orders.PaymentId` (fast webhook-to-order lookup).


## Services

- **PaymentService**: `InitiatePaymentAsync(orderId)` — builds and sends the Moyasar payment creation request, stores `PaymentId` on the Order, returns client-facing payment fields/redirect URL.
- **MoyasarWebhookService**: `ProcessWebhookAsync(payload)` — owns the entire idempotency-check → secret-token verification → event-type branching → transaction logic described above.
- **StockService** (extended from Phase 3): confirms `DeductStockAsync` is called from the webhook success path, reusing the same row-locking mechanism.
- **OutboundNotificationService**: `SendOrderNotificationAsync(orderId)` — builds the payload and posts it to the configured n8n Webhook URL via `HttpClient`; this is the method Hangfire enqueues and retries.
- **OrderExpiryJob** (Phase 3, unchanged) continues to run and now correctly interacts with Phase 4 by ensuring any order it auto-cancels is never in a state Phase 4 could still try to confirm (guarded by the `PaymentStatus == Pending` check before webhook processing acts).


## Repository Methods

**IOrderRepository (extended)**

- `GetByPaymentIdAsync(string paymentId)`
- `UpdatePaymentDetailsAsync(Guid orderId, string paymentId, string paymentGateway)`

**IProcessedWebhookEventRepository**

- `ExistsAsync(string moyasarEventId)`
- `AddAsync(ProcessedWebhookEvent entry)` (relies on unique constraint to throw on duplicate insert attempts under race conditions)


## DTOs

**Request DTOs**

- `InitiatePaymentRequest`: (route param `orderId` only — no body needed, amount is derived server-side from the Order).
- `MoyasarWebhookPayload`: `Id` (event id), `Type` (string, e.g. "payment_paid"/"payment_failed"), `CreatedAt`, `SecretToken`, `Data` (nested object: `Id` as PaymentId, `Status`, `Amount`, `InvoiceId`).

**Response DTOs**

- `PaymentInitiationResponse`: `PaymentId`, `RedirectUrl` (or `PublishableApiKey` + form fields if using Moyasar.js hosted fields instead of redirect), `Amount`, `Currency`.
- `WebhookAckResponse`: simple `{ received: true }` — always 200, minimal body.


## Validation

- `InitiatePaymentRequest` (implicit via route): Order must exist, belong to caller, `PaymentMethod == Online`, `PaymentStatus == Pending`.
- `MoyasarWebhookPayload`: `SecretToken` must exactly match configured secret (constant-time string comparison to avoid timing attacks); `Type` must be a recognized event type, unrecognized types are logged and acknowledged with 200 but otherwise ignored.


## API Endpoints

### Payments

**POST /api/orders/{orderId}/pay**

- Authorization: Customer (must own the order)
- Response: 200 OK, `PaymentInitiationResponse`
- Errors: 400 `PAYMENT_NOT_APPLICABLE`, 400 `ORDER_ALREADY_PAID`, 404 `ORDER_NOT_FOUND`, 403 `NOT_YOUR_ORDER`, 429 (rate limited)
- Example Response:

```json
{ "paymentId": "pay_2a1b...", "redirectUrl": "https://checkout.moyasar.com/...", "amount": 429900, "currency": "SAR" }
```

**POST /api/webhooks/moyasar**

- Authorization: none (public endpoint, secured instead via `SecretToken` verification inside the payload per Moyasar's model)[^1]
- Request Body: `MoyasarWebhookPayload`
- Response: 200 OK, `WebhookAckResponse` — always, per the "never return non-2xx to Moyasar" rule
- Errors: 401 only for secret-token mismatch (Moyasar itself won't retry on true auth failure since it indicates misconfiguration, not a transient error)
- Example Request:

```json
{ "id": "evt_8f2c1d4e", "type": "payment_paid", "created_at": "2026-07-22T18:00:00Z", "secret_token": "your-webhook-secret", "data": { "id": "pay_2a1b...", "status": "paid", "amount": 429900 } }
```

- Example Response:

```json
{ "received": true }
```


### Admin — Webhook Monitoring (operational visibility)

**GET /api/admin/webhook-events**

- Authorization: Admin
- Query: `page`, `pageSize`, `eventType`
- Response: 200 OK, `PagedResponse<ProcessedWebhookEventResponse>` — lets Admin audit incoming Moyasar events for support/debugging.


## Pagination

Same standard pattern applied to `GET /api/admin/webhook-events`.

## Searching

Not applicable beyond the `eventType` filter above.

## Filtering

- `eventType` filter on the webhook events audit list (`payment_paid` / `payment_failed`).


## Sorting

Webhook events list always sorted by `ProcessedAt` descending (most recent first) — no client-controlled sorting needed for an audit view.

## Security

- `/api/webhooks/moyasar` is intentionally `[AllowAnonymous]` at the ASP.NET Core authorization level (Moyasar cannot supply a JWT), but is fully secured via the `secret_token` payload verification, which must never be logged in plaintext by Serilog (mask it in logging enrichers).[^1]
- `/api/orders/{orderId}/pay` requires Customer role + ownership check identical to Phase 3's order-access pattern.
- Rate limiting middleware registered globally in `Program.cs`, with named policies applied per-endpoint via `[EnableRateLimiting("orders-policy")]` attributes.[^10][^11]
- The idempotency unique constraint on `ProcessedWebhookEvents.MoyasarEventId` is the last line of defense even under multi-instance/horizontally-scaled deployment — since it's DB-enforced, not in-memory, it remains correct even with multiple API instances receiving the same webhook simultaneously.[^3]


## Error Handling

| Error | HTTP Status |
| :-- | :-- |
| Payment not applicable (Cash order or wrong state) | 400 |
| Order already paid | 400 |
| Order not found | 404 |
| Not the order owner | 403 |
| Webhook secret token mismatch | 401 |
| Duplicate webhook event (idempotency hit) | 200 (silently acknowledged) |
| Unrecognized webhook event type | 200 (acknowledged, logged, ignored) |
| Order not found for given PaymentId in webhook | 200 (acknowledged, logged as Error for investigation) |
| Rate limit exceeded | 429 |
| Outbound n8n webhook failure (all retries exhausted) | N/A — internal Hangfire Failed state, not exposed via API |
| Unhandled exception | 500 |

## Testing Checklist

**Payment Initiation**

- Initiate payment for a valid Pending Online order → 200, PaymentId stored on Order.
- Initiate payment for a Cash order → 400.
- Initiate payment for an already-Paid order → 400.
- Initiate payment for another customer's order → 403.
- Exceed rate limit (11th request in a minute) → 429.

**Webhook — Success Path**

- Send valid `payment_paid` webhook for a Pending Online order → Order becomes Paid/Confirmed, stock deducted, `ReservedStock` reduced, `OrderStatusHistory` entry created.
- Resend the exact same webhook event id a second time → 200 OK, no duplicate stock deduction, no duplicate status history row (idempotency verified).
- Send `payment_paid` for an order already marked Paid (simulating a race/duplicate without matching event id, edge case) → guarded by internal state check, no double deduction, still 200.

**Webhook — Failure Path**

- Send `payment_failed` webhook → Order becomes Failed/Cancelled, reservation released, stock restored to available pool, history logged.
- Confirm a failed-then-retried payment (new payment attempt, new PaymentId) can still succeed independently.

**Webhook — Security \& Edge Cases**

- Send webhook with wrong `secret_token` → 401, no state change.
- Send webhook with unknown `PaymentId` → 200 OK, Error logged, no crash.
- Send webhook with unrecognized event `Type` → 200 OK, ignored, logged.
- Simulate concurrent duplicate webhook deliveries hitting the API at the exact same millisecond → unique constraint ensures only one succeeds in processing, verified via DB row count = 1 in `ProcessedWebhookEvents`.

**Outbound n8n Notification**

- Successful payment triggers Hangfire job enqueue → n8n receives correct payload matching Order data.
- n8n endpoint simulated as down → Hangfire retries with increasing delay, verified via Hangfire Dashboard/job history.
- After exhausting retries, job lands in Failed state, manually retriggered → succeeds.
- Cash order payment (no online payment) does not trigger this notification path (out of Phase 4's payment-success trigger scope).

**Rate Limiting**

- Burst of requests to `/api/orders` beyond threshold → 429 with `Retry-After` header present.
- Moyasar webhook endpoint under high volume stays within the configured global ceiling without throttling legitimate gateway traffic under normal load.


