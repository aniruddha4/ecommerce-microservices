# E-Commerce Microservices Platform

A distributed e-commerce system built with independently deployable microservices communicating asynchronously via RabbitMQ.

🛍️ **Product Service:** https://productsvc-production-10f5.up.railway.app/scalar/v1  
📦 **Order Service:** https://ordersvc-production-b4d4.up.railway.app/scalar/v1  
💻 **GitHub:** https://github.com/aniruddha4/ecommerce-microservices

> **Testing:** Use Postman or the Scalar UI for API testing.

---

## Overview

This project demonstrates a microservices architecture for an e-commerce platform. Rather than a single monolithic application, the system is split into three independently deployable services, each owning its own database and communicating through message queues. When a customer places an order, the Order Service publishes an event to RabbitMQ, which the Notification Service consumes to send a confirmation — without the services knowing about each other directly.

---

## High-Level Architecture

```
                         ┌─────────────────┐
                         │   API Client    │
                         │ (Postman/Browser)│
                         └────────┬────────┘
                                  │ HTTP
                    ┌─────────────┼──────────────┐
                    │             │              │
                    ▼             ▼              ▼
        ┌───────────────┐ ┌─────────────┐ ┌──────────────┐
        │ Product       │ │   Order     │ │Notification  │
        │ Service       │ │   Service   │ │  Service     │
        │ :5101         │ │   :5102     │ │  :5103       │
        └───────┬───────┘ └──────┬──────┘ └──────┬───────┘
                │                │               │
                ▼                ▼               │
        ┌───────────┐    ┌───────────┐           │
        │  MongoDB  │    │PostgreSQL │           │
        │(Products) │    │ (Orders)  │           │
        └───────────┘    └───────────┘           │
                                │                │
                         ┌──────▼────────────────▼──┐
                         │        RabbitMQ           │
                         │   (Message Broker)        │
                         │  OrderPlacedEvent ──────► │
                         └───────────────────────────┘
                ▲
        ┌───────┴───────┐
        │     Redis     │
        │    (Cache)    │
        └───────────────┘
```

---

## Message Flow Diagram

```
Customer          ProductSvc        OrderSvc         RabbitMQ      NotificationSvc
    │                 │                 │                │                │
    │──GET /products─►│                 │                │                │
    │                 │──Check Redis    │                │                │
    │                 │  (cache miss)   │                │                │
    │                 │──Query MongoDB  │                │                │
    │◄──products──────│                 │                │                │
    │                 │──Store in Redis  │               │                │
    │                 │                 │                │                │
    │──POST /orders──────────────────►  │                │                │
    │                 │                 │──INSERT Order──►│               │
    │                 │                 │──Publish        │               │
    │                 │                 │  OrderPlaced───►│               │
    │◄──201 Created──────────────────── │                │               │
    │                 │                 │                │──Consume ──────►│
    │                 │                 │                │  OrderPlaced   │
    │                 │                 │                │                │──Log email
    │                 │                 │                │                │  notification
```

---

## Project Structure

```
ECommerceMS/
├── ProductService/                  # Product management service
│   ├── Controllers/
│   │   └── ProductsController.cs    # CRUD for products
│   ├── Models/
│   │   └── Product.cs               # Product entity (MongoDB document)
│   ├── Program.cs                   # MongoDB + Redis + MassTransit setup
│   └── appsettings.json
│
├── OrderService/                    # Order processing service
│   ├── Controllers/
│   │   └── OrdersController.cs      # Place and retrieve orders
│   ├── Data/
│   │   ├── OrderDbContext.cs        # EF Core DbContext
│   │   └── OrderDbContextFactory.cs # Design-time factory for migrations
│   ├── Models/
│   │   ├── Order.cs                 # Order entity
│   │   ├── OrderItem.cs             # Order line item entity
│   │   └── CreateOrderDto.cs        # Request DTO
│   ├── Contracts/
│   │   └── OrderPlacedEvent.cs      # Shared message contract
│   └── Program.cs                   # EF Core + MassTransit setup
│
├── NotificationService/             # Event consumer service
│   ├── Consumers/
│   │   └── OrderPlacedConsumer.cs   # Handles OrderPlacedEvent
│   ├── Contracts/
│   │   └── OrderPlacedEvent.cs      # Shared message contract
│   └── Program.cs                   # MassTransit consumer setup
│
├── Shared.Contracts/                # Shared message definitions
│   └── OrderPlacedEvent.cs          # Event published by OrderSvc
│
└── docker-compose.yml               # Local infrastructure setup
```

---

## Components

### ProductService
Manages the product catalogue. Products are stored in MongoDB as documents. A Redis cache layer sits in front of MongoDB — when `GET /api/products` is called, the service first checks Redis. On a cache hit, it returns the cached data immediately without hitting MongoDB. On a cache miss, it queries MongoDB and stores the result in Redis with a 5-minute expiry. Write operations (create, delete) invalidate the cache.

### OrderService
Handles order placement. Uses PostgreSQL via Entity Framework Core for reliable relational storage with full ACID transactions. When an order is successfully saved, the service publishes an `OrderPlacedEvent` to RabbitMQ using MassTransit. This decouples order processing from notification logic entirely.

### NotificationService
A pure consumer — it has no HTTP endpoints of its own. It listens permanently to the RabbitMQ queue and processes `OrderPlacedEvent` messages as they arrive. In this implementation it logs an email notification; in production this would integrate with an email provider like SendGrid.

### RabbitMQ (MassTransit)
The message broker enabling asynchronous communication between services. MassTransit is used as the abstraction layer on top of RabbitMQ, handling queue creation, message serialisation, retry policies, and error queues automatically. Services never call each other's HTTP endpoints directly.

### MongoDB (ProductService database)
A document database suited to product catalogues where schema flexibility is valuable. Each product is stored as a BSON document in the `products` collection.

### PostgreSQL (OrderService database)
A relational database for orders, where transactional integrity is critical. Uses Entity Framework Core with auto-applied migrations on startup.

### Redis (ProductService cache)
An in-memory key-value store used to cache the full product listing. Reduces database load and improves response times for read-heavy product browsing.

---

## API Endpoints

### Product Service
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all products (cached) |
| GET | `/api/products/{id}` | Get a product by ID |
| POST | `/api/products` | Create a new product |
| DELETE | `/api/products/{id}` | Delete a product |

### Order Service
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/orders` | Get all orders |
| POST | `/api/orders` | Place a new order |

---

## Tech Stack

| Technology | Purpose |
|------------|---------|
| ASP.NET Core 10 | Web API framework (×3 services) |
| MassTransit | Message bus abstraction |
| RabbitMQ | Message broker |
| MongoDB | Product catalogue storage |
| PostgreSQL | Order storage |
| Redis | Product cache |
| Entity Framework Core | ORM for OrderService |
| Docker / Docker Compose | Local infrastructure |
| Railway | Cloud deployment |

---

## Running Locally

### Prerequisites
- .NET 10 SDK
- Docker Desktop

```bash
# Clone the repository
git clone https://github.com/aniruddha4/ecommerce-microservices.git
cd ecommerce-microservices

# Start all infrastructure (RabbitMQ, MongoDB, PostgreSQL, Redis)
docker-compose up -d

# Terminal 1 - ProductService
cd ProductService && dotnet run --urls "http://localhost:5101"

# Terminal 2 - OrderService
cd OrderService && dotnet run --urls "http://localhost:5102"

# Terminal 3 - NotificationService
cd NotificationService && dotnet run --urls "http://localhost:5103"
```

| Service | URL |
|---------|-----|
| ProductService | http://localhost:5101/scalar/v1 |
| OrderService | http://localhost:5102/scalar/v1 |
| RabbitMQ Dashboard | http://localhost:15672 (guest/guest) |
