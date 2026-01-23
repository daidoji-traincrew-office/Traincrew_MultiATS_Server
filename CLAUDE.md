# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Traincrew_MultiATS_Server is a multi-ATS server system for train crew operations (Traincrew運転会). It provides real-time SignalR communication for interlocking systems, train operations, and passenger applications.

## Build, Test, and Run Commands

### Database Setup

Start PostgreSQL with Docker:
```bash
cd Database
docker compose up -d
```

Apply database migrations:
```bash
atlas migrate apply --env local
```

Create a new migration after editing `schema.sql`:
```bash
atlas migrate diff --env local <migration_name>
```

Reset database (deletes all data):
```bash
cd Database
docker compose down -v
```

### Running the Server

Run crew server (ATS, interlocking, TID, commander desk):
```bash
cd Traincrew_MultiATS_Server.Crew
dotnet run -lp https
```

Run passenger server:
```bash
cd Traincrew_MultiATS_Server.Passenger
dotnet run -lp https
```

Build solution:
```bash
dotnet build
```

### Running Tests

Run all tests:
```bash
dotnet test
```

Run unit tests only:
```bash
dotnet test Traincrew_MultiATS_Server.UT
```

Run integration tests only:
```bash
dotnet test Traincrew_MultiATS_Server.IT
```

Run a specific test:
```bash
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

### Project Structure

The solution consists of 6 projects:

- **Traincrew_MultiATS_Server.Crew**: Web API/SignalR server for crew operations (ATS, interlocking, TID, commander desk)
- **Traincrew_MultiATS_Server.Passenger**: Web API/SignalR server for passenger applications
- **Traincrew_MultiATS_Server**: Core business logic library (models, repositories, services, hubs)
- **Traincrew_MultiATS_Server.Common**: Shared schema definitions between client and server (SignalR contracts, body models)
- **Traincrew_MultiATS_Server.IT**: Integration tests
- **Traincrew_MultiATS_Server.UT**: Unit tests

Dependency flow: Crew/Passenger → Core Library → Common

### Layered Architecture

The application follows a strict layered architecture:

```
Controller/Hub → Service → Repository → DbContext → PostgreSQL
```

**Access rules:**
- ✅ Controllers/Hubs call Services only (never Repositories or DbContext directly)
- ✅ Services orchestrate business logic using multiple Repositories
- ✅ Only Repositories access DbContext
- ❌ Controllers/Hubs must NOT access DbContext directly
- ❌ Services must NOT access DbContext directly

### Repository Pattern

Each entity has a dedicated Repository with an interface (e.g., `IRouteRepository`) and implementation (e.g., `RouteRepository`).

**Repository method naming conventions:**
- `GetById(id)`, `GetByIds(ids)` - fetch entity objects
- `GetAll()` - fetch all entities
- `GetWhereXXX()` - fetch entities matching condition
- `GetIdsWhereXXX()` - fetch only IDs matching condition
- `UpdateXXX()`, `SetXXXWhereYYY()` - bulk updates using ExecuteUpdateAsync
- `DeleteByXXX()` - bulk deletes using ExecuteDeleteAsync
- `GetAllForKey()`, `GetAllIdsForKey()` - fetch Dictionary keyed by Key property (no filtering)
- `GetForKeyById(id)`, `GetIdsForKeyById(id)` - fetch Dictionary keyed by Key property (with filtering)

**Dictionary return types:**
- When returning Dictionary, use "For" in the key portion instead of "By"
- NO filtering: use `GetAllForKey` (full entities) or `GetAll{Property}ForKey` (e.g., `GetAllIdsForKey`, `GetAllNamesForKey`)
- WITH filtering: use `GetForKeyByFilter` (no "All" prefix)
- This distinguishes from entity-returning methods: `GetByKey` returns entity, `GetAllForKey` returns Dictionary keyed by Key

**State entities:**
- Entities with state (data that changes during operations) have separate State tables (e.g., `Route` has `RouteState`, `Lever` has `LeverState`)
- When fetching entities with state, ALWAYS Include the state: `.Include(r => r.RouteState)`
- Method names don't need `WithState` suffix since state is always included

### SignalR Hub Pattern

Hubs use `TypedSignalR.Client` for type-safe contracts:
- `IXxxHubContract` - server methods callable by clients
- `IXxxClientContract` - client methods callable by server
- Hubs inherit `Hub<IXxxClientContract>` and implement `IXxxHubContract`
- Hub methods delegate to Services immediately

### Scheduler Pattern

Background tasks that push data to SignalR clients at regular intervals:
- Extend the `Scheduler` base class
- Override `Interval` property (milliseconds)
- Override `ExecuteTaskAsync` method
- Register as Singleton in Program.cs

Main schedulers:
- `InterlockingHubScheduler` (250ms) - interlocking data
- `TrainScheduler` - train data
- `SignalScheduler` - signal data
- `SwitchingMachineScheduler` - switching machine data

## Database Schema Management

**IMPORTANT:** This project uses Atlas for database migrations.

Workflow:
1. Edit `Database/schema.sql` manually
2. Generate migration: `atlas migrate diff --env local <meaningful_name>`
3. Apply migration: `atlas migrate apply --env local`

**Never:**
- ❌ Edit migration files manually (edit schema.sql instead)
- ❌ Use EF Core migrations (Atlas is used exclusively)

Schema lives in `Database/schema.sql`. Migrations are auto-generated in `Database/migrations/`.

## Coding Conventions

### Naming Conventions

- Classes: PascalCase (`RouteService`, `ApplicationDbContext`)
- Interfaces: IPascalCase (`IRouteRepository`)
- Methods: PascalCase (`GetRouteById`)
- Private fields: `_camelCase`
- Local variables: camelCase

**Collection naming:**
- Single entity: `route`, `lever`, `signal`
- List: `routes`, `levers`, `signals` (plural)
- Dictionary: `routeById`, `leverByName` (TValueByTKey pattern)
- Dictionary with List values: `routesBySignalId` (TValuesByTKey pattern)

### Async Programming

- Always use `async Task` or `async Task<T>`
- Always use `await` keyword
- ❌ NEVER use `.Result` or `.Wait()` (deadlock risk)

### Declarative Code Style

Prefer LINQ method chains over imperative loops:

```csharp
// ✅ Good: Declarative
var raisedRouteIds = routes
    .Where(r => r.RouteState.IsLeverRelayRaised == RaiseDrop.Raise)
    .Select(r => r.Id)
    .ToList();

// ❌ Bad: Imperative
var raisedRouteIds = new List<ulong>();
foreach (var route in routes)
{
    if (route.RouteState.IsLeverRelayRaised == RaiseDrop.Raise)
    {
        raisedRouteIds.Add(route.Id);
    }
}
```

### Collection Literals (C# 12)

Use collection literals `[]` for all collection initialization:

```csharp
// ✅ Good: Collection literals
var routes = [route1, route2, route3];
List<Route> emptyRoutes = [];
return [];  // Type inferred from return type

// ❌ Bad: Old style
var routes = new List<Route> { route1, route2, route3 };
List<Route> emptyRoutes = new();
```

**Exception:** Use explicit type when specific collection implementation is required (e.g., `new HashSet<T>()`, `new Queue<T>()`).

### Target-typed new (C# 9+)

Omit type in `new` expressions when type can be inferred:

```csharp
// ✅ Good: Type inferred
Route route = new();
RouteData data = new() { Id = 1, Name = "R1" };

// ❌ Bad: Redundant type
Route route = new Route();
RouteData data = new RouteData() { Id = 1 };
```

**Note:** When using `var`, the type must be explicit: `var route = new Route();`

### ExecuteUpdate/ExecuteDelete

For bulk updates/deletes based on conditions, use EF Core's `ExecuteUpdateAsync` / `ExecuteDeleteAsync` to avoid SELECT then UPDATE/DELETE round trips.

Use `GeneralRepository` for single entity updates or when you need to read values before/after update.

### N+1 Query Prevention

Always use `.Include()` to eager-load related entities or fetch IDs in bulk and query once with `.Contains()`.

## Hub Implementation Workflow

When adding a new Hub method:

1. **Define interfaces** in `Traincrew_MultiATS_Server.Common/Contract/`:
   - `IXxxHubContract` (server methods)
   - `IXxxClientContract` (client callbacks)

2. **Define body models** in `Traincrew_MultiATS_Server.Common/Models/`

3. **Implement Hub** in `Traincrew_MultiATS_Server/Hubs/`:
   - Inherit `Hub<IXxxClientContract>` and implement `IXxxHubContract`
   - Delegate to Service immediately

4. **Implement Service** in `Traincrew_MultiATS_Server/Services/`:
   - Orchestrate multiple Repositories
   - Handle business logic
   - Use `IMutexRepository` for critical sections

5. **Implement Repository** in `Traincrew_MultiATS_Server/Repositories/`:
   - Define interface `IXxxRepository`
   - Implement data access using `ApplicationDbContext`

6. **Register in DI** in `Program.cs`:
   - Add Repository as Scoped
   - Add Service as Scoped
   - Map Hub endpoint with `app.MapHub<XxxHub>("/hub/xxx")`

## Domain Model

### Primary Domains

- **Interlocking (連動)**: Signal safety interlocking control (`Route`, `Signal`, `SwitchingMachine`, `TrackCircuit`, `Lever`)
- **Train (列車)**: Train operations (`Train`, `TrainCar`, `TrainDiagram`, `TrainState`)
- **Station (駅・停車場)**: Station master data (`Station`)
- **TTC Window (TTC窓)**: Train number window management (`TtcWindow`, `TtcWindowLink`)
- **Operation Information (操作情報)**: Operation history (`OperationInformation`)

### Key Design Principles

- Primary keys use `ulong`
- Enums stored as Postgres ENUMs (mapped in `EnumTypeMapper.cs`)
- Navigation properties loaded explicitly with `.Include()`
- State tables separate dynamic state from static master data

## Test Strategy

- **Unit Tests (UT)**: Independent logic, hosted services - `Traincrew_MultiATS_Server.UT`
- **Integration Tests (IT)**: Hubs, Controllers, multi-layer logic - `Traincrew_MultiATS_Server.IT`

Test naming: `{MethodName}_{Condition}_{ExpectedResult}`

Use AAA pattern: Arrange, Act, Assert

## Important Documentation

Comprehensive documentation exists in the `Docs/` folder:
- `01_環境構築ガイド.md` - Environment setup
- `02_Hub実装チュートリアル.md` - Hub implementation tutorial
- `03_コーディング規約.md` - Coding conventions
- `04_アーキテクチャ設計.md` - Architecture design
- `05_テスト戦略.md` - Test strategy

Refer to these documents for detailed information not covered here.

## Technology Stack

- ASP.NET Core 8.0
- Entity Framework Core 8.0
- PostgreSQL 16.4
- SignalR with TypedSignalR.Client
- Atlas for database migrations
- OpenIddict for authentication
- xUnit, Moq for testing