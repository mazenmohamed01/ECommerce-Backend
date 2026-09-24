<h1 align="center">🛍️ E-Commerce Backend API</h1>

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%7C%20CQRS-2ea44f?style=for-the-badge)

A highly scalable, robust, and professional RESTful backend API for a modern E-Commerce platform. Built with **.NET 10** using **Clean Architecture**, **Vertical Slice Architecture**, and the **CQRS Pattern**.

</div>

---

## 📖 Table of Contents

- [Project Overview](#-project-overview)
- [Architecture & Design Patterns](#-architecture--design-patterns)
- [Tech Stack](#-tech-stack)
- [Key Features](#-key-features)
- [Project Structure](#-project-structure)
- [Getting Started (Local Development)](#-getting-started-local-development)
- [Engineering Standards](#-engineering-standards)
- [API Documentation](#-api-documentation)
- [License](#-license)

---

## 🎯 Project Overview

This API serves as the core backend engine for a full-fledged E-Commerce platform. It is engineered to handle complex business requirements while ensuring high performance, maintainability, and scalability. The system provides secure and isolated functionalities for both **Customers** (browsing, cart management, checkout) and **Administrators** (catalog management, order fulfillment, dashboard analytics).

---

## 🏗 Architecture & Design Patterns

The project meticulously follows modern software engineering best practices:

*   **Clean Architecture:** Strict separation of concerns across `Domain`, `Application`, `Infrastructure`, and `Api` layers. The core business logic is entirely independent of external frameworks.
*   **Vertical Slice Architecture & CQRS:** The `Application` layer is organized by features (e.g., `Features/Orders`, `Features/Identity`) rather than technical concerns. **MediatR** is used to strictly separate Read operations (Queries) from Write operations (Commands).
*   **Repository Pattern & Unit of Work:** Abstracts data access, providing a clean interface for database transactions and persistence mapping via Entity Framework Core.
*   **Result Pattern:** Exception handling for control flow is eliminated. Instead, `Result<T>` and `Error` objects are returned across all layers, providing deterministic and predictable API responses (`BaseApiController`).
*   **Pipeline Behaviors:** MediatR behaviors are configured for cross-cutting concerns such as Request Logging and FluentValidation.

---

## 🛠 Tech Stack

- **Framework:** .NET 10 (C#)
- **Database:** PostgreSQL (via Npgsql) with Entity Framework Core
- **Caching:** Redis (via StackExchange.Redis)
- **Authentication:** ASP.NET Core Identity, JWT Bearer Auth, Google OAuth
- **Payments:** Moyasar Payment Gateway Integration
- **Storage:** Cloudinary (for scalable image hosting)
- **Webhooks & Automation:** N8n Integration
- **Logging:** Serilog (File & Console logging)
- **Mapping:** Mapster

---

## ✨ Key Features

### 🔐 Identity & Authentication
*   Secure JWT-based authentication with Refresh Tokens.
*   Role-based Authorization (Admin vs Customer).
*   Google OAuth seamless login/registration.
*   Password reset and profile management.

### 📦 Catalog Management (Admin & Public)
*   Hierarchical category management.
*   Product CRUD with inventory tracking and automatic SKU generation.
*   Cloudinary integration for product image galleries (upload, delete, set primary).
*   Search, filtering, and pagination for products.

### 🛒 Shopping Cart
*   High-performance cart persistence backed by Redis.
*   Dynamic total calculations, item addition/removal, and stock validation.

### 💳 Order Processing & Payments
*   Checkout process with shipping addresses.
*   Integration with **Moyasar** for secure credit card payment processing.
*   Secure webhook endpoints to asynchronously update order statuses (Paid, Failed, Cancelled).
*   Redis-backed unique human-readable Order Number generation.

### 📊 Admin Dashboard
*   Analytics endpoints for sales trends, revenue tracking, and inventory alerts.
*   Comprehensive customer search and order management.

---

## 📁 Project Structure

```text
ECommerce-Backend/
├── src/
│   ├── ECommerce.Domain/         # Core Entities, Enums, Interfaces, Errors, Result Pattern
│   ├── ECommerce.Application/    # CQRS Features (Commands/Queries), MediatR, FluentValidation
│   ├── ECommerce.Infrastructure/ # EF Core DbContext, Repositories, Redis, Services (Cloudinary, Moyasar)
│   └── ECommerce.Api/            # API Controllers, Middleware, Serilog, Dependency Injection Hub
├── tests/
│   └── ECommerce.IntegrationTests/ # Integration tests and WebApplicationFactory
└── ECommerce-Backend.sln
```

---

## 🚀 Getting Started (Local Development)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (Running locally or via Docker)
- [Redis](https://redis.io/) (Running locally or via Docker)

### 1. Clone the repository
```bash
git clone https://github.com/your-username/ECommerce-Backend.git
cd ECommerce-Backend/src/ECommerce.Api
```

### 2. Configuration (`appsettings.Development.json`)
You must configure the `appsettings.Development.json` file in the `ECommerce.Api` project with your own credentials. Create the file if it does not exist:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ECommerceDb;Username=postgres;Password=YOUR_PASSWORD",
    "RedisConnection": "localhost:6379,abortConnect=false"
  },
  "Jwt": {
    "Issuer": "https://api.ecommerce.com",
    "Audience": "https://ecommerce.com",
    "Secret": "YOUR_STRONG_SECRET_KEY_AT_LEAST_32_CHARS_LONG",
    "ExpiryMinutes": 60
  },
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  },
  "Moyasar": {
    "ApiKey": "your-moyasar-api-key",
    "BaseUrl": "https://api.moyasar.com/v1"
  }
}
```

### 3. Apply Database Migrations
Navigate to the root directory and apply EF Core migrations to create the PostgreSQL schema:
```bash
dotnet ef database update --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Api
```

### 4. Run the Application
```bash
dotnet run --project src/ECommerce.Api
```

---

## 🛡 Engineering Standards

- **SOLID Principles:** Every class adheres to the Single Responsibility Principle. Components are highly cohesive and loosely coupled.
- **Fail-Fast Validation:** All incoming requests are validated in the `Application` layer using `FluentValidation` before reaching the core domain logic.
- **No Exceptions for Control Flow:** Business rule violations return Domain `Error` objects, mapped to standardized HTTP status codes (e.g., 400, 404, 409) globally.
- **Async All The Way:** Comprehensive use of asynchronous programming (`Task`, `CancellationToken`) to maximize throughput.

---

## 📚 API Documentation

When running locally in the Development environment, Swagger UI is automatically enabled.
Navigate to `https://localhost:port/swagger` to visually explore, authenticate, and test all API endpoints.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
