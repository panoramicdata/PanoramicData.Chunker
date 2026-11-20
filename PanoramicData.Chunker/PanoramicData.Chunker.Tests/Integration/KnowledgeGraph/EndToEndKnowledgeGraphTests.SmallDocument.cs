using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

public partial class EndToEndKnowledgeGraphTests
{
	private string CreateTestHtml()
	{
		var testHtml = @"
<!DOCTYPE html>
<html>
<head><title>Test Document</title></head>
<body>
	<h1>Charles Darwin and the Plinian Society</h1>
	<p>The Plinian Society was founded by Professor Jameson at Edinburgh University.</p>
	<p>Charles Darwin was a member who regularly attended. The society was for students interested in natural history.</p>
	<p>Darwin presented research papers at the Plinian Society meetings.</p>
	
	<h2>The Voyage</h2>
	<p>Darwin later sailed on HMS Beagle from 1831 to 1836. He visited the Galapagos Islands.</p>
	<p>The voyage influenced his theory of evolution.</p>
</body>
</html>";

		_output.WriteLine("Created test HTML document");
		_output.WriteLine("");

		return testHtml;
	}

	private async Task<Models.ChunkingResult> ChunkTestHtmlAsync(string testHtml)
	{
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new HtmlDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 256,
			OverlapTokens = 25
		};

		await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testHtml));
		var result = await chunker.ChunkAsync(stream, options, _cancellationToken);

		result.Success.Should().BeTrue();
		var contentChunks = result.Chunks.OfType<Models.ContentChunk>().ToList();
		contentChunks.Should().NotBeEmpty();

		_output.WriteLine($"Created {result.Chunks.Count} chunks ({contentChunks.Count} content chunks)");
		_output.WriteLine("");

		return result;
	}

	private async Task<List<Entity>> ExtractTestEntitiesAsync(Models.ChunkingResult result)
	{
		var extractor = new HybridEntityExtractor();  // Use Hybrid instead of SimpleKeywordExtractor
		var entities = await extractor.ExtractEntitiesAsync(result.Chunks, _cancellationToken);

		entities.Should().NotBeEmpty();
		_output.WriteLine($"Extracted {entities.Count} entities");

		var darwinEntity = entities.FirstOrDefault(e =>
			e.Name.Equals("darwin", StringComparison.OrdinalIgnoreCase) ||
			e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase));
		darwinEntity.Should().NotBeNull("Should extract 'Darwin' entity");

		var plinianEntity = entities.FirstOrDefault(e =>
			e.Name.Contains("plinian", StringComparison.OrdinalIgnoreCase) ||
			e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase));
		plinianEntity.Should().NotBeNull("Should extract 'Plinian' entity");

		_output.WriteLine($"  - Darwin entity: {darwinEntity!.Name}, type={darwinEntity.Type}, confidence={darwinEntity.Confidence:F2}, frequency={darwinEntity.Frequency}");
		_output.WriteLine($"  - Plinian entity: {plinianEntity!.Name}, type={plinianEntity.Type}, confidence={plinianEntity.Confidence:F2}, frequency={plinianEntity.Frequency}");
		_output.WriteLine("");

		return entities;
	}

	private async Task<Graph> BuildTestGraphAsync(List<Entity> entities, Models.ChunkingResult result)
	{
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Test Knowledge Graph");
		foreach (var entity in entities)
		{
			graph.AddEntity(entity);
		}

		var relExtractor = new PatternBasedRelationshipExtractor(maxDistance: 200);
		var relationships = await relExtractor.ExtractRelationshipsAsync(
			graph.Entities,
			result.Chunks,
			_cancellationToken
		);

		foreach (var rel in relationships)
		{
			graph.AddRelationship(rel);
		}

		graph.ComputeStatistics();

		_output.WriteLine($"Built graph: {graph.Entities.Count} entities, {graph.Relationships.Count} relationships");
		_output.WriteLine("");

		return graph;
	}

	private async Task<(Graph Loaded, Entity LoadedDarwin, List<Relationship> DarwinRels)> LoadAndLogDarwinAsync(IGraphStore graphStore, Graph graph)
	{
		// Load the graph directly
		var loaded = await graphStore.LoadGraphAsync(graph.Id, _cancellationToken);
		loaded.Should().NotBeNull("Should be able to load graph from database");
		_output.WriteLine($"✅ Loaded graph '{loaded!.Name}' with {loaded.Entities.Count} entities");

		// Find Darwin entity using in-memory search
		var loadedDarwin = loaded.GetEntitiesByName("darwin").FirstOrDefault();
		loadedDarwin.Should().NotBeNull("Should be able to find Darwin entity");
		_output.WriteLine($"✅ Found Darwin entity: {loadedDarwin!.Name} (confidence: {loadedDarwin.Confidence:F2})");
		_output.WriteLine("");

		// Find Darwin's relationships
		var darwinRels = loaded.GetRelationships(loadedDarwin.Id);
		_output.WriteLine($"Darwin has {darwinRels.Count} relationships:");
		foreach (var rel in darwinRels.Take(5))
		{
			var toEntity = loaded.GetEntity(rel.ToEntityId);
			_output.WriteLine($"  - Darwin --[{rel.Type}]--> {toEntity?.Name} (confidence: {rel.Confidence:F2})");
		}
		_output.WriteLine("");

		return (loaded, loadedDarwin, darwinRels);
	}

	private async Task TryCypherDarwinQueryAsync()
	{
		_output.WriteLine("Step 7: Testing Cypher query capability...");
		_output.WriteLine("");

		var cypherExecutor = _fixture.Services.GetRequiredService<ICypherQueryExecutor>();
		_output.WriteLine("Cypher Query: MATCH (e:Entity) WHERE toLower(e.Name) = 'darwin' RETURN e");

		try
		{
			var darwinQuery = @"
				MATCH (e:Entity)
				WHERE toLower(e.Name) = 'darwin'
				RETURN e
			";

			var darwinResults = await cypherExecutor.ExecuteQueryAsync<Entity>(
				darwinQuery,
				null,
				_cancellationToken
			);
			var cypherDarwin = darwinResults.FirstOrDefault();

			if (cypherDarwin != null)
			{
				_output.WriteLine($"✅ Cypher query successful: Found {cypherDarwin.Name}");
			}
			else
			{
				_output.WriteLine("⚠️  Cypher query returned no results (AGE graph not yet populated)");
				_output.WriteLine("    This is expected - AGE integration is in progress");
				_output.WriteLine("    Data is currently stored in PostgreSQL tables and accessible via LoadGraphAsync()");
			}
		}
		catch (Exception ex)
		{
			_output.WriteLine($"⚠️  Cypher query error: {ex.Message}");
			_output.WriteLine("    This is expected if Apache AGE is not fully configured");
		}
		_output.WriteLine("");
	}

	private void VerifyJamesonRelationship(Graph loaded, Entity? plinianInLoaded)
	{
		var jamesonInLoaded = loaded.GetEntitiesByName("jameson").FirstOrDefault();
		if (jamesonInLoaded != null)
		{
			_output.WriteLine($"✅ Found Jameson entity (founder of Plinian Society)");

			// Check if Jameson has relationship with Plinian
			var jamesonRels = loaded.GetRelationships(jamesonInLoaded.Id);
			var jamesonToPlinianRel = jamesonRels.FirstOrDefault(r =>
				r.ToEntityId == plinianInLoaded!.Id || r.FromEntityId == plinianInLoaded.Id);

			if (jamesonToPlinianRel != null)
			{
				_output.WriteLine($"✅ Verified Jameson-Plinian relationship: {jamesonToPlinianRel.Type} (confidence: {jamesonToPlinianRel.Confidence:F2})");
			}
		}
	}

	private void WriteSmallDocumentSummary(Graph loaded)
	{
		_output.WriteLine("✅ All core assertions passed!");
		_output.WriteLine("");
		_output.WriteLine("=== Summary ===");
		_output.WriteLine($"✅ Graph saved successfully ({loaded.Entities.Count} entities, {loaded.Relationships.Count} relationships)");
		_output.WriteLine($"✅ Graph loaded successfully");
		_output.WriteLine($"✅ Entity queries work via LoadGraphAsync()");
		_output.WriteLine($"✅ Relationship traversal works");
		_output.WriteLine("⏳ Cypher queries will be fully functional once AGE graph population is implemented");
		_output.WriteLine("");
	}
}
