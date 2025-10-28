using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

/// <summary>
/// Design-time factory for KnowledgeGraphDbContext to support EF Core migrations.
/// </summary>
public class KnowledgeGraphDbContextFactory : IDesignTimeDbContextFactory<KnowledgeGraphDbContext>
{
	/// <summary>
	/// Creates a new instance of KnowledgeGraphDbContext for design-time operations.
	/// </summary>
	public KnowledgeGraphDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<KnowledgeGraphDbContext>();

		// Use a default connection string for migrations
		// In production, this comes from configuration
		var connectionString = "Host=localhost;Database=panoramicdata_chunker;Username=postgres;Password=postgres";

		optionsBuilder.UseNpgsql(connectionString);

		return new KnowledgeGraphDbContext(optionsBuilder.Options);
	}
}
