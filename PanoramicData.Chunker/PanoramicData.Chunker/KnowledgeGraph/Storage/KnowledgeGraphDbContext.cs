using Microsoft.EntityFrameworkCore;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

/// <summary>
/// Entity Framework Core DbContext for Knowledge Graph storage in PostgreSQL.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnowledgeGraphDbContext"/> class.
/// </remarks>
/// <param name="options">The DbContext options.</param>
public class KnowledgeGraphDbContext(DbContextOptions<KnowledgeGraphDbContext> options) : DbContext(options)
{
	/// <summary>
	/// Gets or sets the Entities DbSet.
	/// </summary>
	public DbSet<Entity> Entities { get; set; } = null!;

	/// <summary>
	/// Gets or sets the Relationships DbSet.
	/// </summary>
	public DbSet<Relationship> Relationships { get; set; } = null!;

	/// <summary>
	/// Gets or sets the Knowledge Graphs DbSet.
	/// </summary>
	public DbSet<Graph> Graphs { get; set; } = null!;

	/// <summary>
	/// Configures the model for the Knowledge Graph.
	/// </summary>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Configure Entity
		modelBuilder.Entity<Entity>(entity =>
		{
			entity.ToTable("KgEntities");
			entity.HasKey(e => e.Id);

			entity.Property(e => e.Name)
				.IsRequired()
				.HasMaxLength(500);

			entity.Property(e => e.NormalizedName)
				.IsRequired()
				.HasMaxLength(500);

			entity.Property(e => e.Type)
				.IsRequired()
				.HasConversion<string>()
				.HasMaxLength(50);

			entity.Property(e => e.Confidence)
				.IsRequired();

			entity.Property(e => e.Frequency)
				.IsRequired();

			// JSON columns for complex types
			entity.Property(e => e.Properties)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

			entity.Property(e => e.Aliases)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

			entity.Property(e => e.RelatedEntityIds)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

			entity.Property(e => e.Sources)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<List<EntitySource>>(v, (JsonSerializerOptions?)null) ?? new List<EntitySource>());

			entity.Property(e => e.Metadata)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<EntityMetadata>(v, (JsonSerializerOptions?)null) ?? new EntityMetadata());

			// Indexes (PascalCase)
			entity.HasIndex(e => e.Type).HasDatabaseName("IX_KgEntities_Type");
			entity.HasIndex(e => e.NormalizedName).HasDatabaseName("IX_KgEntities_NormalizedName");
			entity.HasIndex(e => e.Confidence).HasDatabaseName("IX_KgEntities_Confidence");
		});

		// Configure Relationship
		modelBuilder.Entity<Relationship>(entity =>
		{
			entity.ToTable("KgRelationships");
			entity.HasKey(r => r.Id);

			entity.Property(r => r.Type)
				.IsRequired()
				.HasConversion<string>()
				.HasMaxLength(50);

			entity.Property(r => r.FromEntityId)
				.IsRequired();

			entity.Property(r => r.ToEntityId)
				.IsRequired();

			entity.Property(r => r.Weight)
				.IsRequired();

			entity.Property(r => r.Confidence)
				.IsRequired();

			entity.Property(r => r.Bidirectional)
				.IsRequired();

			// JSON columns
			entity.Property(r => r.Properties)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

			entity.Property(r => r.Evidence)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<List<RelationshipEvidence>>(v, (JsonSerializerOptions?)null) ?? new List<RelationshipEvidence>());

			entity.Property(r => r.Metadata)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<RelationshipMetadata>(v, (JsonSerializerOptions?)null) ?? new RelationshipMetadata());

			// Indexes (PascalCase)
			entity.HasIndex(r => r.Type).HasDatabaseName("IX_KgRelationships_Type");
			entity.HasIndex(r => r.FromEntityId).HasDatabaseName("IX_KgRelationships_FromEntity");
			entity.HasIndex(r => r.ToEntityId).HasDatabaseName("IX_KgRelationships_ToEntity");
			entity.HasIndex(r => new { r.FromEntityId, r.ToEntityId }).HasDatabaseName("IX_KgRelationships_FromTo");
		});

		// Configure Graph
		modelBuilder.Entity<Graph>(entity =>
		{
			entity.ToTable("KgGraphs");
			entity.HasKey(g => g.Id);

			entity.Property(g => g.Name)
				.IsRequired()
				.HasMaxLength(200);

			// JSON columns for complex types
			entity.Property(g => g.Metadata)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<GraphMetadata>(v, (JsonSerializerOptions?)null) ?? new GraphMetadata());

			entity.Property(g => g.Schema)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<GraphSchema>(v, (JsonSerializerOptions?)null) ?? new GraphSchema());

			entity.Property(g => g.Statistics)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<GraphStatistics>(v, (JsonSerializerOptions?)null) ?? new GraphStatistics());

			// Ignore navigation properties (handled separately)
			entity.Ignore(g => g.Entities);
			entity.Ignore(g => g.Relationships);

			// Indexes (PascalCase)
			entity.HasIndex(g => g.Name).IsUnique().HasDatabaseName("IX_KgGraphs_Name");
		});
	}
}
