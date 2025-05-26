# CMAPI - Contact Management API

A robust and modern Contact Management API built with ASP.NET Core 8.0, featuring secure authentication, Redis caching, and PostgreSQL database integration.

## 🚀 Features

- **Authentication & Authorization**

  - JWT-based authentication
  - Secure password hashing with BCrypt
  - Role-based access control

- **Database & Caching**

  - PostgreSQL database integration
  - Redis caching for improved performance
  - Entity Framework Core for data access

- **API Features**
  - RESTful API design
  - Swagger/OpenAPI documentation
  - AutoMapper for object mapping
  - Comprehensive DTOs for data transfer

## 🛠️ Technologies

- ASP.NET Core 8.0
- Entity Framework Core 8.0
- PostgreSQL
- Redis
- JWT Authentication
- Swagger/OpenAPI
- Docker support
- WebSockets

## 📋 Prerequisites

- .NET 8.0 SDK
- PostgreSQL
- Redis
- Docker (optional)

## 🚀 Getting Started

1. **Clone the repository**

   ```bash
   git clone [your-repository-url]
   cd CMAPI
   ```

2. **Configure the database**

   - Update the connection string in `appsettings.json` with your PostgreSQL credentials
   - Update the Redis connection string if needed

3. **Run database migrations**

   ```bash
   dotnet ef database update
   ```

4. **Run the application**

   ```bash
   dotnet run
   ```

   Or using Docker:

   ```bash
   docker build -t cmapi .
   docker run -p 5000:80 cmapi
   ```

5. **Access the API**
   - API will be available at `https://localhost:5001`
   - Swagger documentation at `https://localhost:5001/swagger`

## 🔧 Database Migrations

To create a new migration:

```bash
dotnet ef migrations add [MigrationName]
```

To apply migrations:

```bash
dotnet ef database update
```

To remove the last migration:

```bash
dotnet ef migrations remove
```

## 📁 Project Structure

```
CMAPI/
├── Controllers/     # API endpoints
├── Models/         # Database models
├── DTO/            # Data transfer objects
├── Services/       # Business logic
├── Data/           # Database context and configurations
├── Middleware/     # Custom middleware components
├── Helper/         # Utility classes
└── Migrations/     # Database migrations
```

## 🔐 Environment Variables

The following environment variables can be configured in `appsettings.json`:

- Database connection string
- Redis connection string
- JWT secret key
- API configuration settings

## 📚 API Documentation

The API documentation is available through Swagger UI when running the application. Access it at:

```
https://localhost:5001/swagger
```

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.
