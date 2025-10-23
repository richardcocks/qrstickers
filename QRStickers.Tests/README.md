# QRStickers.Tests

Unit testing project for QRStickers application using xUnit, Moq, and EF Core In-Memory database.

## Test Framework Stack

- **xUnit 2.9.0** - Test framework (modern, industry standard for .NET Core)
- **Moq 4.20.70** - Mocking framework for dependencies
- **EF Core In-Memory 9.0.10** - In-memory database provider for fast, isolated tests
- **Microsoft.NET.Test.Sdk 17.11.0** - Test SDK for running tests

## Project Structure

```
QRStickers.Tests/
├── Helpers/
│   ├── InMemoryDbContextFactory.cs  # Creates isolated test databases
│   └── TestDataBuilder.cs           # Builds test entities with sensible defaults
└── Services/
    ├── TemplateMatchingServiceTests.cs   # Tests for template matching logic
    └── ImageUploadValidatorTests.cs      # Tests for image validation
```

## Running Tests

### Visual Studio
- Open Test Explorer (Test → Test Explorer)
- Click "Run All" to execute all tests
- Tests will appear grouped by namespace

### Command Line
```bash
dotnet test
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~TemplateMatchingServiceTests"
```

### Run single test method
```bash
dotnet test --filter "FullyQualifiedName~FindTemplateForDevice_WithMatchingConnectionDefault_ReturnsCorrectTemplate"
```

### With verbose output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Helpers

### InMemoryDbContextFactory

Creates isolated in-memory databases for each test. Each call creates a new database with a unique GUID name, ensuring test isolation.

```csharp
using var db = InMemoryDbContextFactory.Create();
// Database is ready to use, no migrations needed
```

### TestDataBuilder

Provides methods to create test entities with sensible defaults:

```csharp
var user = TestDataBuilder.CreateUser(id: "user-1", email: "test@example.com");
var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id, displayName: "Test");
var template = TestDataBuilder.CreateTemplate(name: "My Template", isSystemTemplate: true);
var device = TestDataBuilder.CreateDevice(connectionId: connection.Id, productType: "switch");
```

All properties can be overridden, but defaults are provided for quick test setup.

## Test Coverage

### TemplateMatchingService ✅
- ✅ Matching connection default by ProductType
- ✅ Fallback to system template when no default
- ✅ Null/empty ProductType handling
- ✅ Case-insensitive ProductType matching
- ✅ System template preference for fallback
- ✅ Alternate templates retrieval
- ✅ Exclude template filtering
- ✅ No templates available error handling

### ImageUploadValidator ✅
- ✅ Valid PNG/JPEG image uploads
- ✅ Connection ownership validation
- ✅ Data URI format validation
- ✅ MIME type validation (allowed: PNG, JPEG, WebP, SVG)
- ✅ Dimension limits (max 900×900px)
- ✅ Invalid dimensions (zero, negative)
- ✅ Name validation (required, max 200 chars)
- ✅ Quota information retrieval

### TODO: Future Test Coverage
- [ ] QRCodeGenerationService
- [ ] DeviceExportHelper
- [ ] PdfExportService
- [ ] Extract and test PageModel business logic

## Testing Patterns

### Arrange-Act-Assert Pattern

All tests follow the AAA pattern:

```csharp
[Fact]
public async Task TestMethod_Condition_ExpectedResult()
{
    // Arrange - Set up test data
    var user = TestDataBuilder.CreateUser();
    var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
    _dbContext.Users.Add(user);
    _dbContext.Connections.Add(connection);
    await _dbContext.SaveChangesAsync();

    // Act - Perform the action being tested
    var result = await _service.SomeMethod(connection.Id);

    // Assert - Verify the outcome
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
}
```

### Test Class Structure

Each test class implements `IDisposable` to clean up resources:

```csharp
public class ServiceTests : IDisposable
{
    private readonly QRStickersDbContext _dbContext;
    private readonly ServiceUnderTest _service;

    public ServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _service = new ServiceUnderTest(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
```

### Theory Tests with InlineData

Use `[Theory]` for parameterized tests:

```csharp
[Theory]
[InlineData(901, 100)] // Width too large
[InlineData(100, 901)] // Height too large
public async Task ValidateUpload_DimensionsTooLarge_ReturnsFail(int width, int height)
{
    // Test implementation
}
```

## Best Practices

1. **Test Isolation**: Each test uses a fresh in-memory database (unique GUID name)
2. **Descriptive Names**: Method names follow pattern `MethodName_Condition_ExpectedResult`
3. **One Assert Per Concept**: Tests verify one logical concept, but may have multiple Assert statements
4. **Dispose Resources**: All tests implement IDisposable to clean up DbContext and services
5. **Test Data Builders**: Use TestDataBuilder for consistent, maintainable test data
6. **Async Tests**: All database operations use async/await for realistic testing

## Adding New Tests

### 1. Create Test Class

```csharp
using QRStickers.Services;
using QRStickers.Tests.Helpers;

namespace QRStickers.Tests.Services;

public class YourServiceTests : IDisposable
{
    private readonly QRStickersDbContext _dbContext;
    private readonly YourService _service;

    public YourServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _service = new YourService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task YourMethod_SomeCondition_ExpectedOutcome()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

### 2. Add Test Data Builder Methods (if needed)

If you need to create new entity types, add helper methods to `TestDataBuilder.cs`.

### 3. Run Tests

```bash
dotnet test
```

## Continuous Integration

Tests are designed to run in CI/CD pipelines:
- Fast execution (in-memory database)
- No external dependencies
- Deterministic results
- Parallel execution safe (isolated databases)

## Troubleshooting

### Tests failing with "Connection not found"
- Ensure you add entities to DbContext and call `SaveChangesAsync()` before testing

### Tests failing with "No templates available"
- Seed required system templates in your test setup

### Slow test execution
- In-memory database should be fast; check for N+1 queries or missing `.AsNoTracking()`

### Build errors referencing old properties
- Entity models may have changed; check TestDataBuilder matches current entity structure
- Common issue: Cached* entities use different property names than expected
