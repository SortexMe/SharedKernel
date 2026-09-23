# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.0.0] - Unreleased

This release fixes several silent-failure defects found in a full audit. Some fixes change public
behaviour; those are listed under **Breaking** so consumers can plan the upgrade.

### Fixed

- **Mediator: void-command exceptions were swallowed.** When any `IPipelineBehavior` was registered, an
  `IRequestHandler<TRequest>` that faulted its `Task` asynchronously reported success to the caller. The
  exception now propagates. (`RequestHandlerWrapper.cs`)
- **Mediator: `CancellationToken` was lost behind behaviors.** A behavior calling `next()` with no
  argument forwarded `CancellationToken.None` to the handler. The caller's token now flows through;
  passing a token to `next(token)` still substitutes it.
- **Mediator: cache poisoning and process-wide cache state.** `Send<object>(query)` on a covariant
  `IRequest<T>` permanently broke every later `Send(query)` for that request type with
  `InvalidCastException`, for the life of the process. The wrapper cache is now keyed on
  `(request type, response type)` and lives in a `RequestHandlerWrapperCache` registered as a **singleton**
  rather than in a `static` field, so it is owned by the container: no bleed between containers or tests, and
  the cached wrappers are collected when the container is disposed (which lets an `AssemblyLoadContext`
  unload). Sharing is unchanged in practice — one cache per container, so the reflection that builds a
  wrapper still runs once per `(request, response)` pair, not once per scope. Measured end-to-end dispatch is
  unchanged within run-to-run noise.
- **Registration: open generic handlers were never registered.** `GenerateCombinations` produced an empty
  sequence for every input, so `RegisterGenericHandlers = true` did nothing.
- **Registration: `RegistrationTimeout = 0`** (documented as "disabled") armed an already-expired timeout.
- **Registration: `MediatRServiceConfiguration.Lifetime`** only applied to `IMediator`; handlers were always
  `Scoped`. All registrations now use the configured lifetime.
- **Registration:** limits were held in mutable static fields and clobbered between containers; the
  `MaxGenericTypeRegistrations` guard tested the wrong option; `Assembly.GetTypes()` failures
  (`ReflectionTypeLoadException`) took down startup instead of skipping the unloadable types.
- **`UserDomainModel.LoginUser`: lockout never expired.** The account stayed locked after `LockoutEnd`
  passed, with the message "Try again in  minutes and  seconds". `LockoutEnabled` is now honoured and the
  message shows the real remaining time.
- **`UserDomainModel.RefreshToken`** threw `NullReferenceException` on a null presented token and did not
  call `IUserRepository.Update`. Comparison is now null-safe and constant-time.
- **`UserDomainModel.Create`** normalised e-mail/username with the current culture (`"i"` → `"İ"` under
  `tr-TR`). Now uses `ToUpperInvariant()`.
- **`DomainEventMessage.ProcessedTimes`** never incremented (`int?` starting at `null`). It now starts at 0
  and counts; rows persisted as `null` are treated as 0.
- **`DomainEntityBase`:** `null == null` returned `false`. Both entity base classes now also treat two
  *transient* entities (`Id == Guid.Empty`) as unequal, and compare through EF Core proxy types.
- **`ValueObject.GetHashCode()`** threw on a value object with no components and was order-insensitive.
  Now uses `HashCode`; `==`/`!=` are defined on the base class so they agree with `Equals`.
- **`BaseResponseDTO` / `BaseResponseDTO<T>` / `DTOValidationError`** could not be deserialised by
  System.Text.Json. They now round-trip (including camelCase web defaults). `WithSuccess` sets
  `StatusCode` to 200 and `WithErrors` to 400.
- **`DomainException.CreateWithErrors`** produced the default "Exception of type…" message; it now joins
  the error messages.
- **`StringExtensions.ToInt/ToLong`** parsed with the current culture; now invariant.
- **`MessageBrokerHost.Port`** defaulted to 5674, which is neither AMQP (5672) nor AMQPS (5671). Now 5671.

### Security

- **`LoggingBehavior` logged every request property, including passwords, at `Information`.** It now logs
  only the request name and duration at `Information`; properties are logged at `Debug` with
  secret-looking names (`Password`, `Token`, `Secret`, `Key`, `ConnectionString`, …) redacted as `***`.
  Override `ShouldRedact` to adjust.
- **Password-reset tokens were replayable and stored in cleartext.** Tokens are now stored as SHA-512
  hashes, only the newest unexpired token is accepted, and a successful `ResetPassword` /
  `ConfirmEmail` removes every outstanding token of that type.
- `CreateUserDTO`, `MessageBrokerHost`, `TokenResponseDTO`, `UserCreated` and `UserPasswordForgotten` no
  longer print their secret members in `ToString()`.

### Added

- `RequestHandlerWrapperCache` — the mediator's wrapper cache, registered as a singleton by `AddMediator`.
  Exposes `Count` and `Clear()` for diagnostics and tests.
- `Mediator(IServiceProvider, RequestHandlerWrapperCache)` constructor for explicit construction.
- `UserDomainModel.ConfirmEmail(string token)` — counterpart to the confirmation token issued by `Create`.
- `TokenGenerator.HashToken(string)` and `TokenGenerator.VerifyToken(string?, string?)` (constant-time).
- `UserDomainModel` exposes its policy constants (`MaxFailedAccessAttempts`, `LockoutDuration`, …).
- `LoggingBehavior` can now be applied to void `IRequest` commands (constraint relaxed to `notnull`).
- Package now ships XML documentation, a `.snupkg` symbols package, SourceLink metadata, and targets
  `net8.0` as well as `net10.0`.
- GitHub Actions CI (build, test on both targets, pack, publish on `v*` tags).
- `Directory.Build.props` for the settings both projects share, and an `.editorconfig` whose naming and
  unused-using rules are enforced at build time (`EnforceCodeStyleInBuild`).
- `TenantConnection.ConnectionString` now documents that it is a credential the consumer must encrypt at
  rest (the library has no EF Core dependency, so it cannot ship the value converter itself).

### Breaking

- **Token storage.** `ApplicationUserToken.Token` now holds a hash. Any confirmation or reset token issued
  by a previous version will no longer verify; consumers that compared `Token` themselves must use
  `TokenGenerator.VerifyToken` (or call the new `ConfirmEmail`). Existing tokens are short-lived (30 min /
  24 h) so no migration is needed beyond the overlap window.
- `ServiceRegistrar.GenerateCombinations` now takes a `MediatRServiceConfiguration` instead of reading
  static fields; `ServiceRegistrar.SetGenericRequestHandlerRegistrationLimitations` was removed.
- `ValueObject.GetEqualityComponents()` returns `IEnumerable<object?>`; overrides must match.
- `Address` properties are `init`-only (a mutable value object breaks hashing).
- `BaseResponseDTO<T>.Data` is annotated `T?`.
- `LoggingBehavior` no longer logs property values at `Information`. Set the request's logger category to
  `Debug` to see them.
- Entities with `Id == Guid.Empty` are no longer equal to each other.
- `PackageRequireLicenseAcceptance` removed; the package declares the `MIT` license expression.
- **`OccurranceTime` → `OccurrenceTime`** on `DomainEventBase` and `DomainEventMessage`. The old name
  remains as an `[Obsolete]` read-only alias (warning `SK0001`) until 3.0.
  - `DomainEventMessage` is an entity, so its column renames too. Add a migration:
    `migrationBuilder.RenameColumn(name: "OccurranceTime", table: "DomainEventMessages", newName: "OccurrenceTime");`
    (adjust the table name to your mapping).
  - Event payloads serialised under the old name (e.g. undispatched outbox rows) deserialise with a fresh
    `OccurrenceTime`; drain the outbox before upgrading if the original timestamp matters.
- **Version scheme.** The package now follows SemVer (`2.0.0`) instead of the 4-part `1.0.0.117`.
- A custom `MediatorImplementationType` that caches wrappers itself should take a
  `RequestHandlerWrapperCache` rather than a `static` field, for the reasons above.
