# Test Infrastructure Guide

This directory contains helper classes for writing maintainable unit tests with dependency injection.

## Overview

The test infrastructure provides a DI-based testing pattern that:
- Automatically mocks all Repository and Service dependencies
- Allows testing a single Service in complete isolation
- Simplifies test setup by reducing boilerplate code
- Makes tests more maintainable as dependencies change

## When to Use This Pattern

### ✅ Use `ServiceTestBase` for:

1. **Service-level tests** - Testing classes in `Traincrew_MultiATS_Server/Services/`
   - Example: `RouteService`, `TrainService`, `InterlockingService`
   - These classes have many dependencies on Repositories and other Services
   - The DI container automatically resolves all dependencies as mocks

2. **Complex dependency chains** - When the class under test has 5+ dependencies
   - Example: `RendoService` has 20 Repository dependencies
   - Manually creating 20 mock instances is error-prone and verbose

3. **Service-to-Service dependencies** - When Services depend on other Services
   - The new interface-based DI registration makes this possible
   - Example: `TrainService` depends on `TrackCircuitService`, `SignalService`, etc.

### ❌ Don't use `ServiceTestBase` for:

1. **DbInitializer tests** - These are not registered as Services in DI
   - Use manual Mock<> setup as currently done
   - Example: `StationDbInitializerTest`, `TrainDbInitializerTest`

2. **Pure functions without dependencies** - Methods that don't need DI
   - Example: `RendoService.CalculateLeverRelayState()` - pure computation
   - Current pattern with `null!` is acceptable for these cases

3. **Integration tests (IT)** - Tests in `Traincrew_MultiATS_Server.IT/`
   - Use real DbContext and real database
   - Don't mock repositories in integration tests

## How to Use

### Basic Pattern

```csharp
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Traincrew_MultiATS_Server.Services;
using Traincrew_MultiATS_Server.UT.TestHelpers;

namespace Traincrew_MultiATS_Server.UT.Services;

public class RouteServiceTest : ServiceTestBase
{
    private readonly Mock<IRouteRepository> _routeRepositoryMock = new();

    protected override void ConfigureTestServices(ServiceCollection services)
    {
        // Step 1: Register ALL mocks (45 Repositories + 21 Services)
        services.AddAllMocks();

        // Step 2: Replace specific mocks you want to configure
        services.ReplaceMock(_routeRepositoryMock);

        // Step 3: Use the REAL implementation for the service under test
        services.UseRealService<IRouteService, RouteService>();
    }

    [Fact]
    public async Task GetActiveRoutes_ShouldReturnActiveRoutes()
    {
        // Arrange
        _routeRepositoryMock
            .Setup(r => r.GetIdsWhereRouteRelayWithoutSwitchingMachineIsRaised())
            .ReturnsAsync([1UL, 2UL, 3UL]);

        var service = GetService<IRouteService>();

        // Act
        var result = await service.GetActiveRoutes();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
}
```

## Available Extension Methods

### `AddAllMocks()`
Registers mock instances for all 41 Repositories and 21 Services.

```csharp
services.AddAllMocks();
```

This creates default mocks for:
- All Repository interfaces (IRouteRepository, ITrainRepository, etc.)
- All Service interfaces (IRouteService, ITrainService, etc.)
- Common dependencies (ILogger<T>)

### `ReplaceMock<TInterface>(Mock<TInterface> mock)`
Replaces a previously registered mock with your configured instance.

```csharp
var mockRepo = new Mock<IRouteRepository>();
mockRepo.Setup(r => r.GetById(1)).ReturnsAsync(someRoute);
services.ReplaceMock(mockRepo);
```

Use this when you need to:
- Setup specific behavior (`Setup()`)
- Verify method calls (`Verify()`)
- Return specific values

### `UseRealService<TInterface, TImplementation>()`
Replaces a mock with the real service implementation.

```csharp
services.UseRealService<IRouteService, RouteService>();
```

**Important:** Only use this for the service you're testing. All its dependencies remain mocked.

## Complete Example

See `Services/RouteServiceTest.cs` for a complete working example showing:
- How to configure test services
- How to setup mock behavior
- How to retrieve and test the service
- How to verify mock interactions

## Migration Guide

### Before (Manual Mock Construction)
```csharp
public class MyServiceTest
{
    private readonly Mock<IRepo1> _repo1Mock = new();
    private readonly Mock<IRepo2> _repo2Mock = new();
    private readonly Mock<IRepo3> _repo3Mock = new();
    // ... 17 more mocks ...

    [Fact]
    public async Task MyTest()
    {
        var service = new MyService(
            _repo1Mock.Object,
            _repo2Mock.Object,
            _repo3Mock.Object,
            // ... 17 more .Object calls ...
        );

        // Test logic...
    }
}
```

### After (DI-Based Pattern)
```csharp
public class MyServiceTest : ServiceTestBase
{
    private readonly Mock<IRepo1> _repo1Mock = new();

    protected override void ConfigureTestServices(ServiceCollection services)
    {
        services.AddAllMocks();
        services.ReplaceMock(_repo1Mock); // Only mock what you need to setup
        services.UseRealService<IMyService, MyService>();
    }

    [Fact]
    public async Task MyTest()
    {
        var service = GetService<IMyService>();
        // Test logic...
    }
}
```

## Benefits

1. **Less boilerplate** - No need to pass 20 `.Object` parameters
2. **More maintainable** - Adding a dependency to a Service doesn't break tests
3. **True isolation** - Test only the service under test, all dependencies are mocked
4. **Type-safe** - Compiler ensures all dependencies are satisfied
5. **Follows best practices** - Uses DI container like production code

## Troubleshooting

### "Service type X has not been registered"
Make sure you called `services.AddAllMocks()` in `ConfigureTestServices()`.

### "Cannot resolve service for type IMyService"
Use `services.UseRealService<IMyService, MyService>()` to register the implementation.

### "Mock returns null unexpectedly"
Replace the auto-generated mock with your configured one:
```csharp
services.ReplaceMock(myConfiguredMock);
```

### Integration test failures
Don't use `ServiceTestBase` for integration tests. They should use real dependencies.
