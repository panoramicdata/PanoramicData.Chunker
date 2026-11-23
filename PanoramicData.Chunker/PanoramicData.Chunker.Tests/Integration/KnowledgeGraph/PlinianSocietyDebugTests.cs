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
/// Deep dive debug test - extract from the exact chunk that contains "Plinian Society".
/// </summary>
[Collection("PostgreSQL")]
[Trait("Category", "Integration")]
public class PlinianSocietyDebugTests(ApacheAgeFixture fixture, ITestOutputHelper output)
	: DarwinIntegrationTestBase(fixture, output)
{
	[Fact]
	public async Task Debug_ExtractFromPlinianSocietyChunk()
	{
		// Arrange
		var htmlContent = await DownloadDarwinHtmlAsync();

		// Chunk document
		var chunkingResult = await ChunkDarwinHtmlAsync(htmlContent);

		// Find the chunk containing "Plinian Society"
		var plinianChunk = FindPlinianSocietyChunk(chunkingResult);

		plinianChunk.Should().NotBeNull("Should find chunk containing 'Plinian Society'");

		_output.WriteLine("=== CHUNK CONTAINING 'PLINIAN SOCIETY' ===");
		_output.WriteLine($"Chunk type: {plinianChunk!.GetType().Name}");
		_output.WriteLine($"Sequence: {plinianChunk.SequenceNumber}");
		_output.WriteLine($"Content length: {GetChunkContent(plinianChunk).Length} characters");
		_output.WriteLine(string.Empty);

		var chunkText = GetChunkContent(plinianChunk);
		_output.WriteLine("Content:");
		_output.WriteLine(chunkText);
		_output.WriteLine(string.Empty);

		// Now extract entities from just this chunk
		var extractor = new CapitalizationEntityExtractor(minOccurrences: 1, minWordLength: 2);
		var entities = await extractor.ExtractEntitiesAsync([plinianChunk], _cancellationToken);

		WriteExtractedEntities(entities);

		// Check if "Plinian Society" was extracted
		WritePlinianExtractionOutcome(entities);

		// This test always passes - it's diagnostic only
		true.Should().BeTrue();
	}

	private static ChunkerBase? FindPlinianSocietyChunk(ChunkingResult chunkingResult)
	{
		ChunkerBase? plinianChunk = null;
		foreach (var chunk in chunkingResult.Chunks)
		{
			var content = GetChunkContent(chunk);
			if (content.Contains("Plinian Society", StringComparison.OrdinalIgnoreCase))
			{
				plinianChunk = chunk;
				break;
			}
		}
		return plinianChunk;
	}

	private void WriteExtractedEntities(List<Entity> entities)
	{
		_output.WriteLine($"=== EXTRACTED {entities.Count} ENTITIES FROM THIS CHUNK ===");
		foreach (var entity in entities.OrderByDescending(e => e.Confidence))
		{
			_output.WriteLine($"  {entity.Name} (conf: {entity.Confidence:F2}, freq: {entity.Frequency})");
		}
		_output.WriteLine(string.Empty);
	}

	private void WritePlinianExtractionOutcome(List<Entity> entities)
	{
		var hasPlinianSociety = entities.Any(e =>
			e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase) &&
			e.Name.Contains("Society", StringComparison.OrdinalIgnoreCase));

		if (hasPlinianSociety)
		{
			_output.WriteLine("? 'Plinian Society' WAS EXTRACTED!");
		}
		else
		{
			_output.WriteLine("? 'Plinian Society' WAS NOT EXTRACTED!");
			_output.WriteLine(string.Empty);
			_output.WriteLine("Entities containing 'Plinian':");
			foreach (var entity in entities.Where(e => e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase)))
			{
				_output.WriteLine($"  - {entity.Name}");
			}
			_output.WriteLine(string.Empty);
			_output.WriteLine("Entities containing 'Society':");
			foreach (var entity in entities.Where(e => e.Name.Contains("Society", StringComparison.OrdinalIgnoreCase)))
			{
				_output.WriteLine($"  - {entity.Name}");
			}
		}
	}

}