# ApacheAgeFixture - Test Fixture for Apache AGE Integration Tests

## Overview

`ApacheAgeFixture` is an xUnit v3 test fixture that provides a clean Apache AGE (PostgreSQL with AGE extension) instance for knowledge graph integration testing.

## Features

- ? **Automatic Container Management** - Spins up Apache AGE Docker container
- ? **Service Provider Configuration** - Pre-configured DI with `IGraphStore` and `ICypherQueryExecutor`
- ? **Database Isolation** - Each test class gets a clean database
- ? **Flexible Configuration** - Supports existing database or Testcontainers
- ? **Apache AGE Verification** - Checks if AGE extension is available
- ? **Clean Test Separation** - `CleanDatabaseAsync()` for test isolation

---

## Usage

### Basic Usage

```csharp
[Collection("PostgreSQL")]
public class MyKnowledgeGraphTests(ApacheAgeFixture fixture, ITestOutputHelper output) 
    : IClassFixture<ApacheAgeFixture>
{
    private readonly ApacheAgeFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task MyTest()
{
        // Clean database before test
    await _fixture.CleanDatabaseAsync();

        // Get services from DI
  var graphStore = _fixture.Services.GetRequiredService<IGraphStore>();
      var cypherExecutor = _fixture.Services.GetRequiredService<ICypherQueryExecutor>();

        // Your test code here...
    }
}
```

### Using Helper Methods

```csharp
[Fact]
public async Task TestWithHelpers()
{
    // Clean database
    await _fixture.CleanDatabaseAsync();

    // Get graph store
    var graphStore = _fixture.GetGraphStore();

    // Get Cypher executor
    var cypherExecutor = _fixture.GetCypherExecutor();

    // Check if Apache AGE is available
    var ageAvailable = await _fixture.IsApacheAgeAvailableAsync();
    if (ageAvailable)
    {
        // Run Cypher queries
    }
    else
    {
    // Fallback to SQL queries
    }
}
```

---

## Configuration

### Using Testcontainers (Default)

The fixture automatically starts an Apache AGE Docker container using the `apache/age:latest` image.

**Default Configuration**:
- Image: `apache/age:latest`
- Username: `postgres`
- Password: `test_password`
- Database: `panoramicdata_chunker_test`
- Graph Name: `test_knowledge_graph`

### Using Existing Database

Configure via `appsettings.Test.json` or user secrets:

```json
{
  "UseExistingDatabase": true,
"ConnectionStrings": {
    "KnowledgeGraph": "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=mypass"
  }
}
```

### Custom Docker Configuration

Configure via `appsettings.Test.json`:

```json
{
  "PostgresDocker": {
 "Image": "apache/age:PG15",
    "Username": "testuser",
    "Password": "testpass",
    "Database": "test_db"
  }
}
```

---

## Properties

### ConnectionString
```csharp
public string ConnectionString { get; private set; }
```
Gets the PostgreSQL connection string for the test instance.

### GraphName
```csharp
public string GraphName { get; } = "test_knowledge_graph";
```
Gets the default graph name used for tests.

### Services
```csharp
public IServiceProvider Services { get; }
```
Gets the service provider with pre-configured services:
- `IGraphStore` (Apache AGE implementation)
- `ICypherQueryExecutor` (Apache AGE Cypher executor)
- `ILogger<T>` (Console logging)

---

## Methods

### CleanDatabaseAsync()
```csharp
public async Task CleanDatabaseAsync()
```
Cleans all data from Apache AGE tables while preserving schema. Call this before each test for isolation.

**Tables Cleaned**:
- `age_relationships`
- `age_entities`
- `age_graph_metadata`

### GetGraphStore()
```csharp
public IGraphStore GetGraphStore()
```
Returns a scoped instance of `IGraphStore` for testing.

### GetCypherExecutor()
```csharp
public ICypherQueryExecutor GetCypherExecutor()
```
Returns a scoped instance of `ICypherQueryExecutor` for testing.

### IsApacheAgeAvailableAsync()
```csharp
public async Task<bool> IsApacheAgeAvailableAsync()
```
Checks if Apache AGE extension is installed and available.

**Returns**: `true` if AGE is available, `false` otherwise.

---

## Lifecycle

### Initialization (InitializeAsync)
1. Reads configuration from `appsettings.Test.json` and user secrets
2. Starts PostgreSQL container with Apache AGE (or uses existing database)
3. Configures DI services (`IGraphStore`, `ICypherQueryExecutor`)
4. Initializes Apache AGE extension and creates graph
5. Verifies Apache AGE availability

### Cleanup (DisposeAsync)
1. Disposes service provider
2. Stops and removes Docker container
3. Cleans up resources

---

## Comparison with PostgresKnowledgeGraphFixture

### ApacheAgeFixture (New)

**Advantages**:
- ? Simpler - Focused only on Apache AGE
- ? Cleaner - No EF Core DbContext dependencies
- ? Lighter - Fewer dependencies
- ? Apache AGE-first - Designed specifically for graph database testing

**Use Case**: Testing Apache AGE graph storage and Cypher queries

### PostgresKnowledgeGraphFixture (Legacy)

**Advantages**:
- ? Full EF Core support
- ? Database migrations
- ? Manual schema creation fallback

**Use Case**: Testing EF Core models and migrations (may be deprecated)

---

## Example Test Class

```csharp
using PanoramicData.Chunker.Tests.Fixtures;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using Microsoft.Extensions.DependencyInjection;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

[Collection("PostgreSQL")]
public class MyGraphTests(ApacheAgeFixture fixture, ITestOutputHelper output) 
    : IClassFixture<ApacheAgeFixture>
{
    private readonly ApacheAgeFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task SaveAndLoadGraph_ShouldWork()
{
        // Arrange
      await _fixture.CleanDatabaseAsync();
   
        var graphStore = _fixture.Services.GetRequiredService<IGraphStore>();
   var graph = new Graph("Test Graph");
 
        var entity = new Entity(EntityType.Person, "Darwin", 0.9);
        graph.AddEntity(entity);

        // Act
        await graphStore.SaveGraphAsync(graph, CancellationToken.None);
        var loaded = await graphStore.LoadGraphAsync(graph.Id, CancellationToken.None);

  // Assert
   loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Test Graph");
      loaded.Entities.Should().ContainSingle();
    }

    [Fact]
    public async Task CypherQuery_WhenAgeAvailable_ShouldExecute()
    {
        // Arrange
      await _fixture.CleanDatabaseAsync();
        
   var ageAvailable = await _fixture.IsApacheAgeAvailableAsync();
        if (!ageAvailable)
        {
      _output.WriteLine("Apache AGE not available, skipping Cypher test");
  return;
  }

  var cypherExecutor = _fixture.Services.GetRequiredService<ICypherQueryExecutor>();

        // Act & Assert
        var query = "MATCH (n:Entity) RETURN count(n)";
      var result = await cypherExecutor.ExecuteQueryAsync<int>(query, null, CancellationToken.None);
        
        result.Should().NotBeNull();
    }
}
```

---

## Troubleshooting

### Container Won't Start

**Problem**: Testcontainers fails to start PostgreSQL

**Solutions**:
1. Ensure Docker is running
2. Check Docker has internet access to pull images
3. Use an existing database instead (set `UseExistingDatabase: true`)

### Apache AGE Not Available

**Problem**: Tests report "Apache AGE not available"

**Solutions**:
1. Verify using `apache/age:latest` image (not plain `postgres`)
2. Check logs: Apache AGE extension should be installed
3. Tests will fall back to SQL storage (no Cypher)

### Tests Interfere With Each Other

**Problem**: Tests fail due to data from previous tests

**Solution**: Call `await _fixture.CleanDatabaseAsync()` at the start of each test

### Slow Tests

**Problem**: Tests take too long to run

**Solutions**:
1. Use `[Collection("PostgreSQL")]` to share fixture across test classes
2. Consider using an existing database for faster startup
3. Reduce test data size

---

## Best Practices

### 1. Always Clean Database
```csharp
[Fact]
public async Task MyTest()
{
    await _fixture.CleanDatabaseAsync(); // ? Always clean first
 // ... test code ...
}
```

### 2. Use Collection Attribute
```csharp
[Collection("PostgreSQL")] // ? Share fixture across test classes
public class MyTests(ApacheAgeFixture fixture) : IClassFixture<ApacheAgeFixture>
```

### 3. Check AGE Availability for Cypher Tests
```csharp
[Fact]
public async Task TestCypher()
{
    var ageAvailable = await _fixture.IsApacheAgeAvailableAsync();
    if (!ageAvailable)
  {
        _output.WriteLine("Skipping - Apache AGE not available");
        return; // ? Graceful fallback
    }
    
    // ... Cypher test code ...
}
```

### 4. Use Service Provider, Not Helper Methods
```csharp
// ? Preferred - uses scoping correctly
var graphStore = _fixture.Services.GetRequiredService<IGraphStore>();

// ?? Less preferred - creates new scope
var graphStore = _fixture.GetGraphStore();
```

---

## Migration from PostgresKnowledgeGraphFixture

### Before (Old Fixture)
```csharp
public class MyTests(PostgresKnowledgeGraphFixture fixture) 
 : IClassFixture<PostgresKnowledgeGraphFixture>
{
    [Fact]
    public async Task MyTest()
    {
        await _fixture.CleanDatabaseAsync();
    var context = _fixture.Context; // EF Core DbContext
// ...
    }
}
```

### After (New Fixture)
```csharp
public class MyTests(ApacheAgeFixture fixture) 
    : IClassFixture<ApacheAgeFixture>
{
    [Fact]
    public async Task MyTest()
    {
        await _fixture.CleanDatabaseAsync();
        var graphStore = _fixture.Services.GetRequiredService<IGraphStore>();
        // ...
}
}
```

**Changes Needed**:
1. Replace `PostgresKnowledgeGraphFixture` with `ApacheAgeFixture`
2. Remove EF Core `DbContext` usage
3. Use `IGraphStore` instead
4. Update assertions to use graph methods instead of EF queries

---

## Related Documentation

- [Apache AGE Integration Complete](../APACHE_AGE_INTEGRATION_COMPLETE.md)
- [End-to-End Knowledge Graph Tests](../End-to-End-KnowledgeGraph-Tests.md)
- [Apache AGE Cypher Executor](../ApacheAgeCypherExecutor-TestCoverage.md)

---

**Status**: ? Production Ready  
**Version**: 1.0  
**Last Updated**: January 2025

