using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using PanoramicData.Chunker.Tests.Fixtures;
using PanoramicData.Chunker.Tests.Helpers;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

/// <summary>
/// Integration tests comparing Ollama LLM-based extraction vs Hybrid (baseline) extraction.
/// NOTE: These tests require Ollama running locally - skip if not available.
/// PERFORMANCE: Tests are SLOW (~25s per chunk with llama2, 47s with llama3).
///    Full Darwin extraction takes 4-8 hours. Use for validation only.
/// </summary>
[Collection("PostgreSQL")]
public class OllamaExtractionComparisonTests(ApacheAgeFixture fixture, ITestOutputHelper output)
	: IClassFixture<ApacheAgeFixture>
{
	private readonly ApacheAgeFixture _fixture = fixture;
	private readonly ITestOutputHelper _output = output;

	private readonly static CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	[Fact]
	public async Task OllamaExtraction_ShouldImprove_RecallVsBaseline()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");
		_output.WriteLine($"Loaded {groundTruth.Count} ground truth relationships");
		_output.WriteLine(string.Empty);

		// Check if Ollama is available
		var ollamaAvailable = await IsOllamaAvailableAsync();
		if (!ollamaAvailable)
		{
			_output.WriteLine("⚠ Ollama not available at http://localhost:11434");
			_output.WriteLine("Skipping test - install Ollama and run 'ollama serve' to enable LLM extraction tests");
			return;
		}

		_output.WriteLine("✓ Ollama is available");
		_output.WriteLine(string.Empty);

		// Act - Extract with both methods
		_output.WriteLine("=== BASELINE EXTRACTION (HybridEntityExtractor) ===");
		var baselineGraph = await ExtractDarwinGraphAsync(useOllama: false);
		var baselineResults = GroundTruthComparison.Compare(baselineGraph, groundTruth);
		_output.WriteLine($"Baseline Recall: {baselineResults.RecallRate:P2}, Precision: {baselineResults.Precision:P2}, F1: {baselineResults.F1Score:P2}");
		_output.WriteLine(string.Empty);

		_output.WriteLine("=== LLM EXTRACTION (OllamaEntityExtractor) ===");
		var llmGraph = await ExtractDarwinGraphAsync(useOllama: true);
		var llmResults = GroundTruthComparison.Compare(llmGraph, groundTruth);
		_output.WriteLine($"LLM Recall: {llmResults.RecallRate:P2}, Precision: {llmResults.Precision:P2}, F1: {llmResults.F1Score:P2}");
		_output.WriteLine(string.Empty);

		// Compare
		_output.WriteLine("=== COMPARISON ===");
		var recallImprovement = llmResults.RecallRate - baselineResults.RecallRate;
		var precisionChange = llmResults.Precision - baselineResults.Precision;
		var f1Improvement = llmResults.F1Score - baselineResults.F1Score;

		_output.WriteLine($"Recall improvement: {recallImprovement:P2}");
		_output.WriteLine($"Precision change: {precisionChange:P2}");
		_output.WriteLine($"F1 improvement: {f1Improvement:P2}");
		_output.WriteLine(string.Empty);

		// Show what LLM found that baseline missed
		var baselineEntityNames = baselineGraph.Entities.Select(e => e.Name.ToLowerInvariant()).ToHashSet();
		var llmOnlyEntities = llmGraph.Entities
			.Where(e => !baselineEntityNames.Contains(e.Name.ToLowerInvariant()))
			.OrderByDescending(e => e.Confidence)
			.Take(20)
			.ToList();

		_output.WriteLine($"=== NEW ENTITIES FOUND BY LLM ({llmOnlyEntities.Count} new) ===");
		foreach (var entity in llmOnlyEntities)
		{
			_output.WriteLine($"  + {entity.Name} ({entity.Type}, conf: {entity.Confidence:F2})");
		}
		_output.WriteLine(string.Empty);

		// Assert - LLM should improve recall (find more ground truth entities)
		llmResults.RecallRate.Should().BeGreaterThanOrEqualTo(baselineResults.RecallRate,
			"LLM extraction should find at least as many ground truth entities as baseline");

		llmResults.F1Score.Should().BeGreaterThanOrEqualTo(baselineResults.F1Score,
			"LLM extraction should have equal or better F1 score");
	}

	[Fact]
	public async Task OllamaExtraction_ShowExtractedEntities()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		// Check Ollama
		var ollamaAvailable = await IsOllamaAvailableAsync();
		if (!ollamaAvailable)
		{
			_output.WriteLine("⚠ Ollama not available - skipping");
			return;
		}

		// Act
		var graph = await ExtractDarwinGraphAsync(useOllama: true);

		// Show results
		_output.WriteLine($"=== EXTRACTED ENTITIES ({graph.Entities.Count}) ===");
		_output.WriteLine(string.Empty);

		var byType = graph.Entities.GroupBy(e => e.Type).OrderByDescending(g => g.Count());
		foreach (var group in byType)
		{
			_output.WriteLine($"{group.Key} ({group.Count()}):");
			foreach (var entity in group.OrderByDescending(e => e.Confidence).Take(10))
			{
				_output.WriteLine($"  - {entity.Name} (conf: {entity.Confidence:F2}, freq: {entity.Frequency})");
				if (entity.Aliases.Count > 0)
				{
					_output.WriteLine($"    Aliases: {string.Join(", ", entity.Aliases)}");
				}
			}
			_output.WriteLine(string.Empty);
		}

		// Ground truth check
		_output.WriteLine("=== GROUND TRUTH ENTITY COVERAGE ===");
		var groundTruthEntities = new[]
		{
			"Plinian Society", "Professor Jameson", "HMS Beagle", "Charles Darwin",
			"Edinburgh University", "Robert Grant", "Captain FitzRoy", "Galapagos Islands",
			"John Henslow", "Cambridge University", "Origin of Species"
		};

		foreach (var gt in groundTruthEntities)
		{
			var found = graph.Entities.Any(e =>
				e.Name.Equals(gt, StringComparison.OrdinalIgnoreCase) ||
				e.Name.Contains(gt, StringComparison.OrdinalIgnoreCase) ||
				gt.Contains(e.Name, StringComparison.OrdinalIgnoreCase) ||
				e.Aliases.Any(a => a.Equals(gt, StringComparison.OrdinalIgnoreCase)));

			_output.WriteLine($"  {(found ? "✓" : "✗")} {gt}");
		}

		// This is a diagnostic test
		true.Should().BeTrue();
	}

	[Fact]
	public async Task OllamaExtraction_SmallSample_ShouldExtractKeyEntities()
	{
		// Arrange - Small text sample
		var sampleText = @"
			The Plinian Society was encouraged and, I believe, founded by Professor Jameson.
			HMS Beagle sailed from Plymouth with Charles Darwin aboard.
			Darwin studied at Edinburgh University with Robert Grant, who taught marine zoology.
			Captain FitzRoy commanded the ship during its voyage to the Galapagos Islands.";

		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk { Content = sampleText }
		};

		// Check Ollama
		var ollamaAvailable = await IsOllamaAvailableAsync();
		if (!ollamaAvailable)
		{
			_output.WriteLine("⚠ Ollama not available - skipping");
			return;
		}

		// Act
		var extractor = new OllamaEntityExtractor();
		var entities = await extractor.ExtractEntitiesAsync(chunks, _cancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities from sample text:");
		foreach (var entity in entities.OrderByDescending(e => e.Confidence))
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, confidence: {entity.Confidence:F2})");
		}

		// Verify key entities
		entities.Should().NotBeEmpty("Should extract entities from sample text");

		var expectedEntities = new[] { "Darwin", "Plinian", "Society", "Jameson", "Beagle", "Grant" };
		var foundEntities = entities.Select(e => e.Name.ToLowerInvariant()).ToList();

		var foundCount = expectedEntities.Count(expected =>
			foundEntities.Any(found => found.Contains(expected, StringComparison.InvariantCultureIgnoreCase)));

		_output.WriteLine($"\nFound {foundCount}/{expectedEntities.Length} expected entities");

		foundCount.Should().BeGreaterThanOrEqualTo(3,
			"Should find at least half of the expected key entities");
	}

	/// <summary>
	/// Extracts Darwin's autobiography knowledge graph.
	/// </summary>
	private async Task<Graph> ExtractDarwinGraphAsync(bool useOllama)
	{
		// Download HTML
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

		_output.WriteLine($"Downloaded HTML ({htmlContent.Length:N0} characters)");

		// Chunk document
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new HtmlDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 512,
			MaxCharactersPerChunk = 2000,
			OverlapTokens = 100,
			EnforceSentenceBoundaries = true,
			ExternalHierarchy = "Project Gutenberg/Charles Darwin/Autobiography",
			Tags = ["darwin", "autobiography", useOllama ? "ollama-extraction" : "baseline-extraction"]
		};

		await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent));
		var chunkingResult = await chunker.ChunkAsync(stream, options, _cancellationToken);

		_output.WriteLine($"Created {chunkingResult.Chunks.Count} chunks");

		// Extract entities with chosen extractor
		IEntityExtractor entityExtractor = useOllama
			? new OllamaEntityExtractor()
			: new HybridEntityExtractor();

		var entities = await entityExtractor.ExtractEntitiesAsync(
			chunkingResult.Chunks, _cancellationToken);

		_output.WriteLine($"Extracted {entities.Count} entities using {entityExtractor.Name}");

		// Build graph
		var graph = new Graph($"Darwin Autobiography - {(useOllama ? "LLM" : "Baseline")} Extraction");
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

	/// <summary>
	/// Checks if Ollama is available at the default endpoint.
	/// </summary>
	private static async Task<bool> IsOllamaAvailableAsync()
	{
		try
		{
			using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
			var response = await httpClient.GetAsync("http://localhost:11434/api/tags", _cancellationToken);
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
