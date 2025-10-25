# Controller Quick Reference

Quick reference guide for working with API controllers in the QRStickers application.

## Controller Locations

All controllers are located in: `src/Controllers/`

## Available Controllers

### DeviceExportController
**File:** `DeviceExportController.cs`
**Route:** `/api/export`
**Purpose:** Single and bulk device export operations

```csharp
GET  /api/export/device/{deviceId}    // Single device export
POST /api/export/bulk-devices         // Bulk device export
```

### TemplateController
**File:** `TemplateController.cs`
**Route:** `/api/templates`
**Purpose:** Template matching operations

```csharp
GET  /api/templates/match              // Find matching template
```

### ExportController
**File:** `ExportController.cs`
**Route:** `/api/export/pdf`
**Purpose:** PDF generation

```csharp
POST /api/export/pdf/bulk              // Generate PDF
```

### UsageController
**File:** `UsageController.cs`
**Route:** `/api/usage`
**Purpose:** Usage tracking

```csharp
POST /api/usage/track                  // Track template/image usage
```

### ImageController
**File:** `ImageController.cs`
**Route:** `/api/images`
**Purpose:** Image upload and management

```csharp
POST   /api/images/upload              // Upload image
GET    /api/images                     // List images
DELETE /api/images/{id}                // Delete image
```

## Adding a New Controller

### Step 1: Create Controller File
```bash
# Create new file in src/Controllers/
touch src/Controllers/MyController.cs
```

### Step 2: Controller Template
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QRStickers.Controllers;

/// <summary>
/// Controller description
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;

    public MyController(ILogger<MyController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Action description
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(new { message = "Hello" });
    }
}
```

### Step 3: Test the Endpoint
```bash
# Build the project
dotnet build

# Run the project
dotnet run

# Test with curl
curl -X GET https://localhost:7044/api/my
```

## Common Patterns

### Constructor Injection
```csharp
public class MyController : ControllerBase
{
    private readonly IMyService _service;
    private readonly ILogger<MyController> _logger;

    public MyController(IMyService service, ILogger<MyController> logger)
    {
        _service = service;
        _logger = logger;
    }
}
```

### HTTP Verb Attributes
```csharp
[HttpGet]                  // GET request
[HttpPost]                 // POST request
[HttpPut]                  // PUT request
[HttpDelete]               // DELETE request
[HttpPatch]                // PATCH request
```

### Route Parameters
```csharp
// Path parameter
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)

// Query parameter
[HttpGet]
public async Task<IActionResult> Get([FromQuery] string filter)

// Body parameter
[HttpPost]
public async Task<IActionResult> Create([FromBody] MyModel model)
```

### Return Types
```csharp
return Ok(data);                    // 200 OK
return Created("/api/my/1", data);  // 201 Created
return NoContent();                 // 204 No Content
return BadRequest(error);           // 400 Bad Request
return Unauthorized();              // 401 Unauthorized
return Forbid();                    // 403 Forbidden
return NotFound();                  // 404 Not Found
return Problem(detail, 500);        // 500 Internal Server Error
```

### Error Handling
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    try
    {
        var data = await _service.GetByIdAsync(id);
        return Ok(data);
    }
    catch (NotFoundException)
    {
        return NotFound();
    }
    catch (UnauthorizedAccessException)
    {
        return Forbid();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting item {Id}", id);
        return Problem(detail: ex.Message, statusCode: 500);
    }
}
```

### Authorization
```csharp
// Require authentication for entire controller
[Authorize]
public class MyController : ControllerBase

// Require specific role
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase

// Require specific policy
[Authorize(Policy = "CanEditTemplates")]
public class TemplateController : ControllerBase

// Allow anonymous access to specific action
[AllowAnonymous]
[HttpGet("public")]
public IActionResult GetPublic()
```

## Helper Methods in Services

Helper methods have been extracted to service classes:

**DeviceExportHelper** (`src/Services/DeviceExportHelper.cs`):
```csharp
// Build template options array
DeviceExportHelper.BuildTemplateOptionsArray(filterResult, templateId)

// Build bulk export response
DeviceExportHelper.BuildBulkExportResponse(devices, matches, ...)
```

## Testing Controllers

### Unit Test Example
```csharp
[TestClass]
public class MyControllerTests
{
    private Mock<IMyService> _mockService;
    private Mock<ILogger<MyController>> _mockLogger;
    private MyController _controller;

    [TestInitialize]
    public void Setup()
    {
        _mockService = new Mock<IMyService>();
        _mockLogger = new Mock<ILogger<MyController>>();
        _controller = new MyController(_mockService.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetById_ValidId_ReturnsOk()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetByIdAsync(1))
            .ReturnsAsync(new MyModel { Id = 1 });

        // Act
        var result = await _controller.GetById(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
```

## Debugging Tips

### Enable Detailed Errors
```csharp
// In Program.cs (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

### Logging
```csharp
_logger.LogDebug("Debug message");
_logger.LogInformation("Info message");
_logger.LogWarning("Warning message");
_logger.LogError(ex, "Error message");
```

### Breakpoint Locations
- Constructor (verify dependency injection)
- Action method entry (verify parameters)
- Service calls (verify data)
- Return statements (verify response)

## Best Practices

1. **Keep Controllers Thin** - Business logic goes in services
2. **Use Dependency Injection** - Constructor injection for all dependencies
3. **Return IActionResult** - For flexibility in return types
4. **Document with XML Comments** - For OpenAPI/Swagger
5. **Handle Errors Properly** - Try/catch with appropriate status codes
6. **Validate Input** - Use model validation attributes
7. **Log Appropriately** - Log errors and important operations
8. **Test Actions** - Unit test controller actions independently

## References

- Full documentation: [CONTROLLER_REFACTORING.md](Implementation/CONTROLLER_REFACTORING.md)
- API Reference: [API_REFERENCE.md](API_REFERENCE.md)
- Architecture: [ARCHITECTURE.md](ARCHITECTURE.md)
