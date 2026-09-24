<br/>
<h1 align="center">🛍️ E-Commerce Platform </h1>

<h4 align="center">An Enterprise-Grade E-Commerce Platform connecting Customers & Administrators</h4>

<p align="center">
  <img src="https://img.shields.io/badge/10.0-512BD4?style=for-the-badge&logo=dotnet&label=.NET&labelColor=555555" alt=".NET">
  <img src="https://img.shields.io/badge/WEB_API-512BD4?style=for-the-badge&logo=dotnet&label=ASP.NET_CORE&labelColor=555555" alt="ASP.NET Core">
  <img src="https://img.shields.io/badge/CODE--FIRST-00599C?style=for-the-badge&label=EF_CORE&labelColor=555555" alt="EF Core">
  <img src="https://img.shields.io/badge/DATABASE-336791?style=for-the-badge&logo=postgresql&label=POSTGRESQL&labelColor=555555" alt="PostgreSQL">
  <br>
  <img src="https://img.shields.io/badge/CACHE-DC382D?style=for-the-badge&logo=redis&label=REDIS&labelColor=555555" alt="Redis">
  <img src="https://img.shields.io/badge/AUTH-000000?style=for-the-badge&logo=jsonwebtokens&label=JWT&labelColor=555555" alt="JWT">
  <img src="https://img.shields.io/badge/MEDIATR-512BD4?style=for-the-badge&label=CQRS&labelColor=555555" alt="CQRS">
  <img src="https://img.shields.io/badge/API_DOCS-85EA2D?style=for-the-badge&logo=swagger&logoColor=black&label=SWAGGER&labelColor=555555" alt="Swagger">
  <br>
  <img src="https://img.shields.io/badge/CLEAN_ARCHITECTURE-512BD4?style=for-the-badge&label=ARCHITECTURE&labelColor=555555" alt="Architecture">
  <img src="https://img.shields.io/badge/MIT-85EA2D?style=for-the-badge&label=LICENSE&labelColor=555555&color=85EA2D" alt="License">
</p>

<p align="center">
  <a  href="#-getting-started">Getting Started</a> · <a href="#-architecture">Architecture</a> · <a href="#-api-reference-highlights">API Reference</a>
</p>

## 📖 Overview
E-Commerce Platform is a full-featured, enterprise-grade backend built on **.NET 10** using **Clean Architecture** and **CQRS**. It serves as a robust engine for modern digital storefronts, providing secure and high-performance operations for both customers and administrators.

| Actor | Role |
| :--- | :--- |
| **🛍️ Customers** | Browse catalog, manage Redis-backed shopping carts, place orders, securely checkout via Moyasar, and track order status. |
| **🏢 Administrators** | Manage product catalog (CRUD), oversee orders, handle user management, and view real-time sales analytics and dashboards. |

## 🌟 Why This Platform?
Modern e-commerce requires extreme speed, security, and maintainability. This platform solves common bottlenecks by:
- **High-Performance Cart Management:** Utilizing **Redis** for sub-millisecond shopping cart operations, avoiding unnecessary relational database hits.
- **Strict Architecture:** Implementing **Clean Architecture** with **Vertical Slices** and **CQRS (MediatR)** to completely decouple business logic from infrastructure.
- **Robust Error Handling:** Eliminating `try/catch` spaghetti code through a standardized **Result Pattern** across all layers.
- **Secure Payment Processing:** Seamless integration with **Moyasar** for compliant and secure payment flows via Webhooks.
- **Media Management:** Offloading image processing and storage to **Cloudinary**.
- **Automations:** Third-party webhook integrations via **N8n**.

## ✨ Key Features

### 🔐 Authentication & Identity
- **Multi-Role Access:** Dedicated authorization policies for `Customer` and `Admin`.
- **JWT Bearer Auth:** Secure stateless authentication with customizable Expiry.
- **Google OAuth:** One-click seamless social login integration.
- **Profile Management:** Secure password changes, profile updates, and secure hashing via ASP.NET Core Identity.

### 📦 Catalog Management
- **Hierarchical Categories:** Organize products logically.
- **Advanced Product Management:** CRUD operations, automatic SKU generation, and stock tracking.
- **Media Uploads:** Direct integration with Cloudinary for product image galleries (upload, delete, set primary).
- **Search & Pagination:** Highly optimized database queries for product filtering and searching.

### 🛒 Redis-Backed Shopping Cart
- **Fast Operations:** Instant item addition, removal, and quantity updates.
- **Dynamic Pricing:** Real-time cart total calculation.
- **TTL Expiry:** Automatic cleanup of abandoned carts using Redis Key Expiration logic.

### 💳 Orders & Payments
- **Secure Checkout:** Converts carts into persistent orders securely and atomically.
- **Moyasar Gateway:** Integration for Visa/Mastercard/Mada payments.
- **Webhook Processing:** Asynchronous order status updates (Paid, Failed, Cancelled) triggered by payment gateway callbacks.
- **Unique Order Numbers:** Thread-safe, Redis-backed human-readable order number generator.

### 📊 Admin Dashboard
- **Analytics:** High-level KPIs for sales trends, total revenue, and order volume.
- **Customer Oversight:** Comprehensive customer search, pagination, and detail views.

---

## 🏗 Architecture
This project is built on **Clean Architecture**, ensuring complete decoupling between business logic and infrastructure concerns. Each layer has a single, well-defined responsibility and references flow strictly inward.

### Why Clean Architecture?
| Concern | Solution |
| :--- | :--- |
| **Testability** | The Domain and Application layers have zero infrastructure dependencies, enabling fast unit testing without a database. |
| **Replaceability** | PostgreSQL can be swapped for SQL Server by changing configuration in `ConfigureServices.cs`. |
| **Separation of Concerns** | Infrastructure concerns (EF Core, Cloudinary, Moyasar) cannot "leak" into business logic. |
| **Maintainability** | Each service does one thing via MediatR handlers. Adding a new feature is always additive. |

### Project Structure
```text
ECommerce-Backend.sln
├── src/
│   ├── ECommerce.Domain/         # 🏛️ Core — Entities, Enums, Exceptions, Result<T> (Zero dependencies)
│   ├── ECommerce.Application/    # 📐 Business Logic — CQRS Features, DTOs, Validation, Interfaces
│   ├── ECommerce.Infrastructure/ # 🔧 Implementation — EF Core, Redis, Cloudinary, Moyasar
│   └── ECommerce.Api/            # 🌐 Presentation — Controllers, Middlewares, Serilog, DI Hub
```

---

## 💻 Technologies
| Category | Technology | Version | Purpose |
| :--- | :--- | :--- | :--- |
| **Runtime** | .NET / C# | 10.0 | Core framework |
| **Web Framework** | ASP.NET Core Web API | 10.0 | HTTP server and routing |
| **ORM** | Entity Framework Core | 10.x | Code-First database access |
| **Database** | PostgreSQL | Latest | Primary relational data store |
| **Caching** | Redis | Latest | Fast in-memory store for carts |
| **Auth** | ASP.NET Core Identity | 10.x | User management, password hashing |
| **Messaging** | MediatR | Latest | CQRS implementation |
| **Validation** | FluentValidation | Latest | Request payload validation |
| **Payments** | Moyasar API | - | Payment gateway integration |
| **Storage** | Cloudinary | - | Image hosting and delivery |
| **Logging** | Serilog | Latest | Structured logging to Console/File |

---

## 🎨 Design Patterns

### Clean Architecture & Vertical Slices
The project uses MediatR to organize the `Application` layer into Vertical Slices by feature (e.g., `Features/Orders/Commands/CreateOrder`). This prevents "service bloat" and strictly adheres to the Single Responsibility Principle.

### CQRS Pattern (Command Query Responsibility Segregation)
Read operations (Queries) are completely separated from Write operations (Commands). Each operation has its own dedicated Handler (`IRequestHandler`), allowing highly optimized queries (e.g., using `.AsNoTracking()`).

### Result Pattern
Instead of throwing exceptions for expected failures, services return a `Result<T>` or `Result` object.
```csharp
public async Task<Result<OrderResponse>> Handle(CreateOrderCommand request, CancellationToken ct)
{
    if (!stockAvailable)
        return Result.Failure<OrderResponse>(DomainErrors.Product.OutOfStock);

    return Result.Success(orderResponse);
}
```
The `BaseApiController` automatically maps these results to appropriate HTTP Status Codes (200 OK, 400 Bad Request, 404 Not Found), eliminating the need for `try/catch` logic in controllers.

### Options Pattern
All configurations (JWT, Redis, Cloudinary, Moyasar, N8n, Email) are bound via `IOptions<T>` using typed settings classes. Settings are validated at startup. Secrets are never hardcoded.

---

## 🔐 Security

- **Authentication:** HMAC-SHA256 JWT tokens.
- **Authorization:** Role-based policies declared in extensions (`[Authorize(Policy = "AdminOnly")]`).
- **Data Protection:** Passwords securely hashed via ASP.NET Core Identity.
- **Input Validation:** All DTOs validated via `FluentValidation`. Invalid payloads return a structured `ProblemDetails` error without hitting the database.
- **Secrets Management:** Sensitive keys are never hardcoded. `appsettings.json` is git-ignored, and developers must use `appsettings.example.json` as a template.

---

## 📚 API Reference (Highlights)

### Auth (`/api/v1/auth`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `POST` | `/register` | Register a new customer | Public |
| `POST` | `/login` | Authenticate and receive JWT | Public |
| `POST` | `/google-login` | Authenticate via Google OAuth | Public |
| `GET`  | `/profile` | Get current authenticated user profile | 🔒 Customer |

### Catalog (`/api/v1/products`) & (`/api/v1/admin/products`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/products` | List all products (paginated, filtered) | Public |
| `GET` | `/products/{id}` | Get product details | Public |
| `POST` | `/admin/products` | Create a new product | 🔒 Admin |
| `POST` | `/admin/products/{id}/images` | Upload image to Cloudinary | 🔒 Admin |

### Cart (`/api/v1/cart`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/` | Get current customer's cart | 🔒 Customer |
| `POST` | `/items` | Add item to cart | 🔒 Customer |
| `DELETE` | `/items/{id}` | Remove item from cart | 🔒 Customer |

### Orders & Webhooks (`/api/v1/orders`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `POST` | `/checkout` | Process cart and place order | 🔒 Customer |
| `GET` | `/{id}` | View order receipt | 🔒 Customer |
| `POST` | `/api/v1/webhooks/moyasar` | Payment callback webhook | System |

---

## 🚀 Getting Started

### Prerequisites
| Requirement | Minimum Version |
| :--- | :--- |
| **.NET SDK** | 10.0 |
| **PostgreSQL** | 14+ (Local or Docker) |
| **Redis** | 6+ (Local or Docker) |

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/ECommerce-Backend.git
cd ECommerce-Backend
```

### 2. Configure Settings
Copy the example configuration and fill in your values:
Copy `src/ECommerce.Api/appsettings.example.json` to `src/ECommerce.Api/appsettings.Development.json` and fill in your own configuration values (DB Connection, Redis, JWT Secret, Cloudinary Keys, Moyasar Keys).

### 3. Apply Database Migrations
Navigate to the root directory and apply EF Core migrations to create the PostgreSQL schema:
```bash
dotnet ef database update --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Api
```

### 4. Run the Application
```bash
dotnet run --project src/ECommerce.Api
```

### 5. Access Swagger
Open your browser at:
`https://localhost:<port>/swagger`
Click **Authorize** and enter `Bearer <your_jwt_token>` to authenticate.

---

## ⚡ Performance Optimizations

| Optimization | Implementation |
| :--- | :--- |
| **Read-Only Queries** | `.AsNoTracking()` on all non-mutating EF Core queries. |
| **Redis Caching** | The shopping cart avoids PostgreSQL entirely, relying on Redis for ultra-fast mutations. |
| **Server-Side Pagination** | `.Skip().Take()` combined with `.CountAsync()` ensures we never load all records into memory. |
| **Asynchronous Execution** | 100% async/await pipeline (`Task`, `CancellationToken`) prevents thread starvation. |

---

## 🤝 Contributing
Contributions are welcome! Please follow these steps:
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Commit your changes: `git commit -m "feat: add your feature"`
4. Push to your fork: `git push origin feature/your-feature-name`
5. Open a Pull Request


---
<div align="center">
Built with .NET 10, Clean Architecture, and CQRS Pattern
</div>
