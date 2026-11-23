using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using PanoramicData.Chunker.Tests.Fixtures;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

/// <summary>
/// Shared plumbing for the Darwin autobiography integration tests that download the Project Gutenberg text.
/// </summary>
public abstract class DarwinIntegrationTestBase(ApacheAgeFixture fixture, ITestOutputHelper output)
	: IClassFixture<ApacheAgeFixture>
{
	protected readonly ApacheAgeFixture _fixture = fixture;
	protected readonly ITestOutputHelper _output = output;

	protected readonly static CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	protected static async Task<string> DownloadDarwinHtmlAsync()
	{
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
		return htmlContent;
	}

	protected static Task<ChunkingResult> ChunkDarwinHtmlAsync(string htmlContent)
		=> ChunkDarwinHtmlAsync(htmlContent, new ChunkingOptions
		{
			MaxTokens = 512,
			MaxCharactersPerChunk = 2000,
			OverlapTokens = 100,
			EnforceSentenceBoundaries = true
		});

	protected static Task<ChunkingResult> ChunkDarwinHtmlAsync(string htmlContent, string tag)
		=> ChunkDarwinHtmlAsync(htmlContent, new ChunkingOptions
		{
			MaxTokens = 512,
			MaxCharactersPerChunk = 2000,
			OverlapTokens = 100,
			EnforceSentenceBoundaries = true,
			ExternalHierarchy = "Project Gutenberg/Charles Darwin/Autobiography",
			Tags = ["darwin", "autobiography", tag]
		});

	protected static string GetChunkContent(ChunkerBase chunk)
	{
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}
		return string.Empty;
	}

	protected async Task<Graph> BuildDarwinGraphAsync(ChunkingResult chunkingResult, IEntityExtractor entityExtractor, string graphName)
	{
		var entities = await entityExtractor.ExtractEntitiesAsync(chunkingResult.Chunks, _cancellationToken);

		_output.WriteLine($"Extracted {entities.Count} entities using {entityExtractor.Name}");

		var graph = new Graph(graphName);
		foreach (var entity in entities)
		{
			graph.AddEntity(entity);
		}

		var relationshipExtractor = new PatternBasedRelationshipExtractor(maxDistance: 500, minConfidence: 0.5);
		var relationships = await relationshipExtractor.ExtractRelationshipsAsync(graph.Entities, chunkingResult.Chunks, _cancellationToken);

		_output.WriteLine($"Extracted {relationships.Count} relationships");

		foreach (var rel in relationships)
		{
			graph.AddRelationship(rel);
		}

		graph.ComputeStatistics();
		return graph;
	}

	private static async Task<ChunkingResult> ChunkDarwinHtmlAsync(string htmlContent, ChunkingOptions options)
	{
		var chunker = new HtmlDocumentChunker(new CharacterBasedTokenCounter());

		await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent));
		return await chunker.ChunkAsync(stream, options, _cancellationToken);
	}
}