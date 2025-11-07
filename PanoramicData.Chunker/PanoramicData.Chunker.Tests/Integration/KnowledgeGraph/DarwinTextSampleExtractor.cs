using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Fixtures;
using PanoramicData.Chunker.Tests.Helpers;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

/// <summary>
/// Helper test to extract actual text samples from Darwin's autobiography.
/// This helps us understand why relationship patterns are failing.
/// </summary>
[Collection("PostgreSQL")]
public class DarwinTextSampleExtractor(ApacheAgeFixture fixture, ITestOutputHelper output)
	: IClassFixture<ApacheAgeFixture>
{
	private readonly ApacheAgeFixture _fixture = fixture;
	private readonly ITestOutputHelper _output = output;

	private readonly static CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	[Fact]
	public async Task ExtractTextSamples_ForGroundTruthRelationships()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");

		_output.WriteLine($"Loaded {groundTruth.Count} ground truth relationships");
		_output.WriteLine("");

		// Download and chunk the document
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

		// Chunk document with FIXED settings to prevent oversized chunks
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new HtmlDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 512,
			MaxCharactersPerChunk = 2000,  // ✅ FORCE smaller chunks!
			OverlapTokens = 100,  // ✅ Increased overlap to catch relationships at boundaries
			EnforceSentenceBoundaries = true,  // ✅ Keep sentences intact
			ExternalHierarchy = "Project Gutenberg/Charles Darwin/Autobiography",
			Tags = ["darwin", "autobiography", "text-sample-extraction"]
		};

		await using var stream = new MemoryStream(
			System.Text.Encoding.UTF8.GetBytes(htmlContent));
		var chunkingResult = await chunker.ChunkAsync(
			stream, options, _cancellationToken);

		_output.WriteLine($"Created {chunkingResult.Chunks.Count} chunks");
		_output.WriteLine($"Average chunk size: {chunkingResult.Chunks.Average(c => GetChunkContent(c).Length):F0} characters");
		_output.WriteLine("");

		// For each ground truth relationship, find chunks containing the entities
		_output.WriteLine("=== TEXT SAMPLES FROM DARWIN'S AUTOBIOGRAPHY ===");
		_output.WriteLine("");

		foreach (var gt in groundTruth.Take(15)) // First 15 relationships
		{
			_output.WriteLine($"Relationship: {gt.Entity1} -> {gt.RelationType} -> {gt.Entity2}");
			_output.WriteLine($"Section: {gt.Section}");
			_output.WriteLine("");

			// Find chunks containing both entities
			var relevantChunks = FindChunksContainingEntities(chunkingResult.Chunks, gt.Entity1, gt.Entity2);

			if (relevantChunks.Count != 0)
			{
				_output.WriteLine($"Found {relevantChunks.Count} chunk(s) containing both entities:");
				_output.WriteLine("");

				foreach (var chunk in relevantChunks.Take(2)) // Show first 2 matches
				{
					var content = GetChunkContent(chunk);
					if (!string.IsNullOrWhiteSpace(content))
					{
						// Extract context window around entities
						var contextWindow = ExtractContextWindow(content, gt.Entity1, gt.Entity2, 300);
						_output.WriteLine($"  Chunk {chunk.SequenceNumber} ({content.Length} chars):");
						_output.WriteLine($"Context: ...{contextWindow}...");
						_output.WriteLine("");
					}
				}
			}
			else
			{
				// Try finding chunks with just one entity
				var chunksWithEntity1 = FindChunksContainingText(chunkingResult.Chunks, gt.Entity1);
				var chunksWithEntity2 = FindChunksContainingText(chunkingResult.Chunks, gt.Entity2);

				if (chunksWithEntity1.Count != 0)
				{
					_output.WriteLine($"  Found '{gt.Entity1}' in {chunksWithEntity1.Count} chunk(s)");
					var sample = GetChunkContent(chunksWithEntity1.First());
					if (!string.IsNullOrWhiteSpace(sample))
					{
						_output.WriteLine($"  Sample: {TruncateText(sample, 200)}");
					}
				}
				else
				{
					_output.WriteLine($"  ⚠️ '{gt.Entity1}' NOT FOUND in any chunk");
				}

				if (chunksWithEntity2.Count != 0)
				{
					_output.WriteLine($"  Found '{gt.Entity2}' in {chunksWithEntity2.Count} chunk(s)");
					var sample = GetChunkContent(chunksWithEntity2.First());
					if (!string.IsNullOrWhiteSpace(sample))
					{
						_output.WriteLine($"  Sample: {TruncateText(sample, 200)}");
					}
				}
				else
				{
					_output.WriteLine($"  ⚠️ '{gt.Entity2}' NOT FOUND in any chunk");
				}

				_output.WriteLine($"  ⚠️ Entities are in SEPARATE chunks (chunking boundary issue)");
			}

			_output.WriteLine("");
			_output.WriteLine("---");
			_output.WriteLine("");
		}

		// This test always passes - it's just for extracting samples
		true.Should().BeTrue();
	}

	private static List<ChunkerBase> FindChunksContainingEntities(
		IReadOnlyList<ChunkerBase> chunks,
		string entity1,
		string entity2) => [.. chunks
			.Where(c =>
			{
				var content = GetChunkContent(c);
				return ContainsEntity(content, entity1) && ContainsEntity(content, entity2);
			})];

	private static List<ChunkerBase> FindChunksContainingText(
		IReadOnlyList<ChunkerBase> chunks,
		string searchText) => [.. chunks
			.Where(c =>
			{
				var content = GetChunkContent(c);
				return ContainsEntity(content, searchText);
			})];

	private static bool ContainsEntity(string content, string entity)
	{
		if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(entity))
			return false;

		// Case-insensitive search, also check for partial matches
		var normalizedContent = content.ToLowerInvariant();
		var normalizedEntity = entity.ToLowerInvariant();

		// Check exact match
		if (normalizedContent.Contains(normalizedEntity))
			return true;

		// Check for word-by-word match (e.g., "Charles Darwin" might appear as just "Darwin")
		var entityWords = normalizedEntity.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return entityWords.All(word => normalizedContent.Contains(word));
	}

	private static string GetChunkContent(ChunkerBase chunk)
	{
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}
		return string.Empty;
	}

	private static string ExtractContextWindow(string content, string entity1, string entity2, int windowSize)
	{
		// Find positions of both entities
		var pos1 = content.IndexOf(entity1, StringComparison.OrdinalIgnoreCase);
		var pos2 = content.IndexOf(entity2, StringComparison.OrdinalIgnoreCase);

		if (pos1 < 0 || pos2 < 0)
			return TruncateText(content, windowSize);

		// Find the earlier and later positions
		var earlierPos = Math.Min(pos1, pos2);
		var laterPos = Math.Max(pos1, pos2);

		// Calculate window start and end to capture both entities and context
		var start = Math.Max(0, earlierPos - windowSize / 2);
		var end = Math.Min(content.Length, laterPos + windowSize / 2);

		// Extract the window
		var window = content[start..end];

		// Highlight the entities
		window = HighlightEntitiesInText(window, entity1, entity2);

		return window;
	}

	private static string HighlightEntitiesInText(string text, string entity1, string entity2)
	{
		// Use regex for case-insensitive replacement
		var result = System.Text.RegularExpressions.Regex.Replace(
			text,
			System.Text.RegularExpressions.Regex.Escape(entity1),
			m => $">>>{m.Value}<<<",
			System.Text.RegularExpressions.RegexOptions.IgnoreCase);

		result = System.Text.RegularExpressions.Regex.Replace(
			result,
			System.Text.RegularExpressions.Regex.Escape(entity2),
			m => $">>>{m.Value}<<<",
			System.Text.RegularExpressions.RegexOptions.IgnoreCase);

		return result;
	}

	private static string HighlightEntities(string content, string entity1, string entity2)
	{
		// Highlight entities with >>> <<< markers
		var result = content;

		// Try to find and highlight entity occurrences (case-insensitive)
		var entity1Index = result.IndexOf(entity1, StringComparison.OrdinalIgnoreCase);
		if (entity1Index >= 0)
		{
			var actualText = result.Substring(entity1Index, entity1.Length);
			result = result.Replace(actualText, $">>>{actualText}<<<", StringComparison.OrdinalIgnoreCase);
		}

		var entity2Index = result.IndexOf(entity2, StringComparison.OrdinalIgnoreCase);
		if (entity2Index >= 0)
		{
			var actualText = result.Substring(entity2Index, entity2.Length);
			result = result.Replace(actualText, $">>>{actualText}<<<", StringComparison.OrdinalIgnoreCase);
		}

		return result;
	}

	private static string TruncateText(string text, int maxLength)
	{
		if (text.Length <= maxLength)
			return text;

		return string.Concat(text.AsSpan(0, maxLength), "...");
	}
}
