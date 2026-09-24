
# Phase 3 — Order Placement \& Stock Reservation

## Phase Goal

Phase 3 converts a Cart into a real, immutable Order — the most business-critical and concurrency-sensitive phase of the entire system. This is where money, stock, and customer trust are all on the line simultaneously, so every rule from Phases 1–2 must now be enforced atomically under real concurrent load. This phase deliberately stops right before payment confirmation (Phase 4) — it only handles Order creation, stock reservation/deduction, Order Number generation, and Admin order management, keeping Moyasar webhook processing isolated to the next phase for cleaner separation of concerns.[^1]

## Features

- Customer: Create Order from current Cart (Cash or Online payment method selection).
- Customer: View own Order history and Order details.
- Customer: Cancel own Order (only before it ships — but only Admin can execute cancellation per business rule, so this becomes a "Request Cancellation" concept handled by Admin).
- Admin: View all Orders with filters, search, pagination.
- Admin: View single Order details.
- Admin: Update Order status (Pending → Confirmed → Processing → Shipped → Delivered).
- Admin: Cancel Order (with automatic stock restoration).
- Order Number generation via Redis atomic counter, collision-free even under concurrent load.
- Stock deduction/reservation logic branching by PaymentMethod (Cash vs Online).
- Race-condition-safe stock handling using row-level locking.


## Business Rules

**Order creation (shared for both payment methods)**

- Order can only be created from a non-empty Cart belonging to the authenticated Customer. Empty cart → 400 `CART_EMPTY`.
- Every CartItem is re-validated at order-creation time (not just at add-to-cart time): product must be `IsActive = true` and have enough stock. If any item fails validation, the entire order creation is rejected — no partial orders — with a response listing exactly which items failed and why (`INSUFFICIENT_STOCK` / `PRODUCT_UNAVAILABLE`), so the client can adjust the cart and retry.
- `OrderItem` records are immutable snapshots: `ProductName`, `UnitPrice`, `Quantity`, `TotalPrice` are copied from the Product/Cart at the exact moment of order creation and never change afterward, even if the Product's price later changes.
- `SubTotal` = sum of all `OrderItem.TotalPrice`. Since no discounts or coupons exist, `TotalPrice` (Order) = `SubTotal` exactly — enforced in code as a direct assignment, not a separate calculation, removing any possibility of drift.
- City must be selected from the fixed Saudi Arabia cities list (validated against a static enum/lookup table, not free text).
- On successful Order creation, the Cart is fully cleared (all CartItems removed) — the Cart row itself persists empty for future use, consistent with Phase 2 rules.
- `OrderNumber` is generated using an atomic Redis `INCR` on a per-day key (e.g. `order_counter:20260722`), with the key's TTL set to expire at midnight so the counter naturally restarts at 1 the next day — this guarantees no two orders ever receive the same number even under simultaneous requests, since Redis INCR is atomic by design. Format: `ORD-{yyyyMMdd}-{counter:D4}`.[^2][^3]
- If Redis is unreachable at order-creation time, the request must fail with 503 `ORDER_NUMBER_SERVICE_UNAVAILABLE` rather than falling back silently to a non-unique method — correctness here is non-negotiable since duplicate order numbers would corrupt reporting and customer trust.

**Stock handling — Cash orders**

- On Cash order creation, stock is deducted immediately and atomically within the same DB transaction as the Order insert, using a pessimistic row lock (`SELECT ... FOR UPDATE` via raw SQL executed through EF Core, since EF has no native LINQ support for row-level locking) to serialize concurrent attempts to buy the same product.[^4][^5][^1]
- If, after acquiring the lock, the stock turns out to be insufficient (another concurrent request beat this one to the last units), the entire transaction is rolled back and the client receives 409 `INSUFFICIENT_STOCK` — this is the core overselling-prevention mechanism.[^6]
- `OrderStatus` starts at `Pending` even for Cash, matching the required flow; stock is deducted at creation time as specified, and the Admin later moves the order to `Confirmed` manually to indicate the shop has physically prepared it — deduction and confirmation are two separate steps, both starting the moment the Cash order is created (per the confirmed business rule: "Stock decreases when the order becomes Confirmed" — clarified below).
- Correction per business rule as literally stated: for Cash, stock decreases when order becomes Confirmed, not at raw creation. So Cash order creation reserves stock (soft-lock, same mechanism as Online) and actual deduction is deferred to the `Pending → Confirmed` transition, executed by Admin in this phase (fully within Phase 3 scope, no payment gateway involvement needed for Cash).

**Stock handling — Online orders**

- On Online order creation, no stock is deducted yet — a stock **reservation** is placed instead (increment `Product.ReservedStock`, not `QuantityInStock`), inside the same row-locked transaction, so that available stock (`QuantityInStock - ReservedStock`) correctly reflects units already claimed by pending payments.
- `OrderStatus = Pending`, `PaymentStatus = Pending` at creation.
- Actual stock deduction (`QuantityInStock -= Quantity`, `ReservedStock -= Quantity`) only happens in Phase 4 when Moyasar's webhook confirms payment success — Phase 3 only creates the reservation and exposes the mechanism (`ProductService.ReserveStockAsync` / `ReleaseReservationAsync`) that Phase 4 will call.
- A reservation has a expiry window (e.g. 30 minutes) tracked via `Order.CreatedAt` + a configurable timeout; a Hangfire recurring job (running every few minutes) scans for Online orders still `Pending` past the expiry and automatically cancels them, releasing their reserved stock back — this is the concrete mechanism that prevents phantom reservations from permanently locking stock if a customer abandons payment.

**Order status transitions**

- Allowed forward path only: `Pending → Confirmed → Processing → Shipped → Delivered`. Any attempt to skip a step (e.g. `Pending → Shipped`) or move backward is rejected with 400 `INVALID_STATUS_TRANSITION`.
- `Cancelled` is reachable from `Pending`, `Confirmed`, or `Processing` only — never from `Shipped` or `Delivered` (physical goods already left the shop). Attempting to cancel a `Shipped`/`Delivered` order returns 400 `CANNOT_CANCEL_SHIPPED_ORDER`.
- Only Admin can transition an order to `Cancelled`, per business rule — there is no customer-facing cancel endpoint in this phase; a customer wanting to cancel must contact support, who (as Admin) executes the cancellation.
- Cancelling an order always restores stock: if it was Cash (already deducted), `QuantityInStock` is restored; if it was Online and still `Pending`/unpaid, the `ReservedStock` reservation is released; if it was Online and already `Paid` (deducted in Phase 4), `QuantityInStock` is restored exactly like Cash.
- Every status transition — regardless of direction — is written to an audit trail (`OrderStatusHistory`, actual table introduced now since this is where transitions first happen), fulfilling the traceability need even though full detail was scoped for Phase 4 originally; it belongs here since transitions start in Phase 3.


## Domain

Entities touched: `Order`, `OrderItem`, `Product` (adds `ReservedStock`), `Cart`/`CartItem` (consumed/cleared).

New entity: `OrderStatusHistory`

```
OrderStatusHistory
- Id
- OrderId (FK)
- PreviousStatus
- NewStatus
- ChangedByUserId
- ChangedAt
- Reason (nullable string, e.g. "Auto-expired reservation")
```

Domain changes required:

- `Product`: add `ReservedStock` (int, default 0). Available stock is always computed as `QuantityInStock - ReservedStock`, never persisted as a separate column to avoid drift.
- `Order`: `PaymentMethod` becomes a proper enum (`Cash = 0`, `Online = 1`) rather than free string, matching the enums already defined for `PaymentStatus`/`OrderStatus`.
- New enum `SaudiCity` (fixed list: Riyadh, Jeddah, Mecca, Medina, Dammam, Khobar, Taif, Buraidah, Tabuk, Abha, Khamis Mushait, Hail, Najran, Jubail, Yanbu, etc.) stored as int on `Order.City`.


## Database

Migrations required:

- Add `ReservedStock` column to `Products` (default 0).
- Change `Order.PaymentMethod` and `Order.City` columns to integer-backed enums (migration to convert existing string columns if any existed from scaffolding).
- New table `OrderStatusHistory`.

Indexes \& Constraints:

- Unique index on `Orders.OrderNumber`.
- Index on `Orders.CustomerId` (fast lookup of a customer's order history).
- Index on `Orders (OrderStatus, PaymentMethod, CreatedAt)` — supports the Hangfire expiry-scan query efficiently (`WHERE PaymentMethod = Online AND OrderStatus = Pending AND CreatedAt < @cutoff`).
- Foreign key `OrderItems.OrderId → Orders.Id`, `ON DELETE CASCADE`.
- Foreign key `OrderItems.ProductId → Products.Id`, `ON DELETE RESTRICT`.
- Foreign key `OrderStatusHistory.OrderId → Orders.Id`, `ON DELETE CASCADE`.
- Check constraint on `Products`: `ReservedStock >= 0 AND ReservedStock <= QuantityInStock`.


## Services

- **OrderService**: Orchestrates `CreateOrderAsync` (validates cart, locks products, generates order number, creates Order+OrderItems, deducts/reserves stock per payment method, clears cart, all within one DB transaction), `GetOrderByIdAsync`, `GetCustomerOrdersAsync`, `UpdateOrderStatusAsync` (validates transition rules, writes audit history), `CancelOrderAsync` (restores stock accordingly), `GetAllOrdersForAdminAsync` (search/filter/pagination).
- **OrderNumberGeneratorService**: Wraps Redis client — `GenerateAsync()` returns the formatted `ORD-yyyyMMdd-0001` string using per-day key + TTL-to-midnight logic.[^2]
- **StockService**: Isolated concurrency-safe stock operations — `TryReserveStockAsync(productId, qty)`, `DeductStockAsync(productId, qty)`, `ReleaseReservationAsync(productId, qty)`, `RestoreStockAsync(productId, qty)` — all implemented using row-level locking transactions so `OrderService` never touches raw SQL directly.[^1][^4]
- **OrderExpiryJob** (Hangfire recurring job, e.g. every 5 minutes): finds expired Pending Online orders, calls `OrderService.CancelOrderAsync` with reason "Reservation expired", releasing reserved stock.


## Repository Methods

**IOrderRepository**

- `GetByIdAsync(Guid id, bool includeItems = true)`
- `GetByOrderNumberAsync(string orderNumber)`
- `GetByCustomerIdAsync(Guid customerId, PagedRequest request)`
- `SearchForAdminAsync(OrderSearchFilter filter)` — supports status, paymentMethod, date range, customer name/phone keyword
- `AddStatusHistoryAsync(OrderStatusHistory entry)`
- `GetExpiredPendingOnlineOrdersAsync(DateTime cutoff)`

**IProductRepository (extended from Phase 1)**

- `GetByIdForUpdateAsync(Guid id)` — issues raw SQL `SELECT ... FOR UPDATE` inside an open transaction, returning the locked row for safe mutation.
- `UpdateStockAsync(Guid id, int quantityDelta, int reservedDelta)`


## DTOs

**Request DTOs**

- `CreateOrderRequest`: `CustomerName`, `CustomerPhone`, `Address`, `City` (enum), `Notes` (optional), `PaymentMethod` (enum: Cash/Online).
- `UpdateOrderStatusRequest`: `NewStatus` (enum), `Reason` (optional string, used mainly for Cancelled).
- `AdminOrderSearchRequest`: `Status` (enum?), `PaymentMethod` (enum?), `DateFrom`, `DateTo`, `Keyword` (matches CustomerName/CustomerPhone/OrderNumber), `Page`, `PageSize`, `SortBy` (CreatedAt/TotalPrice), `SortDirection`.

**Response DTOs**

- `OrderResponse`: `Id`, `OrderNumber`, `CustomerName`, `CustomerPhone`, `Address`, `City`, `PaymentMethod`, `PaymentStatus`, `OrderStatus`, `SubTotal`, `TotalPrice`, `Items` (List of `OrderItemResponse`), `CreatedAt`, `UpdatedAt`.
- `OrderItemResponse`: `ProductId`, `ProductName`, `UnitPrice`, `Quantity`, `TotalPrice`.
- `OrderSummaryResponse` (for admin lists): `Id`, `OrderNumber`, `CustomerName`, `OrderStatus`, `PaymentStatus`, `TotalPrice`, `CreatedAt`.
- `OrderStatusHistoryResponse`: `PreviousStatus`, `NewStatus`, `ChangedBy`, `ChangedAt`, `Reason`.


## Validation

- `CreateOrderRequest`: `CustomerName` NotEmpty; `CustomerPhone` matches Saudi format; `Address` NotEmpty, MaxLength(300); `City` must be a valid `SaudiCity` enum value; `PaymentMethod` required, must be Cash or Online.
- `UpdateOrderStatusRequest`: `NewStatus` must be a valid `OrderStatus` enum value; if `NewStatus = Cancelled`, `Reason` becomes required.
- `AdminOrderSearchRequest`: `DateFrom` <= `DateTo` when both provided; `PageSize` <= 100.


## API Endpoints

### Customer — Orders

**POST /api/orders**

- Authorization: Customer
- Request Body: `CreateOrderRequest`
- Response: 201 Created, `OrderResponse`
- Errors: 400 `CART_EMPTY`, 409 `INSUFFICIENT_STOCK` (with list of failing items), 400 `PRODUCT_UNAVAILABLE`, 503 `ORDER_NUMBER_SERVICE_UNAVAILABLE`
- Example Request:

```json
{ "customerName": "Ahmed Ali", "customerPhone": "0512345678", "address": "Al Olaya St, Building 4", "city": "Riyadh", "paymentMethod": "Online" }
```

- Example Response:

```json
{ "orderNumber": "ORD-20260722-0007", "orderStatus": "Pending", "paymentStatus": "Pending", "subTotal": 4299.00, "totalPrice": 4299.00, "items": [ { "productName": "iPhone 15", "unitPrice": 4299.00, "quantity": 1, "totalPrice": 4299.00 } ] }
```

**GET /api/orders**

- Authorization: Customer
- Query: `page`, `pageSize`
- Response: 200 OK, `PagedResponse<OrderSummaryResponse>` (only the authenticated customer's own orders)

**GET /api/orders/{id}**

- Authorization: Customer (must own the order)
- Response: 200 OK, `OrderResponse`
- Errors: 404 `ORDER_NOT_FOUND`, 403 `NOT_YOUR_ORDER`


### Admin — Orders

**GET /api/admin/orders**

- Authorization: Admin
- Query: `status`, `paymentMethod`, `dateFrom`, `dateTo`, `keyword`, `page`, `pageSize`, `sortBy`, `sortDirection`
- Response: 200 OK, `PagedResponse<OrderSummaryResponse>`

**GET /api/admin/orders/{id}**

- Authorization: Admin
- Response: 200 OK, `OrderResponse` (includes `StatusHistory` list)
- Errors: 404 `ORDER_NOT_FOUND`

**PUT /api/admin/orders/{id}/status**

- Authorization: Admin
- Request Body: `UpdateOrderStatusRequest`
- Response: 200 OK, `OrderResponse`
- Errors: 400 `INVALID_STATUS_TRANSITION`, 400 `CANNOT_CANCEL_SHIPPED_ORDER`, 404 `ORDER_NOT_FOUND`
- Example Request:

```json
{ "newStatus": "Cancelled", "reason": "Customer requested cancellation by phone" }
```


## Pagination

Same standard pattern as Phase 1 (`Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasNext`, `HasPrevious`), applied to both customer order history and admin order list.

## Searching

Admin order search matches `Keyword` against `OrderNumber` (exact/partial), `CustomerName` (`ILIKE`), and `CustomerPhone` (partial match) in a single combined `OR` clause.

## Filtering

- `Status`: exact match on `OrderStatus` enum.
- `PaymentMethod`: exact match on `PaymentMethod` enum.
- `DateFrom`/`DateTo`: filters `Orders.CreatedAt` range, inclusive on both ends.


## Sorting

Allowed columns: `CreatedAt` (default, Newest first), `TotalPrice`. `SortDirection` asc/desc supported on both.

## Security

- `/api/orders/*` (customer) requires `[Authorize(Roles = "Customer")]` plus an ownership check in the service layer (`order.CustomerId == currentUserId`) — never trust the route alone.
- `/api/admin/orders/*` requires `[Authorize(Roles = "Admin")]`.
- Row-level locking transactions must have a strict timeout (e.g. 5 seconds) to avoid deadlocks freezing the whole checkout pipeline under load.[^7][^8]
- Redis connection for Order Number generation should use a resilient client with retry policy, but never retry indefinitely — must fail fast and surface 503 rather than hang the checkout request.


## Error Handling

| Error | HTTP Status |
| :-- | :-- |
| Cart empty | 400 |
| Product unavailable/inactive in cart at checkout | 400 |
| Insufficient stock (lost the race) | 409 |
| Order number service (Redis) unavailable | 503 |
| Order not found | 404 |
| Not the order owner | 403 |
| Invalid status transition | 400 |
| Cancel attempted on shipped/delivered order | 400 |
| Unauthorized | 401 |
| Forbidden (wrong role) | 403 |
| Unhandled exception | 500 |

## Testing Checklist

**Order Creation**

- Create Cash order with valid cart → 201, stock reserved, cart cleared.
- Create Online order with valid cart → 201, `ReservedStock` incremented, `QuantityInStock` unchanged, cart cleared.
- Create order with empty cart → 400.
- Create order where one cart item became inactive since being added → 400, offending item listed.
- Create order where stock dropped below cart quantity since being added → 409.
- Simulate 50 concurrent order requests for the last 1 unit of a product → exactly one succeeds, 49 receive 409 (race condition test).
- Verify OrderNumber sequence resets to 0001 the day after — test with mocked system clock across midnight boundary.
- Verify two orders created in the same millisecond receive different sequential OrderNumbers.
- Redis unavailable simulated → order creation returns 503, no Order row persisted (transaction rolled back).

**Status Transitions**

- Pending → Confirmed → Processing → Shipped → Delivered, each step in order → all succeed, history logged each time.
- Pending → Shipped directly → 400 `INVALID_STATUS_TRANSITION`.
- Delivered → Processing (backward) → 400.
- Cancel from Pending → 200, stock/reservation restored.
- Cancel from Confirmed (Cash, stock already deducted) → 200, stock restored.
- Cancel from Shipped → 400 `CANNOT_CANCEL_SHIPPED_ORDER`.
- Cancel attempted by Customer role → 403 (endpoint itself is Admin-only).
- Every transition produces a new `OrderStatusHistory` row with correct Previous/New status and ChangedByUserId.

**Reservation Expiry**

- Create Online order, artificially age its CreatedAt past the expiry window, run the Hangfire job → order auto-cancelled, reservation released.
- Confirm a still-fresh (not expired) Online Pending order is untouched by the job.

**Customer Order Access**

- Customer views own order → 200.
- Customer attempts to view another customer's order by guessing the ID → 403.
- Customer order list pagination works correctly with more than one page of orders.
