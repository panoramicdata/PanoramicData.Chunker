using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PanoramicData.Chunker.KnowledgeGraph.Storage.Migrations
{
	/// <inheritdoc />
	public partial class InitialCreateWithApacheAge : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Step 1: Install Apache AGE extension (requires superuser)
			migrationBuilder.Sql(@"
				CREATE EXTENSION IF NOT EXISTS age;
			");

			// Step 2: Set search path to include AGE catalog
			migrationBuilder.Sql(@"
				SET search_path = ag_catalog, ""$user"", public;
			");

			// Step 3: Create the knowledge_graph in Apache AGE
			migrationBuilder.Sql(@"
				SELECT ag_catalog.create_graph('knowledge_graph');
			");

			// Step 4: Create EF Core tables for CRUD operations
			migrationBuilder.CreateTable(
				name: "KgEntities",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
					Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					NormalizedName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					Confidence = table.Column<double>(type: "double precision", nullable: false),
					Frequency = table.Column<int>(type: "integer", nullable: false),
					Properties = table.Column<string>(type: "jsonb", nullable: false),
					Metadata = table.Column<string>(type: "jsonb", nullable: false),
					Sources = table.Column<string>(type: "jsonb", nullable: false),
					Aliases = table.Column<string>(type: "jsonb", nullable: false),
					RelatedEntityIds = table.Column<string>(type: "jsonb", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_KgEntities", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "KgGraphs",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
					Metadata = table.Column<string>(type: "jsonb", nullable: false),
					Schema = table.Column<string>(type: "jsonb", nullable: false),
					Statistics = table.Column<string>(type: "jsonb", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_KgGraphs", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "KgRelationships",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
					FromEntityId = table.Column<Guid>(type: "uuid", nullable: false),
					ToEntityId = table.Column<Guid>(type: "uuid", nullable: false),
					Weight = table.Column<double>(type: "double precision", nullable: false),
					Confidence = table.Column<double>(type: "double precision", nullable: false),
					Bidirectional = table.Column<bool>(type: "boolean", nullable: false),
					Properties = table.Column<string>(type: "jsonb", nullable: false),
					Metadata = table.Column<string>(type: "jsonb", nullable: false),
					Evidence = table.Column<string>(type: "jsonb", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_KgRelationships", x => x.Id);
				});

			// Step 5: Create indexes for EF Core tables
			migrationBuilder.CreateIndex(
				name: "IX_KgEntities_Confidence",
				table: "KgEntities",
				column: "Confidence");

			migrationBuilder.CreateIndex(
				name: "IX_KgEntities_NormalizedName",
				table: "KgEntities",
				column: "NormalizedName");

			migrationBuilder.CreateIndex(
				name: "IX_KgEntities_Type",
				table: "KgEntities",
				column: "Type");

			migrationBuilder.CreateIndex(
				name: "IX_KgGraphs_Name",
				table: "KgGraphs",
				column: "Name",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_KgRelationships_FromEntity",
				table: "KgRelationships",
				column: "FromEntityId");

			migrationBuilder.CreateIndex(
				name: "IX_KgRelationships_FromTo",
				table: "KgRelationships",
				columns: ["FromEntityId", "ToEntityId"]);

			migrationBuilder.CreateIndex(
				name: "IX_KgRelationships_ToEntity",
				table: "KgRelationships",
				column: "ToEntityId");

			migrationBuilder.CreateIndex(
				name: "IX_KgRelationships_Type",
				table: "KgRelationships",
				column: "Type");

			// Step 6: Create Apache AGE vertex and edge labels
			migrationBuilder.Sql(@"
				SELECT ag_catalog.create_vlabel('knowledge_graph', 'Entity');
			");

			migrationBuilder.Sql(@"
				SELECT ag_catalog.create_elabel('knowledge_graph', 'Relationship');
			");

			// Step 7: Grant permissions (adjust username as needed)
			// Note: This may fail if the user doesn't exist yet - that's okay
			migrationBuilder.Sql(@"
				DO $$
				BEGIN
					IF EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'test_user') THEN
						GRANT USAGE ON SCHEMA ag_catalog TO test_user;
						GRANT SELECT ON ALL TABLES IN SCHEMA ag_catalog TO test_user;
						GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA ag_catalog TO test_user;
					END IF;
				END
				$$;
			", suppressTransaction: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Drop EF Core tables
			migrationBuilder.DropTable(
				name: "KgEntities");

			migrationBuilder.DropTable(
				name: "KgGraphs");

			migrationBuilder.DropTable(
				name: "KgRelationships");

			// Drop Apache AGE graph
			migrationBuilder.Sql(@"
				SELECT ag_catalog.drop_graph('knowledge_graph', true);
			");

			// Drop AGE extension
			migrationBuilder.Sql(@"
				DROP EXTENSION IF EXISTS age CASCADE;
			");
		}
	}
}
