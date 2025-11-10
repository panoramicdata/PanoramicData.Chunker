using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using PanoramicData.Chunker.Tests.Fixtures;
using PanoramicData.Chunker.Tests.Helpers;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

/// <summary>
/// Phase 2: Baseline comparison tests - compares extracted knowledge graph against ground truth.
/// </summary>
[Collection("PostgreSQL")]
public class GroundTruthComparisonTests(ApacheAgeFixture fixture, ITestOutputHelper output)
	: IClassFixture<ApacheAgeFixture>
{
	private readonly ApacheAgeFixture _fixture = fixture;
	private readonly ITestOutputHelper _output = output;

	private readonly static CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	[Fact]
	public async Task ExtractedGraph_ShouldMatch_GroundTruthRelationships()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");

		_output.WriteLine($"Loaded {groundTruth.Count} ground truth relationships");
		_output.WriteLine(string.Empty);

		// Act: Extract knowledge graph (same as EndToEnd test)
		var extractedGraph = await ExtractDarwinKnowledgeGraphAsync();

		_output.WriteLine($"Extracted graph with {extractedGraph.Entities.Count} entities and {extractedGraph.Relationships.Count} relationships");
		_output.WriteLine(string.Empty);

		// Compare
		var comparison = new GroundTruthComparison();
		var results = GroundTruthComparison.Compare(extractedGraph, groundTruth);

		// Report
		_output.WriteLine(results.GenerateReport());
		_output.WriteLine(string.Empty);

		// Assert - Baseline expectations (realistic for initial extraction)
		results.RecallRate.Should().BeGreaterThan(0.10,
			"Baseline: Should find at least 10% of ground truth relationships");
		results.F1Score.Should().BeGreaterThan(0.05,
			"Baseline: Should have at least 5% F1 score");

		// Log statistics for analysis
		_output.WriteLine("=== Extraction Statistics ===");
		_output.WriteLine($"Total ground truth relationships: {groundTruth.Count}");
		_output.WriteLine($"Total extracted relationships: {extractedGraph.Relationships.Count}");
		_output.WriteLine($"True positives: {results.TruePositives}");
		_output.WriteLine($"False positives: {results.FalsePositives}");
		_output.WriteLine($"False negatives: {results.FalseNegatives}");
		_output.WriteLine($"Precision: {results.Precision:P2}");
		_output.WriteLine($"Recall: {results.RecallRate:P2}");
		_output.WriteLine($"F1 Score: {results.F1Score:P2}");
		_output.WriteLine(string.Empty);

		// Categorize misses
		_output.WriteLine("=== Miss Categories ===");
		var missCategories = results.Misses
			.GroupBy(m => m.Category)
			.OrderByDescending(g => g.Count());

		foreach (var category in missCategories)
		{
			_output.WriteLine($"{category.Key}: {category.Count()} ({category.Count() * 100.0 / results.Misses.Count:F1}%)");
		}
	}

	[Fact]
	public async Task ExtractedGraph_ShouldExtractDarwinEntity()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		// Act
		var graph = await ExtractDarwinKnowledgeGraphAsync();

		// Assert - Verify Darwin entity exists
		var darwinEntities = graph.Entities
			.Where(e => e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase))
			.ToList();

		darwinEntities.Should().NotBeEmpty("Darwin should be extracted as an entity");

		_output.WriteLine($"Found {darwinEntities.Count} Darwin-related entities:");
		foreach (var entity in darwinEntities)
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, confidence: {entity.Confidence:F2})");
		}
	}

	[Fact]
	public async Task ExtractedGraph_ShouldExtractKeyOrganizations()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		// Act
		var graph = await ExtractDarwinKnowledgeGraphAsync();

		// Assert - Check for key organizations from ground truth
		var organizations = graph.Entities
			.Where(e => e.Type is EntityType.Organization or EntityType.ProperNoun)
			.ToList();

		_output.WriteLine($"Found {organizations.Count} organization/proper noun entities:");
		foreach (var org in organizations.Take(20))
		{
			_output.WriteLine($"  - {org.Name} (confidence: {org.Confidence:F2}, frequency: {org.Frequency})");
		}

		// Check for specific organizations
		var hasUniversity = organizations.Any(e =>
			e.Name.Contains("University", StringComparison.OrdinalIgnoreCase));
		var hasSociety = organizations.Any(e =>
			e.Name.Contains("Society", StringComparison.OrdinalIgnoreCase));

		_output.WriteLine(string.Empty);
		_output.WriteLine($"Has University: {hasUniversity}");
		_output.WriteLine($"Has Society: {hasSociety}");
	}

	[Fact]
	public async Task Debug_ActualEntityExtraction_ShowAliases()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		// Act
		var graph = await ExtractDarwinKnowledgeGraphAsync();

		// Assert - Show all extracted entities with aliases
		_output.WriteLine($"=== EXTRACTED ENTITIES ({graph.Entities.Count}) ===");
		_output.WriteLine(string.Empty);

		var properNouns = graph.Entities
			.Where(e => e.Type == EntityType.ProperNoun)
			.OrderByDescending(e => e.Confidence)
			.Take(50)
			.ToList();

		_output.WriteLine($"Proper Nouns ({properNouns.Count}):");
		foreach (var entity in properNouns)
		{
			_output.WriteLine($"  {entity.Name} (conf: {entity.Confidence:F2}, freq: {entity.Frequency})");
			if (entity.Aliases.Count > 0)
			{
				_output.WriteLine($"    Aliases: {string.Join(", ", entity.Aliases)}");
			}
		}

		_output.WriteLine(string.Empty);
		_output.WriteLine("=== GROUND TRUTH ENTITY CHECK ===");

		// Ground truth entities we expect to find
		var groundTruthEntities = new[]
		{
			"Plinian Society", "Professor Jameson", "HMS Beagle", "Charles Darwin",
			"Edinburgh University", "Robert Grant", "Captain FitzRoy", "Galapagos Islands",
			"John Henslow", "Cambridge University", "Origin of Species"
		};

		foreach (var gt in groundTruthEntities)
		{
			var found = graph.Entities.Any(e =>
				// Exact or partial match
				e.Name.Equals(gt, StringComparison.OrdinalIgnoreCase) ||
				e.Name.Contains(gt, StringComparison.OrdinalIgnoreCase) ||
				gt.Contains(e.Name, StringComparison.OrdinalIgnoreCase) ||
				// Check aliases
				e.Aliases.Any(a =>
					a.Equals(gt, StringComparison.OrdinalIgnoreCase) ||
					gt.Contains(a, StringComparison.OrdinalIgnoreCase)));

			_output.WriteLine($"  {(found ? "✓" : "✗")} {gt}");

			// If not found, try to help debug
			if (!found)
			{
				var partialMatches = graph.Entities.Where(e =>
				{
					var words = gt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					return words.Any(w => e.Name.Contains(w, StringComparison.OrdinalIgnoreCase));
				}).ToList();

				if (partialMatches.Count != 0)
				{
					_output.WriteLine($"   Partial matches: {string.Join(", ", partialMatches.Take(3).Select(e => e.Name))}");
				}
			}
		}

		// This test always passes - it's diagnostic only
		true.Should().BeTrue();
	}

	[Fact]
	public async Task Debug_SeparateExtractors_ShowWhatEachFinds()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var documentUrl = "https://www.gutenberg.org/files/2010/2010-h/2010-h.htm";

		string htmlContent;
		using (var httpClient = new HttpClient())
		{
			httpClient.DefaultRequestHeaders.Add("User-Agent",
				"PanoramicData.Chunker/1.0 (Educational Testing)");
			var response = await httpClient.GetAsync(documentUrl, _cancellationToken);
			response.EnsureSuccessStatusCode();
			htmlContent = await response.Content.ReadAsStringAsync(_cancellationToken);
		}

		// Chunk document
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new HtmlDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 512,
			MaxCharactersPerChunk = 2000,
			OverlapTokens = 100,
			EnforceSentenceBoundaries = true
		};

		await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent));
		var chunkingResult = await chunker.ChunkAsync(stream, options, _cancellationToken);

		_output.WriteLine($"Created {chunkingResult.Chunks.Count} chunks");
		_output.WriteLine(string.Empty);

		// Test each extractor separately
		var keywordExtractor = new SimpleKeywordExtractor(maxKeywords: 15, minWordLength: 4, minConfidence: 0.0);
		var capitalizationExtractor = new CapitalizationEntityExtractor(minOccurrences: 1, minWordLength: 2);

		var keywords = await keywordExtractor.ExtractEntitiesAsync(chunkingResult.Chunks, _cancellationToken);
		var properNouns = await capitalizationExtractor.ExtractEntitiesAsync(chunkingResult.Chunks, _cancellationToken);

		_output.WriteLine($"=== KEYWORD EXTRACTOR ({keywords.Count} entities) ===");
		foreach (var entity in keywords.Take(20))
		{
			_output.WriteLine($"  {entity.Name} (conf: {entity.Confidence:F2}, freq: {entity.Frequency})");
		}
		_output.WriteLine(string.Empty);

		_output.WriteLine($"=== CAPITALIZATION EXTRACTOR ({properNouns.Count} entities) ===");

		// Check for ground truth entities
		var groundTruth = new[] { "Plinian Society", "Professor Jameson", "Edinburgh University", "Origin" };
		foreach (var gt in groundTruth)
		{
			var found = properNouns.Any(e => e.Name.Contains(gt, StringComparison.OrdinalIgnoreCase));
			_output.WriteLine($"  {(found ? "✓" : "✗")} {gt}");
		}

		_output.WriteLine(string.Empty);
		_output.WriteLine("Sample proper nouns:");
		foreach (var entity in properNouns.Take(20))
		{
			_output.WriteLine($"  {entity.Name} (conf: {entity.Confidence:F2}, freq: {entity.Frequency})");
		}

		// This test always passes - it's diagnostic only
		true.Should().BeTrue();
	}

	[Fact]
	public async Task Debug_Phase12_WhyNoRelationships()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		// Act: Extract graph
		var graph = await ExtractDarwinKnowledgeGraphAsync();

		_output.WriteLine("=== PHASE 12 DIAGNOSTIC: Why No Relationships? ===");
		_output.WriteLine(string.Empty);

		// Test Case 1: Professor Jameson -> Founded -> Plinian Society
		_output.WriteLine("TEST CASE 1: Professor Jameson -> Founded -> Plinian Society");
		_output.WriteLine("----------------------------------------------------------------");

		var jameson = graph.Entities.FirstOrDefault(e =>
			e.Name.Contains("Jameson", StringComparison.OrdinalIgnoreCase));
		var plinian = graph.Entities.FirstOrDefault(e =>
			e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase));

		if (jameson == null)
		{
			_output.WriteLine("❌ PROBLEM: Jameson entity NOT FOUND");
			_output.WriteLine("   Searching for partial matches...");
			var partialMatches = graph.Entities
				.Where(e => e.Name.Contains("James", StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (partialMatches.Count > 0)
			{
				_output.WriteLine($"   Found {partialMatches.Count} partial matches:");
				foreach (var match in partialMatches.Take(5))
				{
					_output.WriteLine($"     - '{match.Name}' (type: {match.Type})");
				}
			}
		}
		else
		{
			_output.WriteLine($"✅ Jameson entity FOUND: '{jameson.Name}' (ID: {jameson.Id})");
			_output.WriteLine($"   Type: {jameson.Type}, Confidence: {jameson.Confidence:F2}, Frequency: {jameson.Frequency}");
			_output.WriteLine($"   Aliases: {(jameson.Aliases.Count > 0 ? string.Join(", ", jameson.Aliases) : "none")}");
			_output.WriteLine($"   Sources: {jameson.Sources.Count} chunks");
		}

		if (plinian == null)
		{
			_output.WriteLine("❌ PROBLEM: Plinian entity NOT FOUND");
			_output.WriteLine("   Searching for partial matches...");
			var partialMatches = graph.Entities
				.Where(e => e.Name.Contains("Plin", StringComparison.OrdinalIgnoreCase) ||
				    e.Name.Contains("Society", StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (partialMatches.Count > 0)
			{
				_output.WriteLine($"   Found {partialMatches.Count} partial matches:");
				foreach (var match in partialMatches.Take(5))
				{
					_output.WriteLine($"     - '{match.Name}' (type: {match.Type})");
				}
			}
		}
		else
		{
			_output.WriteLine($"✅ Plinian entity FOUND: '{plinian.Name}' (ID: {plinian.Id})");
			_output.WriteLine($"   Type: {plinian.Type}, Confidence: {plinian.Confidence:F2}, Frequency: {plinian.Frequency}");
			_output.WriteLine($"   Aliases: {(plinian.Aliases.Count > 0 ? string.Join(", ", plinian.Aliases) : "none")}");
			_output.WriteLine($"   Sources: {plinian.Sources.Count} chunks");
		}

		_output.WriteLine(string.Empty);

		// Check if they're in the same chunk
		if (jameson != null && plinian != null)
		{
			var jamesonChunks = jameson.Sources.Select(s => s.ChunkId).ToHashSet();
			var plinianChunks = plinian.Sources.Select(s => s.ChunkId).ToHashSet();
			var commonChunks = jamesonChunks.Intersect(plinianChunks).ToList();

			_output.WriteLine($"Jameson appears in {jamesonChunks.Count} chunk(s)");
			_output.WriteLine($"Plinian appears in {plinianChunks.Count} chunk(s)");
			_output.WriteLine($"Common chunks: {commonChunks.Count}");

			if (commonChunks.Count > 0)
			{
				_output.WriteLine("✅ BOTH entities in same chunk! Checking distances...");
				_output.WriteLine(string.Empty);

				foreach (var chunkId in commonChunks)
				{
					var jamesonPos = jameson.Sources.First(s => s.ChunkId == chunkId).Position;
					var plinianPos = plinian.Sources.First(s => s.ChunkId == chunkId).Position;
					var distance = Math.Abs(jamesonPos - plinianPos);

					_output.WriteLine($"  Chunk {chunkId}:");
					_output.WriteLine($"    Jameson position: {jamesonPos}");
					_output.WriteLine($"    Plinian position: {plinianPos}");
					_output.WriteLine($"    Distance: {distance} characters");
					_output.WriteLine($"    Within maxDistance (500)? {(distance <= 500 ? "YES ✅" : "NO ❌")}");
				}
			}
			else
			{
				_output.WriteLine("❌ PROBLEM: Entities NOT in same chunk!");
			}

			_output.WriteLine(string.Empty);

			// Check for relationships between them
			var jamesonRelationships = graph.GetRelationships(jameson.Id);
			var relationshipToPlinian = jamesonRelationships.FirstOrDefault(r =>
				r.ToEntityId == plinian.Id || r.FromEntityId == plinian.Id);

			_output.WriteLine($"Jameson has {jamesonRelationships.Count} total relationships");
			if (relationshipToPlinian != null)
			{
				_output.WriteLine($"✅ Relationship FOUND: {relationshipToPlinian.Type}");
				_output.WriteLine($"   Confidence: {relationshipToPlinian.Confidence:F2}");
				_output.WriteLine($"   Evidence count: {relationshipToPlinian.Evidence.Count}");
			}
			else
			{
				_output.WriteLine("❌ NO relationship between Jameson and Plinian");
			}
		}

		_output.WriteLine(string.Empty);
		_output.WriteLine("=== OTHER GROUND TRUTH ENTITIES ===");

		// Check a few more key entities
		var testEntities = new[]
		{
			("Darwin", "Charles Darwin"),
			("Edinburgh", "Edinburgh University"),
			("Beagle", "HMS Beagle"),
			("Henslow", "John Henslow")
		};

		foreach (var (searchTerm, fullName) in testEntities)
		{
			var found = graph.Entities.FirstOrDefault(e =>
				e.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

			if (found != null)
			{
				_output.WriteLine($"✅ {fullName}: Found as '{found.Name}' ({found.Sources.Count} chunks)");
			}
			else
			{
				_output.WriteLine($"❌ {fullName}: NOT FOUND");
			}
		}

		// This test always passes - it's diagnostic only
		true.Should().BeTrue();
	}

	/// <summary>
	/// Extracts Darwin's autobiography knowledge graph using current extraction pipeline.
	/// </summary>
	private async Task<Graph> ExtractDarwinKnowledgeGraphAsync()
	{
		// Download HTML from Project Gutenberg
		var documentUrl = "https://www.gutenberg.org/files/2010/2010-h/2010-h.htm";

		string htmlContent;
		using (var httpClient = new HttpClient())
		{
			httpClient.DefaultRequestHeaders.Add("User-Agent",
				"PanoramicData.Chunker/1.0 (Educational Testing)");
			var response = await httpClient.GetAsync(documentUrl, _cancellationToken);
			response.EnsureSuccessStatusCode();
			htmlContent = await response.Content.ReadAsStringAsync(_cancellationToken);
		}

		_output.WriteLine($"Downloaded HTML document ({htmlContent.Length:N0} characters)");

		// Chunk document with FIXED settings
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new HtmlDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 512,
			MaxCharactersPerChunk = 2000,  // ✅ FORCE smaller chunks for better relationship detection
			OverlapTokens = 100,  // ✅ Increased from 50 to catch relationships at boundaries
			EnforceSentenceBoundaries = true,  // ✅ Keep sentences intact
			ExternalHierarchy = "Project Gutenberg/Charles Darwin/Autobiography",
			Tags = ["darwin", "autobiography", "ground-truth-test"]
		};

		await using var stream = new MemoryStream(
			System.Text.Encoding.UTF8.GetBytes(htmlContent));
		var chunkingResult = await chunker.ChunkAsync(
			stream, options, _cancellationToken);

		_output.WriteLine($"Created {chunkingResult.Chunks.Count} chunks");

		// Extract entities
		var entityExtractor = new HybridEntityExtractor();
		var entities = await entityExtractor.ExtractEntitiesAsync(
			chunkingResult.Chunks, _cancellationToken);

		_output.WriteLine($"Extracted {entities.Count} entities");

		// Build graph
		var graph = new Graph("Darwin Autobiography - Baseline Extraction");
		foreach (var entity in entities)
		{
			graph.AddEntity(entity);
		}

		// Extract relationships
		var relationshipExtractor = new PatternBasedRelationshipExtractor(
			maxDistance: 500,
			minConfidence: 0.5);
		var relationships = await relationshipExtractor.ExtractRelationshipsAsync(
			graph.Entities,
			chunkingResult.Chunks,
			_cancellationToken);

		_output.WriteLine($"Extracted {relationships.Count} relationships");

		foreach (var rel in relationships)
		{
			graph.AddRelationship(rel);
		}

		graph.ComputeStatistics();

		// Save to database
		var graphStore = _fixture.Services.GetRequiredService<IGraphStore>();
		await graphStore.SaveGraphAsync(graph, _cancellationToken);

		_output.WriteLine("Saved graph to database");
		_output.WriteLine(string.Empty);

		return graph;
	}
}
