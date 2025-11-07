using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Fixtures;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

/// <summary>
/// Diagnostic test to search for ground truth entities in actual chunk content.
/// </summary>
[Collection("PostgreSQL")]
public class ChunkContentSearchTests(ApacheAgeFixture fixture, ITestOutputHelper output)
	: IClassFixture<ApacheAgeFixture>
{
	private readonly ApacheAgeFixture _fixture = fixture;
	private readonly ITestOutputHelper _output = output;

	private readonly static CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	[Fact]
	public async Task SearchChunks_ForGroundTruthEntities()
	{
		// Arrange
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
			Tags = ["darwin", "autobiography", "chunk-search-test"]
		};

		await using var stream = new MemoryStream(
			System.Text.Encoding.UTF8.GetBytes(htmlContent));
		var chunkingResult = await chunker.ChunkAsync(
			stream, options, _cancellationToken);

		_output.WriteLine($"Created {chunkingResult.Chunks.Count} chunks");
		_output.WriteLine("");

		// Ground truth entities to search for
		var searchTerms = new[]
		{
			"Plinian Society", "Professor Jameson", "HMS Beagle", "Beagle",
			"Robert Grant", "Captain FitzRoy", "FitzRoy", "Galapagos Islands",
			"Edinburgh University", "Cambridge University", "Origin of Species"
		};

		_output.WriteLine("=== SEARCHING CHUNKS FOR GROUND TRUTH ENTITIES ===");
		_output.WriteLine("");

		foreach (var term in searchTerms)
		{
			var foundChunks = new List<ChunkerBase>();

			foreach (var chunk in chunkingResult.Chunks)
			{
				var content = GetChunkContent(chunk);
				if (content.Contains(term, StringComparison.OrdinalIgnoreCase))
				{
					foundChunks.Add(chunk);
				}
			}

			_output.WriteLine($"{term}:");
			if (foundChunks.Count != 0)
			{
				_output.WriteLine($"  ✓ Found in {foundChunks.Count} chunk(s)");

				// Show first occurrence
				var firstChunk = foundChunks.First();
				var content = GetChunkContent(firstChunk);
				var index = content.IndexOf(term, StringComparison.OrdinalIgnoreCase);
				if (index >= 0)
				{
					var start = Math.Max(0, index - 50);
					var length = Math.Min(term.Length + 100, content.Length - start);
					var snippet = content.Substring(start, length);
					_output.WriteLine($"  Context: ...{snippet}...");
				}
			}
			else
			{
				_output.WriteLine($"  ✗ NOT FOUND in any chunk");
			}
			_output.WriteLine("");
		}

		// This test always passes - it's diagnostic only
		true.Should().BeTrue();
	}

	private static string GetChunkContent(ChunkerBase chunk)
	{
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}
		return string.Empty;
	}
}
