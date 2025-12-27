# Clean Architecture Refactoring

This document describes the clean architecture structure that has been implemented for the Travelette Relay project.

## Architecture Overview

The project now follows Clean Architecture principles with clear separation of concerns across four main layers:

### 1. Domain Layer (`Domain/`)
The innermost layer containing business entities and domain logic.

**Structure:**
- `Domain/Entities/` - Domain entities (Trip, Booking, ItineraryDay, BookingStatus)
- `Domain/Repositories/` - Repository interfaces (ITripRepository, IBookingRepository)
- `Domain/Services/` - Domain service interfaces (IPaymentService)

**Key Principles:**
- Contains no dependencies on other layers
- Pure business logic and entities
- Interfaces define contracts for external dependencies

### 2. Application Layer (`Application/`)
Contains application-specific business logic and use cases.

**Structure:**
- `Application/DTOs/` - Data Transfer Objects for API communication
- `Application/Services/` - Application services (ITripService, IBookingService, IPaymentApplicationService)
- `Application/Services/TripService.cs` - Implements trip management use cases
- `Application/Services/BookingService.cs` - Implements booking management use cases
- `Application/Services/PaymentApplicationService.cs` - Implements payment processing use cases

**Key Principles:**
- Depends only on Domain layer
- Contains application-specific business logic
- Coordinates domain objects to perform application tasks

### 3. Infrastructure Layer (`Infrastructure/`)
Contains implementations of external concerns.

**Structure:**
- `Infrastructure/Data/` - Data access (AppDbContext compatibility layer)
- `Infrastructure/Repositories/` - Repository implementations (TripRepository, BookingRepository)
- `Infrastructure/Services/` - External service implementations
  - `StripePaymentService.cs` - Stripe payment processing
  - `ConfigurationService.cs` - Configuration access
- `Infrastructure/HealthChecks/` - Health check implementations

**Key Principles:**
- Implements interfaces defined in Domain layer
- Handles all external dependencies (database, APIs, file system)
- Can be swapped without affecting other layers

### 4. Presentation Layer (`Presentation/`)
Contains the API controllers and presentation logic.

**Structure:**
- `Presentation/Controllers/` - API controllers
  - `TripsController.cs`
  - `BookingsController.cs`
  - `PaymentsController.cs`
  - `SettingsController.cs`

**Key Principles:**
- Thin controllers that delegate to application services
- Handles HTTP concerns (requests, responses, status codes)
- No business logic

## Dependency Flow

```
Presentation → Application → Domain ← Infrastructure
```

- **Presentation** depends on **Application** (controllers call application services)
- **Application** depends on **Domain** (uses domain entities and interfaces)
- **Infrastructure** implements **Domain** interfaces (repository and service implementations)
- **Domain** has no dependencies (pure business logic)

## Migration Notes

### Backwards Compatibility
- The old `Data/AppDbContext.cs` is maintained for migration compatibility
- It now references entities from `Domain.Entities` namespace
- Existing migrations continue to work without modification

### Old Files
The following old files have been replaced but may still exist for reference:
- `Models/` - Replaced by `Domain/Entities/`
- `DTOs/` - Replaced by `Application/DTOs/`
- `Controllers/` - Replaced by `Presentation/Controllers/`

These can be safely removed after verifying the new structure works correctly.

## Dependency Injection

All dependencies are configured in `Program.cs`:

```csharp
// Repositories (Domain interfaces -> Infrastructure implementations)
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// Domain services (Domain interfaces -> Infrastructure implementations)
builder.Services.AddScoped<IPaymentService, StripePaymentService>();

// Application services (Application interfaces -> Application implementations)
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();

// Infrastructure services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
```

## Benefits of This Architecture

1. **Separation of Concerns** - Each layer has a single, well-defined responsibility
2. **Testability** - Easy to mock dependencies and test each layer in isolation
3. **Maintainability** - Changes in one layer don't ripple through the entire codebase
4. **Flexibility** - Can swap implementations (e.g., change database, payment provider) without affecting business logic
5. **Scalability** - Easy to add new features following the established patterns

## Next Steps

1. Remove old files (`Models/`, old `DTOs/`, old `Controllers/`) after verification
2. Add unit tests for each layer
3. Consider adding AutoMapper for entity-to-DTO mapping
4. Add validation using FluentValidation or Data Annotations
5. Consider implementing CQRS pattern for more complex use cases

