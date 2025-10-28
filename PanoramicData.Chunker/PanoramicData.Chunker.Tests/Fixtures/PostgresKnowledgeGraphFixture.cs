using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.KnowledgeGraph.Storage;
using Testcontainers.PostgreSql;

namespace PanoramicData.Chunker.Tests.Fixtures;

/// <summary>
/// xUnit v3 fixture for setting up PostgreSQL database for Knowledge Graph integration tests.
/// Uses Testcontainers to spin up a PostgreSQL instance.
/// </summary>
public class PostgresKnowledgeGraphFixture : IAsyncLifetime
{
	private PostgreSqlContainer? _postgresContainer;
	private ServiceProvider? _serviceProvider;

	/// <summary>
	/// Gets the service provider with all required services configured.
	/// </summary>
	public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized");

	/// <summary>
	/// Gets the connection string for the test database.
	/// </summary>
	public string ConnectionString { get; private set; } = string.Empty;

	/// <summary>
	/// Gets the database context for direct database access in tests.
	/// </summary>
	public KnowledgeGraphDbContext Context => Services.GetRequiredService<KnowledgeGraphDbContext>();

	/// <summary>
	/// Initializes the fixture asynchronously.
	/// </summary>
	public async ValueTask InitializeAsync()
	{
		// Build configuration (supports user secrets for local dev)
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.Test.json", optional: true)
			.AddUserSecrets<PostgresKnowledgeGraphFixture>(optional: true)
			.AddEnvironmentVariables()
			.Build();

		// Check if user wants to use an existing PostgreSQL instance
		var useExistingDatabase = configuration.GetValue<bool>("UseExistingDatabase");
		var existingConnectionString = configuration.GetConnectionString("KnowledgeGraph");

		if (useExistingDatabase && !string.IsNullOrEmpty(existingConnectionString))
		{
			// Use existing database (configured via user secrets or environment variables)
			ConnectionString = existingConnectionString;
		}
		else
		{
			// Spin up Testcontainers PostgreSQL
			var postgresImage = configuration["PostgresDocker:Image"] ?? "postgres:17-alpine";
			var username = configuration["PostgresDocker:Username"] ?? "test_user";
			var password = configuration["PostgresDocker:Password"] ?? "test_password";
			var database = configuration["PostgresDocker:Database"] ?? "panoramicdata_chunker_test";

			_postgresContainer = new PostgreSqlBuilder()
				.WithImage(postgresImage)
				.WithDatabase(database)
				.WithUsername(username)
				.WithPassword(password)
				.WithCleanUp(true)
				.Build();

			await _postgresContainer.StartAsync();
			ConnectionString = _postgresContainer.GetConnectionString();
		}

		// Setup DI container
		var services = new ServiceCollection();

		// Add logging
		services.AddLogging(builder =>
		{
			builder.AddConsole();
			builder.SetMinimumLevel(LogLevel.Information);
		});

		// Add DbContext
		services.AddDbContext<KnowledgeGraphDbContext>(options =>
		{
			options.UseNpgsql(ConnectionString);
			options.EnableSensitiveDataLogging();
			options.EnableDetailedErrors();
		});

		// Add PostgresGraphStore
		services.AddScoped<Interfaces.KnowledgeGraph.IGraphStore, PostgresGraphStore>();

		_serviceProvider = services.BuildServiceProvider();

		// Run migrations to create schema
		using var scope = _serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<KnowledgeGraphDbContext>();
		await context.Database.EnsureCreatedAsync();
	}

	/// <summary>
	/// Cleans up the fixture asynchronously.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		if (_serviceProvider != null)
		{
			// Clean up database
			using (var scope = _serviceProvider.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<KnowledgeGraphDbContext>();
				await context.Database.EnsureDeletedAsync();
			}

			await _serviceProvider.DisposeAsync();
		}

		if (_postgresContainer != null)
		{
			await _postgresContainer.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Cleans the database between tests (removes all data but keeps schema).
	/// </summary>
	public async Task CleanDatabaseAsync()
	{
		using var scope = _serviceProvider!.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<KnowledgeGraphDbContext>();

		// Delete all data (order matters due to foreign keys)
		context.Relationships.RemoveRange(context.Relationships);
		context.Entities.RemoveRange(context.Entities);
		context.Graphs.RemoveRange(context.Graphs);

		await context.SaveChangesAsync();
	}
}
