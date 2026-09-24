
# Phase 2 — Customer Identity \& Cart

## Phase Goal

Phase 2 builds the customer-facing authentication layer (registration, login, Google sign-in) and the authenticated Cart system, both of which every future Order depends on. For an SPA/mobile client, the correct pattern is to let the API itself remain the single JWT issuer — even when the user authenticates via Google, the API validates the Google ID token server-side and then issues its own JWT so the rest of the system never needs to know how the user logged in. The Cart is scoped strictly to a registered user (one cart per user), which is the simplest and most consistent model for this store since Phase 1 established no guest browsing restrictions on catalog but the business rule requires login for cart.[^1][^2][^3]

## Features

- Customer registration with email/password.
- Customer login with email/password (JWT issuance).
- Customer login with Google (ID token verification → JWT issuance).
- Get current authenticated customer profile.
- Add item to cart.
- Update cart item quantity.
- Remove item from cart.
- View current cart (with live price/stock recalculation).
- Clear entire cart.
- Automatic stock/price validation on every cart read (no stale data shown to customer).


## Business Rules

**Registration / Login**

- Email must be unique across all Identity users (Admin and Customer share the same `AspNetUsers` table, differentiated by Role claim).
- Password policy inherited from existing Identity configuration (already set up in Infrastructure — not re-defined here).
- On successful email/password login, return JWT with claims: `sub` (UserId), `email`, `role` = "Customer", `name`.
- Google login flow: client sends the Google ID token (obtained client-side via Google Sign-In SDK) to the API; API verifies the token's signature and audience against Google's public keys, extracts `email` and `name` claims.[^2]
- If a user with that email already exists (registered via password), and they log in via Google with the same email, the existing account is linked (an `ExternalLogins` row is added) rather than creating a duplicate user — this prevents duplicate customer profiles for one person.[^4]
- If no user exists, a new Customer user is created automatically with `EmailConfirmed = true` (Google already verified the email).[^3]
- Google login never sets a local password — `PasswordHash` remains null; such accounts can only ever log in via Google unless they explicitly set a password later (out of scope for Phase 2).
- Invalid/expired Google ID token → 401 with `INVALID_GOOGLE_TOKEN`.

**Cart Rules**

- Cart is created lazily — the first time a customer adds an item, if no Cart row exists for that `CustomerId`, one is created automatically.
- Exactly one Cart per Customer, enforced by a unique constraint on `Carts.CustomerId`.
- Adding a product already in the cart increments its quantity rather than creating a duplicate `CartItem` row.
- Adding a product with `IsActive = false` (soft-deleted or deactivated) → 400 `PRODUCT_UNAVAILABLE`.
- Adding a product with `QuantityInStock = 0` → 400 `PRODUCT_OUT_OF_STOCK` (cart never accepts an out-of-stock item, unlike the catalog which still displays it).
- Adding/updating a quantity that would exceed `QuantityInStock` → 400 `INSUFFICIENT_STOCK`, response includes the actual available stock so the client can adjust.
- Cart does NOT reserve stock in Phase 2 — that belongs to Phase 3/4 (Order creation). The cart is purely a "wish list with live validation," re-checked against current stock every time it's read or an order is placed.
- Every time the cart is fetched (`GET /api/cart`), each `CartItem` is re-validated: if the product became inactive or stock dropped below the cart quantity since it was added, the response flags that line item (`IsAvailable = false`, `AvailableStock = X`) instead of silently removing it — the customer decides whether to adjust or remove.
- `CartItem.UnitPrice` is NOT stored — cart always reflects the live `Product.Price` at read time (no snapshot), because unlike an Order, a cart is not a binding transaction.
- Removing the last item from a cart does not delete the Cart row itself — the Cart persists empty for that customer, ready for reuse.
- Quantity must be a positive integer (>= 1); `UpdateCartItem` with quantity 0 is treated as a removal (equivalent to DELETE), not an error.


## Domain

Entities touched: `Cart` (new), `CartItem` (new). Identity: `ApplicationUser` (existing, extended with claims/roles, no schema change needed since Identity already supports external logins natively via `AspNetUserLogins`).

New entities required:

```
Cart
- Id
- CustomerId (FK to AspNetUsers.Id)
- CreatedAt
- UpdatedAt

CartItem
- Id
- CartId (FK)
- ProductId (FK)
- Quantity
- CreatedAt
- UpdatedAt
```

No new enums required in this phase.

## Database

Migrations required:

- New tables `Carts` and `CartItems`.
- Unique index on `Carts.CustomerId` (enforces one cart per customer).
- Unique composite index on `CartItems (CartId, ProductId)` — prevents duplicate rows for the same product in one cart, backing the "increment instead of duplicate" business rule at the DB level as a safety net.
- Foreign key `Carts.CustomerId → AspNetUsers.Id`, `ON DELETE CASCADE` (deleting a user removes their cart).
- Foreign key `CartItems.CartId → Carts.Id`, `ON DELETE CASCADE`.
- Foreign key `CartItems.ProductId → Products.Id`, `ON DELETE RESTRICT` (never allow a product hard-delete to silently orphan cart items — though Products are soft-deleted anyway per Phase 1).


## Services

- **AuthService**: Handles `RegisterAsync`, `LoginAsync` (email/password), `GoogleLoginAsync` (verifies Google token via `GoogleTokenValidationService`, finds-or-creates user, links external login), `GenerateJwtAsync(ApplicationUser user)` (shared token-issuance logic used by both password and Google flows).
- **GoogleTokenValidationService**: Wraps `Google.Apis.Auth` `GoogleJsonWebSignature.ValidateAsync()`, returns verified payload (email, name, subject id) or throws a specific exception caught by AuthService.
- **CartService**: `GetCartAsync(customerId)` (with live validation logic), `AddItemAsync`, `UpdateItemQuantityAsync`, `RemoveItemAsync`, `ClearCartAsync`. Owns all business rule enforcement described above (stock checks, active checks, increment-not-duplicate).


## Repository Methods

**ICartRepository**

- `GetByCustomerIdAsync(Guid customerId, bool includeItems = true)`
- `CreateAsync(Cart cart)`
- `GetItemAsync(Guid cartId, Guid productId)`
- `AddItemAsync(CartItem item)`
- `RemoveItemAsync(Guid cartItemId)`
- `ClearItemsAsync(Guid cartId)`

**IUserRepository / extends Identity's UserManager (no custom repository needed — UserManager<ApplicationUser> already covers this)**

- `FindByEmailAsync(string email)` (built-in)
- `AddLoginAsync(user, UserLoginInfo)` (built-in, used to link Google external login)
- `FindByLoginAsync(string provider, string providerKey)` (built-in)


## DTOs

**Request DTOs**

- `RegisterRequest`: `FullName` (string, required), `Email` (string, required), `Password` (string, required), `Phone` (string, required).
- `LoginRequest`: `Email` (string, required), `Password` (string, required).
- `GoogleLoginRequest`: `IdToken` (string, required — the raw token from Google Sign-In SDK on client).
- `AddCartItemRequest`: `ProductId` (Guid, required), `Quantity` (int, required, >=1).
- `UpdateCartItemRequest`: `Quantity` (int, required, >=0).

**Response DTOs**

- `AuthResponse`: `Token` (string, JWT), `ExpiresAt` (DateTime), `CustomerId` (Guid), `FullName`, `Email`.
- `CartResponse`: `CartId`, `Items` (List of `CartItemResponse`), `SubTotal` (decimal, computed sum of available items only), `TotalItemsCount`, `HasUnavailableItems` (bool).
- `CartItemResponse`: `CartItemId`, `ProductId`, `ProductName`, `ProductSlug`, `MainImageUrl`, `UnitPrice` (live price), `Quantity`, `LineTotal`, `IsAvailable` (bool), `AvailableStock` (int, only populated if `IsAvailable = false`).


## Validation

- `RegisterRequest`: `Email` valid email format, unique check against `AspNetUsers`; `Password` min length 8, requires uppercase/lowercase/digit per existing Identity policy; `Phone` matches Saudi phone regex `^(05|5)[0-9]{8}$`.
- `LoginRequest`: `Email` NotEmpty, valid format; `Password` NotEmpty.
- `GoogleLoginRequest`: `IdToken` NotEmpty.
- `AddCartItemRequest`: `ProductId` not empty Guid; `Quantity` between 1 and 100 (sane upper bound to prevent abuse).
- `UpdateCartItemRequest`: `Quantity` between 0 and 100.


## API Endpoints

### Authentication

**POST /api/auth/register**

- Authorization: none
- Request Body: `RegisterRequest`
- Response: 201 Created, `AuthResponse`
- Errors: 400 `VALIDATION_ERROR`, 409 `EMAIL_ALREADY_EXISTS`
- Example Request:

```json
{ "fullName": "Ahmed Ali", "email": "ahmed@example.com", "password": "P@ssw0rd1", "phone": "0512345678" }
```

- Example Response:

```json
{ "token": "eyJhbGci...", "expiresAt": "2026-07-22T08:00:00Z", "customerId": "d3f...", "fullName": "Ahmed Ali", "email": "ahmed@example.com" }
```

**POST /api/auth/login**

- Authorization: none
- Request Body: `LoginRequest`
- Response: 200 OK, `AuthResponse`
- Errors: 401 `INVALID_CREDENTIALS`, 403 `ACCOUNT_LOCKED` (if lockout policy triggered by Identity)

**POST /api/auth/google**

- Authorization: none
- Request Body: `GoogleLoginRequest`
- Response: 200 OK, `AuthResponse`
- Errors: 401 `INVALID_GOOGLE_TOKEN`
- Example Request:

```json
{ "idToken": "eyJhbGciOiJSUzI1NiIsImtpZ..." }
```

- Example Response:

```json
{ "token": "eyJhbGci...", "expiresAt": "2026-07-22T08:00:00Z", "customerId": "d3f...", "fullName": "Ahmed Ali", "email": "ahmed@gmail.com" }
```

**GET /api/auth/me**

- Authorization: `[Authorize(Roles = "Customer")]`
- Response: 200 OK, `{ customerId, fullName, email, phone }`
- Errors: 401 (missing/invalid token)


### Cart

**GET /api/cart**

- Authorization: Customer
- Response: 200 OK, `CartResponse` (empty items array if no cart yet — does not error)
- Errors: 401

**POST /api/cart/items**

- Authorization: Customer
- Request Body: `AddCartItemRequest`
- Response: 200 OK, `CartResponse` (full updated cart)
- Errors: 400 `PRODUCT_UNAVAILABLE`, 400 `PRODUCT_OUT_OF_STOCK`, 400 `INSUFFICIENT_STOCK`, 404 `PRODUCT_NOT_FOUND`
- Example Request:

```json
{ "productId": "c2f...", "quantity": 2 }
```

**PUT /api/cart/items/{cartItemId}**

- Authorization: Customer
- Request Body: `UpdateCartItemRequest`
- Response: 200 OK, `CartResponse`
- Errors: 404 `CART_ITEM_NOT_FOUND`, 400 `INSUFFICIENT_STOCK`
- Note: `Quantity: 0` behaves as delete, returns updated cart without that item.

**DELETE /api/cart/items/{cartItemId}**

- Authorization: Customer
- Response: 200 OK, `CartResponse`
- Errors: 404 `CART_ITEM_NOT_FOUND`

**DELETE /api/cart**

- Authorization: Customer
- Response: 204 No Content
- Errors: none (idempotent even if cart is already empty)


## Pagination

Not applicable in Phase 2 — Cart is expected to hold a small number of items (rarely more than 20-30), so no pagination is implemented on cart endpoints.

## Searching

Not applicable in Phase 2.

## Filtering

Not applicable in Phase 2.

## Sorting

Cart items are always returned ordered by `CreatedAt ascending` (order added), no client-controlled sorting needed.

## Security

- `/api/auth/register`, `/api/auth/login`, `/api/auth/google` are `[AllowAnonymous]`.
- All `/api/cart/*` endpoints require `[Authorize(Roles = "Customer")]` — Admin JWTs are explicitly rejected here since Admins have no cart concept.
- Google ID token is verified server-side against Google's public certs on every login call — never trust a client-decoded token without server verification.[^2]
- JWT signing key, issuer, and audience remain the same shared configuration as the Admin JWT (from Infrastructure setup) — only the `role` claim differs, keeping one unified authentication scheme.[^3]
- Rate limiting (introduced generally in Phase 4) should also apply to `/api/auth/login` and `/api/auth/google` to prevent credential brute-forcing, even though full detail is scoped later.
- Passwords hashed via Identity's built-in `PasswordHasher` — never stored or logged in plain text; Serilog log enrichers must be configured to never emit request bodies containing password fields.


## Error Handling

| Error | HTTP Status |
| :-- | :-- |
| Validation failure | 400 |
| Email already exists | 409 |
| Invalid credentials | 401 |
| Account locked | 403 |
| Invalid/expired Google token | 401 |
| Product not found | 404 |
| Product unavailable/inactive | 400 |
| Product out of stock | 400 |
| Insufficient stock for requested quantity | 400 |
| Cart item not found | 404 |
| Unauthorized (no/invalid JWT) | 401 |
| Forbidden (wrong role) | 403 |
| Unhandled exception | 500 (ProblemDetails via Global Exception Middleware) |

## Testing Checklist

**Auth**

- Register with valid data → 201, JWT returned.
- Register with duplicate email → 409.
- Register with weak password → 400.
- Login with correct credentials → 200, JWT returned.
- Login with wrong password → 401.
- Login with non-existent email → 401 (same generic message, no email enumeration leak).
- Google login with valid ID token, new email → new Customer created, 200.
- Google login with valid ID token, email matches existing password-based account → account linked, not duplicated, 200.
- Google login with tampered/invalid ID token → 401.
- GET /api/auth/me without token → 401.
- GET /api/auth/me with Admin token → 403 (role mismatch).

**Cart**

- Add item to empty cart (no Cart row yet) → Cart auto-created, item added, 200.
- Add same product twice → quantity incremented, not duplicated row.
- Add inactive product → 400.
- Add product with QuantityInStock=0 → 400.
- Add quantity exceeding available stock → 400, response includes available stock.
- Update cart item quantity to 0 → item removed, cart returned without it.
- Update cart item quantity exceeding stock → 400.
- Remove existing cart item → 200, item gone.
- Remove non-existent cart item id → 404.
- Clear cart with items → 204, cart empty afterward.
- Clear already-empty cart → 204 (idempotent, no error).
- Fetch cart after admin reduces product stock below cart quantity → `IsAvailable=false`, `AvailableStock` shown, item still present.
- Fetch cart after admin deactivates a product in it → `IsAvailable=false` shown.
- Cart access attempted with Admin JWT → 403.