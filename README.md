# SharedKernel

SharedKernel is a reusable, open-source .NET library of foundational building blocks for Domain-Driven Design (DDD), Clean Architecture, and CQRS. It provides a lightweight mediator, pipeline behaviors, domain event primitives, base entities, standardized DTOs, domain exceptions, and utilities that reduce boilerplate and promote consistent patterns across services.

- Target framework: `net10.0`
- Package id: `Sortex.SharedKernel`
- Current package version in this repository: `1.0.0.116`
- License: `MIT` (see `LICENSE`)

---

## Project overview

SharedKernel focuses on common building blocks used across multiple services and bounded contexts:

- CQRS / mediator primitives and pipeline behaviors
- Aggregate root base classes and domain event support
- Standard response DTOs and validation error types
- Domain exception types with implicit conversions to `BaseResponseDTO`
- Utilities for token generation, date/time helpers, platform detection and common extensions
- Markers and interfaces to support multi-tenant and company-scoped entities

The repository includes integration and unit tests for the mediator pipeline under `test/SharedKernel.Mediator.Tests` to demonstrate expected behavior, concurrency safety and performance characteristics.

---

## Key features

- CQRS / Mediator primitives
 - `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`, `IMediator`
 - DI registration helpers (see `SharedKernel.DependencyInjection` in tests)
 - Pipeline behaviors via `IPipelineBehavior<TRequest, TResponse>` (example: `LoggingBehavior<TRequest,TResponse>`)

- Domain model helpers
 - `EntityBase`, `DomainEntityBase` (aggregate support and event registration)
 - `DomainEventBase`, `DomainEventMessage` and `IDomainEventDispatcher` for safe dispatch and retry/persistence

- Standard DTOs & Exceptions
 - `BaseResponseDTO` and `BaseResponseDTO<T>` for consistent API responses
 - `DTOValidationError` and helpers to produce standardized error payloads
 - `DomainException`, `NotAuthorizedException`, `RecordNotFoundException` with implicit conversions to `BaseResponseDTO`

- Utilities & extensions
 - `TokenGenerator` for cryptographic token generation
 - Date/time helpers and platform detection utilities
 - Marker interfaces like `ICompanyRelatedEntity` for company/tenant scoping

- Tests & quality
 - Extensive mediator tests validate pipeline ordering, concurrency, error propagation and performance

---

## Install

From NuGet (recommended):

```bash
dotnet add package Sortex.SharedKernel
```

Or reference the project directly in your solution:

```xml
<ProjectReference Include="src/SharedKernel/SharedKernel.csproj" />
```

---

## Quick start

Below are minimal examples showing how to register the mediator, add pipeline behaviors, create a request/handler, and use common DTOs/exceptions.

###1) Register mediator and pipeline behaviors (DI)

The test projects demonstrate a common pattern to register the mediator and scan for handlers from an assembly. Example (Program.cs / Startup.cs):

```csharp
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Behaviors;
using System.Reflection;

var services = new ServiceCollection();

// Optional: register individual handlers
// services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();

// Register mediator and scan handlers/behaviors from the provided assembly
services.AddMediator(options =>
{
 options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

// Register pipeline behaviors explicitly if needed
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

var provider = services.BuildServiceProvider();
```

> The repository tests use `AddMediator` and `RegisterServicesFromAssembly` helpers to automatically discover handlers and other services.

###2) Define a request and handler

```csharp
public record PingCommand(string Message) : IRequest<string>;

public class PingCommandHandler : IRequestHandler<PingCommand, string>
{
 public Task<string> Handle(PingCommand request, CancellationToken cancellationToken)
 {
 return Task.FromResult($"Pong: {request.Message}");
 }
}
```

Send a request via `IMediator`:

```csharp
var mediator = provider.GetRequiredService<IMediator>();
var result = await mediator.Send(new PingCommand("Hello"));
// result -> "Pong: Hello"
```

###3) Use pipeline behaviors

Register `LoggingBehavior<TRequest, TResponse>` to log request properties and handling time. Example registration is shown above. The behavior logs each public property via reflection and timing information using `ILogger<TRequest>`.

###4) Use `BaseResponseDTO` and domain exceptions

`BaseResponseDTO` and `BaseResponseDTO<T>` are intended for standardized API responses.

```csharp
var success = BaseResponseDTO.WithSuccess();
var data = BaseResponseDTO<string>.WithSuccess("payload");

// Convert DomainException to response (implicit operator)
var domainEx = DomainException.CreateDetailedException("Invalid input", "InvalidValidator", "Name");
BaseResponseDTO resp = domainEx; // resp.StatusCode =400 and errors populated
```

`NotAuthorizedException` similarly converts to a `DTOValidationError` and `BaseResponseDTO` with a401 status code.

###5) Domain model example — `UserDomainModel`

The repository contains `UserDomainModel` that demonstrates realistic domain logic and domain events. Common operations include:

- `UserDomainModel.ForgetPassword()` — generates a reset token, registers a `UserPasswordForgotten` domain event and updates the user entity.
- `UserDomainModel.ResetPassword(token, passwordHash)` — validates token, updates password and emits `UserPasswordReset` event.
- `UserDomainModel.LoginUser(isPasswordValid, newRefreshToken, refreshTokenDuration)` — validates user state and issues a refresh token; throws `DomainException` on validation failures.

Usage sketch:

```csharp
// Assume userRepository and an existing ApplicationUser instance
var domainModel = new UserDomainModel(user, userRepository);

domainModel.ForgetPassword(); // registers event and saves token

try
{
 domainModel.ResetPassword(token, newHash);
}
catch (DomainException ex)
{
 // Convert to API response
 var apiResp = (BaseResponseDTO)ex;
}
```

Domain exceptions are created via factory methods and include structured `DTOValidationError` items to return to API callers.

---

## Tests

Tests for the mediator are included under `test/SharedKernel.Mediator.Tests`. They cover:

- Basic send/handle scenarios
- Pipeline behavior ordering and cancellation propagation
- Concurrency and performance (throughput, memory usage)
- Error propagation and handler discovery

Run tests:

```bash
dotnet test
```

---

## Packaging

`SharedKernel.csproj` is configured to generate a NuGet package on build (`GeneratePackageOnBuild`), include `README.md` in the package, and expose package metadata such as title, tags and license. The package id is `Sortex.SharedKernel` (see `src/SharedKernel/SharedKernel.csproj`).

---

## Contributing

Contributions are welcome. Recommended workflow:

- Fork the repository and make small, focused changes
- Add unit tests for new behaviors or bug fixes
- Follow nullable and coding conventions (this project uses nullable references enabled)
- Update `README.md` and public XML docs when adding or changing public API

Please include a clear PR description explaining the change and rationale.

---

## Roadmap ideas

- Add assembly-scanning registration that optionally auto-registers handlers and behaviors
- Additional built-in pipeline behaviors: validation, retry, metrics
- Reference example projects: ASP.NET Core minimal API + EF Core + an example message broker integration

---

## License

This project is licensed under the MIT License. See `LICENSE` for details.
