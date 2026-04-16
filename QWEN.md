# LoggingService — Complete Project Context

## Overview

**LoggingService** is a .NET 10 microservice that provides **centralized logging, audit tracking, and user synchronization** for the **NIRAST Workforce Management System**. It is a **query-heavy, event-driven** microservice that:

1. **Receives** log events (errors, activities, login audits) via RabbitMQ from other microservices
2. **Stores** them in PostgreSQL with rich filtering/indexing
3. **Exposes** REST APIs for querying, filtering, and dashboard summaries
4. **Syncs** user data from `UserManagementService` via RabbitMQ for local resolution
5. **Cleans up** old logs on a configurable retention schedule (default: 30 days)

---

## Architecture: Clean Architecture (4 Layers)

```
┌─────────────────────────────────────────────────────────────────┐
│                    LoggingService.API (Presentation)            │
│  Controllers │ Middleware │ Program.cs │ JWT Auth │ Serilog     │
├─────────────────────────────────────────────────────────────────┤
│                LoggingService.Application (Use Cases)            │
│  MediatR Commands/Queries │ Handlers │ DTOs │ Exceptions        │
├─────────────────────────────────────────────────────────────────┤
│              LoggingService.Infrastructure (Data Access)         │
│  EF Core DbContext │ Services │ RabbitMQ Consumers │ Migrations  │
│  Background Services (cleanup, consumer host) │ Seeding          │
├─────────────────────────────────────────────────────────────────┤
│                  LoggingService.Domain (Entities)                │
│  Entities (ErrorLog, ActivityLog, LoginAudit, UserSync)         │
│  Enums (Severity, ActionType, EntityType, etc.)                 │
│  Common (BaseEntity, IEntity)                                    │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Flow

```
API → Application → Domain
API → Infrastructure → Application → Domain
```

The API layer depends on both Application and Infrastructure. Infrastructure depends on Application and Domain. Application depends only on Domain. Domain has zero external dependencies.

---

## Layer Details

### Domain Layer (`LoggingService.Domain`)

**Zero NuGet dependencies.** Pure POCOs, enums, and interfaces.

#### Entities

| Entity | Key Type | Base Class | Mutability | Purpose |
|---|---|---|---|---|
| `ErrorLog` | `Guid` | `BaseEntity` | **Immutable** (write-once) | Captures errors/exceptions from backend and frontend |
| `ActivityLog` | `Guid` | `BaseEntity` | **Immutable** (write-once) | Audit trail for all CRUD operations on domain entities |
| `LoginAudit` | `Guid` | `BaseEntity` | **Immutable** (write-once) | Login success and logout events only |
| `UserSync` | `string UserId` | None | **Mutable** (updated on sync) | Local cache of user data from UserManagementService |

**Critical design decision:** The three log entities are **immutable** — they have no `UpdatedAt` field, no soft-delete flag. Once written, they are never modified. The only lifecycle operation is deletion by the `LogCleanupService`. `UserSync` is the exception — it IS mutable because it gets updated on every user lifecycle event.

#### Common

- **`IEntity<TKey>`** — Generic interface requiring `Id` (typed) and `CreatedAt`.
- **`IEntity`** — Non-generic convenience interface defaulting to `Guid` key.
- **`BaseEntity<TKey>`** — Abstract base class implementing `IEntity<TKey>`, providing `Id` and `CreatedAt` (defaults to `DateTime.UtcNow`).
- **`BaseEntity`** — Non-generic subclass defaulting to `Guid`.

#### Enums (9 total)

| Enum | Values | Used By |
|---|---|---|
| `Severity` | Debug=0, Info=1, Warning=2, Error=3, Critical=4 | `ErrorLog.Severity` |
| `LogSource` | Backend=0, Frontend=1 | `ErrorLog.Source` |
| `ErrorCategory` | ServerError=0, AuthFailure=1, ValidationFailure=2, NotFound=3, Conflict=4, FrontendError=5 | `ErrorLog.Category` |
| `ActionType` | 17 values grouped by tens (User=0-4, Role=10-13, App=20-22, Page=30-32, Action=40-43, Tenant=50-52, Branch=60-63, Auth=70-74) | `ActivityLog.ActionType` |
| `EntityType` | User=0, Role=1, App=2, Page=3, AppAction=4, Permission=5, Tenant=6, Branch=7 | `ActivityLog.EntityType` |
| `ActionStatus` | Success=0, Failed=1 | (available for future use) |
| `LoginEventType` | Login=0, Logout=1 | `LoginAudit.EventType` |
| `DeviceType` | Unknown=0, Desktop=1, Mobile=2, Tablet=3 | `LoginAudit.DeviceType` |
| `LoginFailureReason` | None=0, WrongPassword=1, AccountDeactivated=2, AccountDeleted=3, ExpiredTemporaryPassword=4, InvalidToken=5, ExpiredToken=6 | Used with `ErrorLog` when `Category=AuthFailure` |

---

### Application Layer (`LoggingService.Application`)

**Dependencies:** Only `LoggingService.Domain` + `MediatR 14.1.0`.

#### CQRS Pattern — 7 Queries (All Read-Only)

| Command | Response | Purpose |
|---|---|---|
| `ListErrorLogsCommand` | `ErrorLogListResponse` | Paginated error log list with filtering |
| `GetErrorLogByIdCommand` | `ErrorLogResponse` | Single error log by ID |
| `ListActivityLogsCommand` | `ActivityLogListResponse` | Paginated activity log list with filtering |
| `GetActivityLogByIdCommand` | `ActivityLogResponse` | Single activity log by ID |
| `ListLoginAuditsCommand` | `LoginAuditListResponse` | Paginated login audit list with filtering |
| `GetLoginAuditByIdCommand` | `LoginAuditResponse` | Single login audit by ID |
| `GetSummaryCommand` | `SummaryResponse` | Aggregated dashboard summary |

**Notable:** There are **zero write commands**. All writes happen via RabbitMQ consumers in the Infrastructure layer. This is a **purely read/query service** for the API surface.

#### Handlers

Every handler is a thin delegator following this pattern:
1. Constructor injection of a service interface
2. `Handle` method delegates to the service
3. `*ById` handlers check for null → throw `NotFoundException`
4. `*ById` handlers for ActivityLog and LoginAudit check tenant isolation → throw `UnauthorizedException`

#### Service Interfaces

| Interface | Methods |
|---|---|
| `IErrorLogService` | `GetLogsAsync(...)`, `GetByIdAsync(id)` |
| `IActivityLogService` | `GetLogsAsync(...)`, `GetByIdAsync(id)` |
| `ILoginAuditService` | `GetLogsAsync(...)`, `GetByIdAsync(id)`, `GetSummaryAsync(...)` |
| `IUserSyncService` | `GetByUserIdAsync(id)`, `UpsertAsync(user)`, `MarkDeletedAsync(id)`, `MarkRestoredAsync(id)` |

#### Common

- **`ApiResponse<T>`** — Generic response envelope with `Success`, `StatusCode`, `Message`, `Data`, `Errors`. Static factories: `Ok(data)`, `Fail(status, message, errors)`.
- **`ApiResponse`** — Non-generic version for void responses.
- **`NotFoundException`** — Thrown on missing entity lookups.
- **`UnauthorizedException`** — Thrown on tenant boundary violations.
- **`ValidationException`** — Carries a list of validation errors.

#### Pagination

All list commands share defaults: `Page=1`, `PageSize=20`, `SortOrder="desc"`. Response DTOs compute `TotalPages`, `HasPreviousPage`, `HasNextPage` as expression-bodied properties.

---

### Infrastructure Layer (`LoggingService.Infrastructure`)

**Dependencies:** `LoggingService.Application` + EF Core 10 + Npgsql + RabbitMQ.Client + Microsoft.Extensions.Hosting.

#### Persistence

**`LoggingDbContext`** — EF Core DbContext with four `DbSet` properties:
- `DbSet<ErrorLog>` → `errorlogs` table
- `DbSet<ActivityLog>` → `activitylogs` table
- `DbSet<LoginAudit>` → `loginaudits` table
- `DbSet<UserSync>` → `usersyncs` table

**Configuration Pattern:** Uses `ApplyConfigurationsFromAssembly()` to auto-discover `IEntityTypeConfiguration<>` classes. Each entity has a dedicated configuration class:

| Configuration | Table | Key | Required Columns | Indexes |
|---|---|---|---|---|
| `ActivityLogConfiguration` | `activitylogs` | `Id` (Guid) | Timestamp, ActionType, EntityType, ServiceName, Description, CreatedAt | `ix_activitylogs_timestamp`, `ix_activitylogs_tenantid_timestamp`, `ix_activitylogs_actiontype`, `ix_activitylogs_userid` |
| `ErrorLogConfiguration` | `errorlogs` | `Id` (Guid) | Timestamp, Severity, Source, Category, ServiceName, Message, CreatedAt | `ix_errorlogs_timestamp`, `ix_errorlogs_tenantid_timestamp`, `ix_errorlogs_servicename`, `ix_errorlogs_category`, `ix_errorlogs_severity` |
| `LoginAuditConfiguration` | `loginaudits` | `Id` (Guid) | Timestamp, EventType, Email, DeviceType, CreatedAt | `ix_loginaudits_timestamp`, `ix_loginaudits_tenantid_timestamp`, `ix_loginaudits_email`, `ix_loginaudits_userid` |
| `UserSyncConfiguration` | `usersyncs` | `UserId` (string, 450 chars) | UserId, TenantId, Email, UserName, IsActive, IsDeleted, IsSuperAdmin, Roles, LastSyncedAt, CreatedAt, UpdatedAt | `ix_usersyncs_tenantid_isactive_isdeleted`, `ix_usersyncs_email` |

All `CreatedAt` columns use `HasDefaultValueSql("NOW()")` for database-level defaults.

**Migrations:** Single migration `20260406125940_InitialCreate` creates all four tables and 14 indexes.

#### RabbitMQ Consumers (8 Total)

All consumers use **fanout exchanges**, **durable queues**, **manual acknowledgment**, and **scope-per-message** pattern (fresh DI scope per message for DbContext isolation).

| Consumer | Exchange | Queue | Action |
|---|---|---|---|
| `ErrorLogConsumer` | `logging.error` | `logging.errorlog` | Deserializes `ErrorLogEventDto` → creates `ErrorLog` entity → `SaveChangesAsync` |
| `ActivityLogConsumer` | `logging.activity` | `logging.activitylog` | Deserializes `ActivityLogEventDto` → creates `ActivityLog` entity → `SaveChangesAsync` |
| `LoginAuditConsumer` | `logging.loginaudit` | `logging.loginaudit` | Deserializes `LoginAuditEventDto` → creates `LoginAudit` entity → `SaveChangesAsync` |
| `UserCreatedConsumer` | `UserManagementService.Application.Events.UserCreatedEvent` | `companysettings.user.created` | Deserializes → calls `IUserSyncService.UpsertAsync()` |
| `UserUpdatedConsumer` | `UserManagementService.Application.Events.UserUpdatedEvent` | `companysettings.user.updated` | Deserializes → calls `IUserSyncService.UpsertAsync()` |
| `UserStatusChangedConsumer` | `UserManagementService.Application.Events.UserStatusChangedEvent` | `companysettings.user.statuschanged` | Fetches existing user → mutates `IsActive` → `UpsertAsync()` |
| `UserDeletedConsumer` | `UserManagementService.Application.Events.UserDeletedEvent` | `companysettings.user.deleted` | Calls `IUserSyncService.MarkDeletedAsync(userId)` |
| `UserRestoredConsumer` | `UserManagementService.Application.Events.UserRestoredEvent` | `companysettings.user.restored` | Calls `IUserSyncService.MarkRestoredAsync(userId)` |

**`RabbitMqConsumerHostedService`** — `IHostedService` that creates a shared `IConnection` with automatic recovery (10s reconnect interval), instantiates all 8 consumers, and manages their lifecycle.

#### Service Implementations

| Service | Key Methods | Notes |
|---|---|---|
| `ErrorLogService` | `GetLogsAsync()` with Severity/Source/Category filtering, `GetByIdAsync()` | Dynamic LINQ query building |
| `ActivityLogService` | `GetLogsAsync()` with ActionType/EntityType filtering, `GetByIdAsync()` | Dynamic LINQ query building |
| `LoginAuditService` | `GetLogsAsync()`, `GetByIdAsync()`, `GetSummaryAsync()` | `GetSummaryAsync` aggregates across all 3 log tables with tenant isolation |
| `UserSyncService` | `GetByUserIdAsync()`, `UpsertAsync()`, `MarkDeletedAsync()`, `MarkRestoredAsync()` | Insert-or-update pattern with soft delete support |

#### Background Services

| Service | Trigger | Action |
|---|---|---|
| `LogCleanupService` | Daily at midnight UTC | `ExecuteDeleteAsync()` on ErrorLogs, ActivityLogs, LoginAudits older than retention days (default 30 each) |
| `RabbitMqConsumerHostedService` | App startup | Starts all 8 RabbitMQ consumers |

#### Seeding

**`DatabaseSeeder`** — Seeds a hardcoded SuperAdmin user (`UserId: e71a9f33-1832-4055-a852-7beb05ec44d0`) into `UserSync` on first startup. Same ID used across UserManagementService and CompanySettingsService for cross-service admin consistency.

---

### API Layer (`LoggingService.API`)

**Dependencies:** Infrastructure + Application + JWT Auth + Serilog + OpenApi.

#### Program.cs Pipeline (in order)

```
1. GlobalExceptionHandlingMiddleware   ← catches all unhandled exceptions
2. OpenApi (dev only)
3. HttpsRedirection
4. CORS ("AllowAll")
5. Authentication (JWT)
6. UserSyncValidationMiddleware        ← validates account is active
7. Authorization
8. Health Checks (/health)
9. Controllers
```

**Startup:** Auto-runs EF Core migrations (`MigrateAsync()`) and seeds SuperAdmin (`DatabaseSeeder.SeedAsync()`).

#### Controllers & Endpoints

| Controller | Route | Auth | Endpoints |
|---|---|---|---|
| `ErrorLogsController` | `api/errorlogs` | Mixed | `GET /` (SuperAdmin only), `GET /{id}` (SuperAdmin only), `POST /frontend` (AllowAnonymous) |
| `ActivityLogsController` | `api/activitylogs` | `[Authorize]` | `GET /` (list with filtering), `GET /{id}` (single by ID) |
| `LoginAuditsController` | `api/loginaudits` | `[Authorize]` | `GET /` (list with filtering), `GET /{id}` (single by ID) |
| `SummaryController` | `api/summary` | `[Authorize]` | `GET /` (dashboard summary) |

#### Authorization Model

- **JWT Bearer tokens** issued by `UserManagementService` (Issuer: `UserManagementService`, Audience: `WorkforcePlatform`)
- **Claim-based authorization:** `IsSuperAdmin`, `TenantId`, `BranchId`, `UserId`, `email`
- **Tenant isolation:** Non-SuperAdmin users have their `tenantId`/`branchId` overridden to JWT claims — they can only see their own tenant's data
- **ErrorLogs are SuperAdmin-only** for reading — only the `POST /frontend` endpoint is `[AllowAnonymous]`
- **`POST /api/errorlogs/frontend`** — Allows unauthenticated frontend clients to report errors (login page errors, OTP errors, etc.). If a JWT is present, the log is enriched with user context

#### Middleware

**`GlobalExceptionHandlingMiddleware`** — First in pipeline. Maps exceptions:
- `NotFoundException` → 404
- `ValidationException` → 400 (with error list)
- `UnauthorizedException` / `UnauthorizedAccessException` → 403
- All others → 500

**`UserSyncValidationMiddleware`** — After auth, before authorization. Validates that the authenticated user's account exists, is active, and is not soft-deleted in the `UserSync` table. **SuperAdmins bypass this check.** Returns 401 if account is inactive/removed. This is a **defense-in-depth** measure — JWTs are stateless and may remain valid after account deactivation.

---

## Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LoggingDb;Username=postgres;Password=Aneesh@123"
  },
  "Jwt": {
    "Key": "your-super-secret-key-at-least-32-chars-long",
    "Issuer": "UserManagementService",
    "Audience": "WorkforcePlatform"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Retention": {
    "ErrorLogDays": 30,
    "ActivityLogDays": 30,
    "LoginAuditDays": 30
  },
  "Urls": "http://localhost:5270"
}
```

---

## Building and Running

### Prerequisites
- **.NET 10 SDK**
- **PostgreSQL** running on `localhost:5432`
- **RabbitMQ** running on `localhost`

### Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API (auto-migrates DB and seeds SuperAdmin on startup)
dotnet run --project LoggingService.API

# Run EF Core migrations separately (if needed)
dotnet ef database update --project LoggingService.Infrastructure

# Add a new migration
dotnet ef migrations add <MigrationName> --project LoggingService.Infrastructure
```

### Default URL
- `http://localhost:5270` (from appsettings.json)
- Development profiles: `http://localhost:5209` and `https://localhost:7101`

---

## Key Architectural Patterns

| Pattern | Where Used |
|---|---|
| **Clean Architecture** | Strict layer dependencies; no cross-layer leaks |
| **CQRS with MediatR** | All API queries go through MediatR handlers; zero write commands |
| **Event-Driven Writes** | All writes happen via 8 RabbitMQ consumers |
| **Immutable Logs** | Log entities have no UpdatedAt; write-once, read-many |
| **Multi-Tenancy** | TenantId/BranchId on all entities with composite indexes |
| **Tenant Isolation** | Handler-level checks for non-SuperAdmin users |
| **Scope-per-Message** | Fresh DI scope per RabbitMQ message for DbContext isolation |
| **Manual Ack/Nack** | RabbitMQ messages acked on success, nacked with requeue on failure |
| **Soft Delete** | UserSync uses IsDeleted flag |
| **Bulk Deletes** | LogCleanupService uses EF Core `ExecuteDeleteAsync()` |
| **Upsert Pattern** | UserSyncService.UpsertAsync() does fetch-check-insert-or-update |
| **Defense-in-Depth Auth** | JWT validation + UserSyncValidationMiddleware for real-time account status |
| **Structured Responses** | ApiResponse<T> envelope with Ok/Fail factories |
| **Database Defaults** | `HasDefaultValueSql("NOW()")` for CreatedAt columns |
| **Auto-Migration** | `MigrateAsync()` on startup ensures schema is always current |

---

## Cross-Service Communication

### Incoming Events (Consumed)

| Source Service | Event | Exchange | Purpose |
|---|---|---|---|
| Any service | Error log event | `logging.error` | Record error logs |
| Any service | Activity log event | `logging.activity` | Record activity logs |
| Any service | Login audit event | `logging.loginaudit` | Record login audits |
| UserManagementService | UserCreatedEvent | `UserManagementService.Application.Events.UserCreatedEvent` | Sync new user |
| UserManagementService | UserUpdatedEvent | `UserManagementService.Application.Events.UserUpdatedEvent` | Update synced user |
| UserManagementService | UserStatusChangedEvent | `UserManagementService.Application.Events.UserStatusChangedEvent` | Toggle user active status |
| UserManagementService | UserDeletedEvent | `UserManagementService.Application.Events.UserDeletedEvent` | Soft-delete synced user |
| UserManagementService | UserRestoredEvent | `UserManagementService.Application.Events.UserRestoredEvent` | Restore synced user |

---

## Notable Design Decisions

1. **No FluentValidation** — The Application layer has no validation library. Validation is handled manually in handlers or at the API layer.
2. **No IPipelineBehavior** — No MediatR pipeline behaviors (no logging, validation, or caching pipelines).
3. **No Write APIs** — The REST API is read-only. All writes are event-driven via RabbitMQ.
4. **Frontend Error Endpoint is AllowAnonymous** — `POST /api/errorlogs/frontend` accepts errors from unauthenticated clients (login pages, OTP screens).
5. **Error Logs Globally Visible** — Unlike ActivityLog and LoginAudit, `GetErrorLogByIdCommand` has no tenant authorization check.
6. **SuperAdmin Hardcoded** — The SuperAdmin user ID (`e71a9f33-1832-4055-a852-7beb05ec44d0`) is hardcoded for cross-service consistency.
7. **Cleanup at Midnight UTC** — `LogCleanupService` calculates delay to next midnight UTC, then runs daily.
8. **Roles as JSON String** — `UserSync.Roles` is stored as a JSON string array (`"[]"`) rather than a relational table.
9. **CORS AllowAll** — Fully permissive CORS, acceptable for internal microservices but should be tightened for production.
