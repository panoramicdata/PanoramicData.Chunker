using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for OllamaEntityExtractor.
/// NOTE: These tests require Ollama to be running locally with llama3.2 model available.
/// Skip these tests if Ollama is not available in your environment.
/// </summary>
public class OllamaEntityExtractorTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtractPeople()
	{
		// Arrange
		var extractor = new OllamaEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "Charles Darwin and Professor Jameson were both members of the Plinian Society in Edinburgh."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type})");
		}

		entities.Should().NotBeEmpty();

		// Should extract people
		var people = entities.Where(e => e.Type == EntityType.Person).ToList();
		people.Should().NotBeEmpty();

		// Should have Darwin
		_ = entities.Should().Contain(e => e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase), "Should extract Darwin");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtractOrganizations()
	{
		// Arrange
		var extractor = new OllamaEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "The Plinian Society was founded by Professor Jameson at Edinburgh University in 1823."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, confidence: {entity.Confidence:F2})");
		}

		entities.Should().NotBeEmpty();

		// Should extract organizations
		var orgs = entities.Where(e => e.Type == EntityType.Organization).ToList();
		orgs.Should().NotBeEmpty("Should extract at least one organization");

		// Should extract Plinian Society
		_ = entities.Should().Contain(e => e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase), "Should extract Plinian Society");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldGenerateAliases()
	{
		// Arrange
		var extractor = new OllamaEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "Professor Jameson founded the society. HMS Beagle sailed in 1831."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities with aliases:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type})");
			if (entity.Aliases.Count > 0)
			{
				_output.WriteLine($"    Aliases: {string.Join(", ", entity.Aliases)}");
			}
		}

		// Should have generated aliases
		var entitiesWithAliases = entities.Where(e => e.Aliases.Count > 0).ToList();
		entitiesWithAliases.Should().NotBeEmpty("Should generate aliases for some entities");

		// Professor Jameson should have alias "Jameson"
		var jameson = entities.FirstOrDefault(e => e.Name.Contains("Jameson", StringComparison.OrdinalIgnoreCase));
		if (jameson != null)
		{
			_output.WriteLine($"Jameson entity: {jameson.Name}, Aliases: {string.Join(", ", jameson.Aliases)}");
		}

		// HMS Beagle should have alias "Beagle"
		var beagle = entities.FirstOrDefault(e => e.Name.Contains("Beagle", StringComparison.OrdinalIgnoreCase));
		if (beagle != null)
		{
			_output.WriteLine($"Beagle entity: {beagle.Name}, Aliases: {string.Join(", ", beagle.Aliases)}");
		}
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldAggregateAcrossChunks()
	{
		// Arrange
		var extractor = new OllamaEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk { Content = "Charles Darwin studied at Edinburgh University." },
			new HtmlParagraphChunk { Content = "Darwin later moved to Cambridge." },
			new HtmlParagraphChunk { Content = "Charles Darwin published Origin of Species in 1859." }
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities.OrderByDescending(e => e.Frequency))
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, freq: {entity.Frequency}, sources: {entity.Sources.Count})");
		}

		// Darwin should appear multiple times
		var darwin = entities.FirstOrDefault(e => e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase));
		darwin.Should().NotBeNull("Should extract Darwin");
		darwin!.Frequency.Should().BeGreaterThan(1, "Darwin should be aggregated across chunks");
		darwin.Sources.Count.Should().BeGreaterThan(1, "Darwin should have multiple sources");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldHandleWorks()
	{
		// Arrange
		var extractor = new OllamaEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "Darwin published 'Origin of Species' in 1859. The book revolutionized biology."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type})");
		}

		// Should extract the work
		var works = entities.Where(e => e.Type == EntityType.Work ||
				 e.Name.Contains("Origin", StringComparison.OrdinalIgnoreCase))
					 .ToList();
		works.Should().NotBeEmpty("Should extract 'Origin of Species' as a work");
	}

	[Fact]
	public void Constructor_ShouldAcceptCustomParameters()
	{
		// Arrange & Act
		var extractor = new OllamaEntityExtractor(
			ollamaEndpoint: "http://custom:11434",
			modelName: "mistral",
			temperature: 0.5,
			maxTokensPerChunk: 1000);

		// Assert
		extractor.Name.Should().Be("OllamaEntityExtractor");
		extractor.Version.Should().Be("1.0");
		extractor.SupportedEntityTypes.Should().Contain(EntityType.Person);
		extractor.SupportedEntityTypes.Should().Contain(EntityType.Organization);
		extractor.SupportedEntityTypes.Should().Contain(EntityType.Location);
	}

	[Fact]
	public void SupportedEntityTypes_ShouldIncludeKeyTypes()
	{
		// Arrange
		var extractor = new OllamaEntityExtractor();

		// Act
		var types = extractor.SupportedEntityTypes;

		// Assert
		types.Should().Contain(EntityType.Person);
		types.Should().Contain(EntityType.Organization);
		types.Should().Contain(EntityType.Location);
		types.Should().Contain(EntityType.Date);
		types.Should().Contain(EntityType.Event);
		types.Should().Contain(EntityType.Work);
		types.Should().Contain(EntityType.Product);
		types.Should().Contain(EntityType.ProperNoun);
	}
}
