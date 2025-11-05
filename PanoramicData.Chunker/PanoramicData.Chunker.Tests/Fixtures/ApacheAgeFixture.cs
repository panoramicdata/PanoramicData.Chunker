using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Storage;
using Testcontainers.PostgreSql;

namespace PanoramicData.Chunker.Tests.Fixtures;

/// <summary>
/// xUnit v3 fixture for Apache AGE (PostgreSQL with AGE extension) integration tests.
/// Provides a clean Apache AGE instance for knowledge graph testing.
/// </summary>
public class ApacheAgeFixture : IAsyncLifetime
{
	private PostgreSqlContainer? _postgresContainer;
	private ServiceProvider? _serviceProvider;

	/// <summary>
	/// Gets the service provider with Apache AGE graph store and Cypher executor configured.
	/// </summary>
	public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized");

	/// <summary>
	/// Gets the connection string for the Apache AGE PostgreSQL instance.
	/// </summary>
	public string ConnectionString { get; private set; } = string.Empty;

	/// <summary>
	/// Gets the graph name used for this test instance.
	/// </summary>
	public string GraphName { get; } = "test_knowledge_graph";

	/// <summary>
	/// Initializes the fixture asynchronously - sets up Apache AGE container and services.
	/// </summary>
	public async ValueTask InitializeAsync()
	{
		Console.WriteLine("[ApacheAgeFixture] Starting initialization...");

		// Build configuration
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.Test.json", optional: true)
			.AddUserSecrets<ApacheAgeFixture>(optional: true)
			.AddEnvironmentVariables()
			.Build();

		// Check if using existing database
		var useExistingDatabase = configuration.GetValue<bool>("UseExistingDatabase");
		var existingConnectionString = configuration.GetConnectionString("KnowledgeGraph");

		if (useExistingDatabase && !string.IsNullOrEmpty(existingConnectionString))
		{
			ConnectionString = existingConnectionString;
			Console.WriteLine($"[ApacheAgeFixture] Using existing database");
		}
		else
		{
			// Start Apache AGE container
			var postgresImage = configuration["PostgresDocker:Image"] ?? "apache/age:latest";
			var username = configuration["PostgresDocker:Username"] ?? "postgres";
			var password = configuration["PostgresDocker:Password"] ?? "test_password";
			var database = configuration["PostgresDocker:Database"] ?? "panoramicdata_chunker_test";

			Console.WriteLine($"[ApacheAgeFixture] Starting PostgreSQL container with Apache AGE: {postgresImage}");

			_postgresContainer = new PostgreSqlBuilder()
				.WithImage(postgresImage)
				.WithDatabase(database)
				.WithUsername(username)
				.WithPassword(password)
				.WithCleanUp(true)
				.Build();

			await _postgresContainer.StartAsync();
			ConnectionString = _postgresContainer.GetConnectionString();
			Console.WriteLine($"[ApacheAgeFixture] Container started successfully");
		}

		// Setup DI container
		var services = new ServiceCollection();

		// Add logging
		services.AddLogging(builder =>
		{
			builder.AddConsole();
			builder.SetMinimumLevel(LogLevel.Information);
			builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
		});

		// Add Apache AGE Graph Store
		services.AddScoped<IGraphStore>(sp =>
		{
			var logger = sp.GetRequiredService<ILogger<ApacheAgeGraphStore>>();
			return new ApacheAgeGraphStore(ConnectionString, logger);
		});

		// Add Apache AGE Cypher Executor
		services.AddScoped<ICypherQueryExecutor>(sp =>
		{
			var logger = sp.GetRequiredService<ILogger<ApacheAgeCypherExecutor>>();
			return new ApacheAgeCypherExecutor(ConnectionString, GraphName, logger);
		});

		_serviceProvider = services.BuildServiceProvider();

		// Initialize Apache AGE tables
		Console.WriteLine("[ApacheAgeFixture] Initializing Apache AGE tables...");
		await InitializeApacheAgeAsync();
		Console.WriteLine("[ApacheAgeFixture] Initialization complete");
	}

	/// <summary>
	/// Initializes Apache AGE extension and creates necessary tables.
	/// </summary>
	private async Task InitializeApacheAgeAsync()
	{
		using var scope = _serviceProvider!.CreateScope();
		var graphStore = scope.ServiceProvider.GetRequiredService<IGraphStore>();

		// The ApacheAgeGraphStore.SaveGraphAsync will automatically create tables
		// on first use via CreateMetadataTableIfNotExistsAsync
		// We just need to verify the extension is available

		try
		{
			await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
			await connection.OpenAsync();

			// Try to load AGE
			try
			{
				await using var cmd = new Npgsql.NpgsqlCommand("LOAD 'age';", connection);
				await cmd.ExecuteNonQueryAsync();
				Console.WriteLine("[ApacheAgeFixture] ? Apache AGE extension loaded");

				// Set search path
				await using var setPathCmd = new Npgsql.NpgsqlCommand("SET search_path = ag_catalog, public;", connection);
				await setPathCmd.ExecuteNonQueryAsync();

				// Check if graph exists, create if not
				await using var checkGraphCmd = new Npgsql.NpgsqlCommand(
					$"SELECT COUNT(*) FROM ag_catalog.ag_graph WHERE name = '{GraphName}';",
					connection);
				var graphExists = Convert.ToInt32(await checkGraphCmd.ExecuteScalarAsync()) > 0;

				if (!graphExists)
				{
					Console.WriteLine($"[ApacheAgeFixture] Creating graph '{GraphName}'...");
					await using var createGraphCmd = new Npgsql.NpgsqlCommand(
						$"SELECT ag_catalog.create_graph('{GraphName}');",
						connection);
					await createGraphCmd.ExecuteNonQueryAsync();
					Console.WriteLine($"[ApacheAgeFixture] ? Graph '{GraphName}' created");
				}
				else
				{
					Console.WriteLine($"[ApacheAgeFixture] ? Graph '{GraphName}' already exists");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApacheAgeFixture] ?? Apache AGE not available: {ex.Message}");
				Console.WriteLine("[ApacheAgeFixture] Tests will use SQL storage only (no Cypher)");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[ApacheAgeFixture] ?? Database connection error: {ex.Message}");
			throw;
		}
	}

	/// <summary>
	/// Cleans the database between tests - removes all graph data but keeps schema.
	/// </summary>
	public async Task CleanDatabaseAsync()
	{
		Console.WriteLine("[ApacheAgeFixture] Cleaning database...");

		await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
		await connection.OpenAsync();

		try
		{
			// Delete all data from Apache AGE tables (order matters due to FKs)
			await using (var cmd = new Npgsql.NpgsqlCommand("TRUNCATE TABLE age_relationships CASCADE", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			await using (var cmd = new Npgsql.NpgsqlCommand("TRUNCATE TABLE age_entities CASCADE", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			await using (var cmd = new Npgsql.NpgsqlCommand("TRUNCATE TABLE age_graph_metadata CASCADE", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			Console.WriteLine("[ApacheAgeFixture] ? Database cleaned");
		}
		catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
		{
			// Tables don't exist yet - this is fine, they'll be created on first use
			Console.WriteLine("[ApacheAgeFixture] Tables don't exist yet - will be created on first use");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[ApacheAgeFixture] ?? Error cleaning database: {ex.Message}");
			// Don't throw - let tests proceed, they might create tables
		}
	}

	/// <summary>
	/// Disposes the fixture asynchronously - cleans up container and services.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		Console.WriteLine("[ApacheAgeFixture] Disposing...");

		if (_serviceProvider != null)
		{
			await _serviceProvider.DisposeAsync();
		}

		if (_postgresContainer != null)
		{
			await _postgresContainer.DisposeAsync();
		}

		Console.WriteLine("[ApacheAgeFixture] Disposed");
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Gets a scoped graph store for testing.
	/// </summary>
	/// <returns>A new scoped instance of IGraphStore.</returns>
	public IGraphStore GetGraphStore()
	{
		using var scope = _serviceProvider!.CreateScope();
		return scope.ServiceProvider.GetRequiredService<IGraphStore>();
	}

	/// <summary>
	/// Gets a scoped Cypher executor for testing.
	/// </summary>
	/// <returns>A new scoped instance of ICypherQueryExecutor.</returns>
	public ICypherQueryExecutor GetCypherExecutor()
	{
		using var scope = _serviceProvider!.CreateScope();
		return scope.ServiceProvider.GetRequiredService<ICypherQueryExecutor>();
	}

	/// <summary>
	/// Verifies that Apache AGE extension is available and working.
	/// </summary>
	/// <returns>True if Apache AGE is available, false otherwise.</returns>
	public async Task<bool> IsApacheAgeAvailableAsync()
	{
		try
		{
			await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
			await connection.OpenAsync();

			await using var cmd = new Npgsql.NpgsqlCommand(
				"SELECT COUNT(*) FROM pg_extension WHERE extname = 'age';",
				connection);
			var result = await cmd.ExecuteScalarAsync();
			return Convert.ToInt32(result) > 0;
		}
		catch
		{
			return false;
		}
	}
}
