using AwesomeAssertions;
using PanoramicData.Chunker.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

public class BasicEntityNormalizerTests
{
	private readonly BasicEntityNormalizer _normalizer = new();

	[Theory]
	[InlineData("  Charles   DARWIN ", EntityType.Person, "charles darwin")]
	[InlineData("HTTPS://www.Example.com/", EntityType.Url, "example.com")]
	[InlineData("Me@Example.COM", EntityType.Email, "me@example.com")]
	[InlineData("+44 (0)20 7946 0958", EntityType.Phone, "4402079460958")]
	[InlineData("$1,200.50", EntityType.Money, "1200.50")]
	[InlineData("45 %", EntityType.Percent, "45")]
	[InlineData("12 March 1859", EntityType.Date, "12 march 1859")]
	[InlineData("   ", EntityType.Person, "")]
	public void Normalize_AppliesTypeSpecificRules(string name, EntityType type, string expected)
	{
		_ = _normalizer.Normalize(name, type).Should().Be(expected);
	}

	[Theory]
	[InlineData("Charles Darwin", "charles  darwin", true)]
	[InlineData("Charles Darwin", "Robert Grant", false)]
	[InlineData("", "Darwin", false)]
	public void AreEquivalent_ComparesNormalisedNames(string first, string second, bool expected)
	{
		_ = _normalizer.AreEquivalent(first, second, EntityType.Person).Should().Be(expected);
	}

	[Fact]
	public void GenerateAliases_ForTwoPartPersonNames_GivesSurnameFirstAndInitial()
	{
		_ = _normalizer.GenerateAliases("Charles Darwin", EntityType.Person)
			.Should().BeEquivalentTo("Darwin, Charles", "C. Darwin");
	}

	[Fact]
	public void GenerateAliases_ForThreePartPersonNames_GivesShortAndInitialForms()
	{
		_ = _normalizer.GenerateAliases("Charles Robert Darwin", EntityType.Person)
			.Should().BeEquivalentTo("Charles Darwin", "C. R. Darwin", "Darwin, Charles Robert");
	}

	[Fact]
	public void GenerateAliases_ForOrganisations_DropsSuffixesAndBuildsAnAcronym()
	{
		var aliases = _normalizer.GenerateAliases("Acme Widgets Inc", EntityType.Organization);

		_ = aliases.Should().Contain("Acme Widgets").And.Contain("AWI");
	}

	[Theory]
	[InlineData("cats", "cat")]
	[InlineData("dog", "dogs")]
	public void GenerateAliases_ForKeywords_TogglesThePlural(string keyword, string expected)
	{
		_ = _normalizer.GenerateAliases(keyword, EntityType.Keyword).Should().Contain(expected);
	}

	[Fact]
	public void GenerateAliases_ForOtherTypesOrBlankNames_IsEmpty()
	{
		_ = _normalizer.GenerateAliases("Edinburgh", EntityType.Location).Should().BeEmpty();
		_ = _normalizer.GenerateAliases(" ", EntityType.Person).Should().BeEmpty();
	}
}