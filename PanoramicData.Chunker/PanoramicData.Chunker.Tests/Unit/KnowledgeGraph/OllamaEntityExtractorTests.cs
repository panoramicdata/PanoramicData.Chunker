using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for OllamaEntityExtractor.
/// NOTE: These tests require Ollama to be running locally with llama3.2 model available.
/// These tests need Ollama; they carry the Integration trait so CI excludes them.
/// </summary>
[Trait("Category", "Integration")]
public class OllamaEntityExtractorTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtractPeople()
	{
		var entities = await ExtractAsync("Charles Darwin and Professor Jameson were both members of the Plinian Society in Edinburgh.");

		entities.Should().NotBeEmpty();
		WithType(entities, EntityType.Person).Should().NotBeEmpty();
		_ = entities.Should().Contain(e => e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase), "Should extract Darwin");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtractOrganizations()
	{
		var entities = await ExtractAsync("The Plinian Society was founded by Professor Jameson at Edinburgh University in 1823.");

		entities.Should().NotBeEmpty();
		WithType(entities, EntityType.Organization).Should().NotBeEmpty("Should extract at least one organization");
		_ = entities.Should().Contain(e => e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase), "Should extract Plinian Society");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldGenerateAliases()
	{
		var entities = await ExtractAsync("Professor Jameson founded the society. HMS Beagle sailed in 1831.");

		var entitiesWithAliases = entities.Where(e => e.Aliases.Count > 0).ToList();
		entitiesWithAliases.Should().NotBeEmpty("Should generate aliases for some entities");

		foreach (var fragment in new[] { "Jameson", "Beagle" })
		{
			var entity = entities.FirstOrDefault(e => e.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
			if (entity != null)
			{
				_output.WriteLine($"{fragment} entity: {entity.Name}, Aliases: {string.Join(", ", entity.Aliases)}");
			}
		}
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldAggregateAcrossChunks()
	{
		var entities = await ExtractAsync(
			"Charles Darwin studied at Edinburgh University.",
			"Darwin later moved to Cambridge.",
			"Charles Darwin published Origin of Species in 1859.");

		var darwin = entities.FirstOrDefault(e => e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase));
		darwin.Should().NotBeNull("Should extract Darwin");
		darwin!.Frequency.Should().BeGreaterThan(1, "Darwin should be aggregated across chunks");
		darwin.Sources.Count.Should().BeGreaterThan(1, "Darwin should have multiple sources");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldHandleWorks()
	{
		var entities = await ExtractAsync("Darwin published 'Origin of Species' in 1859. The book revolutionized biology.");

		var works = entities.Where(e => e.Type == EntityType.Work || e.Name.Contains("Origin", StringComparison.OrdinalIgnoreCase)).ToList();
		works.Should().NotBeEmpty("Should extract 'Origin of Species' as a work");
	}

	[Fact]
	public void Constructor_ShouldAcceptCustomParameters()
	{
		var extractor = new OllamaEntityExtractor(
			ollamaEndpoint: "http://custom:11434",
			modelName: "mistral",
			temperature: 0.5,
			maxTokensPerChunk: 1000);

		extractor.Name.Should().Be("OllamaEntityExtractor");
		extractor.Version.Should().Be("1.0");
		extractor.SupportedEntityTypes.Should().Contain(EntityType.Person);
		extractor.SupportedEntityTypes.Should().Contain(EntityType.Organization);
		extractor.SupportedEntityTypes.Should().Contain(EntityType.Location);
	}

	[Fact]
	public void SupportedEntityTypes_ShouldIncludeKeyTypes()
	{
		var types = new OllamaEntityExtractor().SupportedEntityTypes;

		types.Should().Contain(
		[
			EntityType.Person,
			EntityType.Organization,
			EntityType.Location,
			EntityType.Date,
			EntityType.Event,
			EntityType.Work,
			EntityType.Product,
			EntityType.ProperNoun
		]);
	}

	private async Task<List<Entity>> ExtractAsync(params string[] paragraphs)
	{
		var chunks = paragraphs.Select(content => (ChunkerBase)new HtmlParagraphChunk { Content = content }).ToList();

		var entities = await new OllamaEntityExtractor().ExtractEntitiesAsync(chunks, CancellationToken);

		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, freq: {entity.Frequency}, aliases: {string.Join(", ", entity.Aliases)})");
		}

		return entities;
	}

	private static List<Entity> WithType(List<Entity> entities, EntityType type)
		=> [.. entities.Where(e => e.Type == type)];
}