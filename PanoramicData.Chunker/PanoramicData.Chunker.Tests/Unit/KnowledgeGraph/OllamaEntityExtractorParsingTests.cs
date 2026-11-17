using AwesomeAssertions;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Reflection;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

public class OllamaEntityExtractorParsingTests
{
	private const string SourceText = "Charles Darwin sailed on HMS Beagle with Captain FitzRoy.";

	private static List<Entity> Parse(string response, Guid? chunkId = null)
	{
		var extractor = new OllamaEntityExtractor();
		var method = typeof(OllamaEntityExtractor).GetMethod("ParseNerResponse", BindingFlags.Instance | BindingFlags.NonPublic)!;
		return (List<Entity>)method.Invoke(extractor, [response, SourceText, chunkId ?? Guid.NewGuid()])!;
	}

	[Fact]
	public void ParseNerResponse_ReadsEntitiesFromEmbeddedJson()
	{
		var chunkId = Guid.NewGuid();
		const string response = "Here you go: {\"entities\":[{\"name\":\"Charles Darwin\",\"type\":\"PERSON\"},{\"name\":\"HMS Beagle\",\"type\":\"WORK\"}]} done";

		var entities = Parse(response, chunkId);

		_ = entities.Select(e => e.Name).Should().Equal("Charles Darwin", "HMS Beagle");
		_ = entities[0].Type.Should().Be(EntityType.Person);
		_ = entities[1].Type.Should().Be(EntityType.Work);
		_ = entities[0].Confidence.Should().Be(0.9);
		_ = entities[0].Frequency.Should().Be(1);
		_ = entities[0].Sources.Should().ContainSingle().Which.Should().Match<EntitySource>(s => s.ChunkId == chunkId && s.Position == 0);
		_ = entities[0].Aliases.Should().Contain("Darwin");
		_ = entities[1].Aliases.Should().Contain("Beagle");
	}

	[Theory]
	[InlineData("no json here")]
	[InlineData("{\"other\":[]}")]
	[InlineData("{not valid json}")]
	[InlineData("\"entities\": [{\"name\":\"Captain FitzRoy\",\"type\":\"PERSON\"}]")]
	[InlineData("{\"entities\":[{\"name\":\"\",\"type\":\"PERSON\"},{\"name\":\"Nameless\"}]}")]
	public void ParseNerResponse_ReturnsNothingForUnusableResponses(string response)
	{
		_ = Parse(response).Should().BeEmpty();
	}


	[Fact]
	public void ParseNerResponse_MapsUnknownTypesToProperNoun()
	{
		var entities = Parse("{\"entities\":[{\"name\":\"Darwin\",\"type\":\"MYSTERY\"}]}");

		_ = entities.Should().ContainSingle().Which.Type.Should().Be(EntityType.ProperNoun);
	}

	[Fact]
	public void ParseNerResponse_AddsNoSourceWhenTheNameIsNotInTheText()
	{
		var entities = Parse("{\"entities\":[{\"name\":\"Wallace\",\"type\":\"PERSON\"}]}");

		_ = entities.Should().ContainSingle().Which.Sources.Should().BeEmpty();
	}
}
