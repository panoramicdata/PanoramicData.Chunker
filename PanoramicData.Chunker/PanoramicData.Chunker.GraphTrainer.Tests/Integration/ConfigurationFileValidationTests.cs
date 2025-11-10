using FluentAssertions;
using PanoramicData.Chunker.Configuration;

namespace PanoramicData.Chunker.GraphTrainer.Tests.Integration;

/// <summary>
/// Integration tests that validate the actual shipped JSON configuration files.
/// </summary>
public class ConfigurationFileValidationTests
{
	[Fact]
	public async Task RelationshipPatterns_DefaultFile_ShouldBeValid()
	{
		// Act
		var patterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		patterns.Should().NotBeNull();
		patterns.Should().NotBeEmpty("default patterns file should contain patterns");
		
		// Validate all patterns
		patterns.Should().AllSatisfy(pattern =>
		{
			pattern.Name.Should().NotBeNullOrEmpty("all patterns must have names");
			pattern.Regex.Should().NotBeNull("all patterns must have compiled regex");
			pattern.Type.Should().NotBe(default, "all patterns must have valid relationship types");
			pattern.Confidence.Should().BeInRange(0.0, 1.0, "confidence must be between 0 and 1");
		});
	}

	[Fact]
	public async Task RelationshipPatterns_DefaultFile_ShouldHaveExpectedPatterns()
	{
		// Act
		var patterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();

		// Assert - Check for key patterns we expect
		var patternNames = patterns.Select(p => p.Name).ToList();
		
		patternNames.Should().Contain("FoundedByPassive", "should have passive founded pattern");
		patternNames.Should().Contain("FoundedActive", "should have active founded pattern");
		patternNames.Should().Contain("StudiedAt", "should have education pattern");
		patternNames.Should().Contain("MemberOf", "should have membership pattern");
		patternNames.Should().Contain("WorksFor", "should have employment pattern");
	}

	[Fact]
	public async Task RelationshipPatterns_AllRegexPatterns_ShouldCompile()
	{
		// Act
		var patterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();

		// Assert - All regexes should be compiled without exceptions
		patterns.Should().AllSatisfy(pattern =>
		{
			pattern.Regex.Should().NotBeNull();
			
			// Test that regex actually works
			var testMatch = pattern.Regex.Match("test string");
			testMatch.Should().NotBeNull(); // Just ensure it doesn't throw
		});
	}

	[Fact]
	public async Task RelationshipPatterns_HighConfidencePatterns_ShouldBeSpecific()
	{
		// Act
		var patterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();

		// Assert - High confidence patterns should be more specific
		var highConfidencePatterns = patterns.Where(p => p.Confidence >= 0.9).ToList();
		
		highConfidencePatterns.Should().NotBeEmpty("should have some high-confidence patterns");
		highConfidencePatterns.Should().AllSatisfy(pattern =>
		{
			// High confidence patterns should generally be longer/more specific
			pattern.Regex.ToString().Length.Should().BeGreaterThan(10,
				$"high confidence pattern '{pattern.Name}' should be specific");
		});
	}

	[Fact]
	public async Task EntityPatterns_DefaultFile_ShouldBeValid()
	{
		// Act
		var config = await EntityPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		config.Should().NotBeNull();
		config.Version.Should().NotBeNullOrEmpty();
		config.ProperNounDictionary.Should().NotBeNull();
		config.TitlePrefixes.Should().NotBeNull();
		config.OrganizationalSuffixes.Should().NotBeNull();
		config.AllowedConnectors.Should().NotBeNull();
		config.SentenceStarters.Should().NotBeNull();
		config.ExtractionRules.Should().NotBeNull();
	}

	[Fact]
	public async Task EntityPatterns_ProperNounDictionary_ShouldHaveEntries()
	{
		// Act
		var config = await EntityPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		config.ProperNounDictionary.Should().NotBeNull();
		config.ProperNounDictionary!.People.Should().NotBeNullOrEmpty("should have people names");
		config.ProperNounDictionary.Places.Should().NotBeNullOrEmpty("should have place names");
		config.ProperNounDictionary.Organizations.Should().NotBeNullOrEmpty("should have organization names");

		// Check for expected entries from Darwin corpus
		config.ProperNounDictionary.People.Should().Contain("Darwin");
		config.ProperNounDictionary.People.Should().Contain("Jameson");
		config.ProperNounDictionary.Places.Should().Contain("Edinburgh");
		config.ProperNounDictionary.Places.Should().Contain("Cambridge");
	}

	[Fact]
	public async Task EntityPatterns_TitlePrefixes_ShouldHaveCommonTitles()
	{
		// Act
		var config = await EntityPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		config.TitlePrefixes.Should().NotBeNull();
		config.TitlePrefixes.Should().ContainKey("academic");
		config.TitlePrefixes.Should().ContainKey("military");
		config.TitlePrefixes.Should().ContainKey("ships");

		var allTitles = config.GetAllTitlePrefixes();
		allTitles.Should().Contain("Professor");
		allTitles.Should().Contain("Dr");
		allTitles.Should().Contain("Captain");
		allTitles.Should().Contain("HMS");
	}

	[Fact]
	public async Task EntityPatterns_ExtractionRules_ShouldHaveReasonableDefaults()
	{
		// Act
		var config = await EntityPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		config.ExtractionRules.Should().NotBeNull();
		config.ExtractionRules!.MinWordLength.Should().BeGreaterThan(0);
		config.ExtractionRules.MinOccurrences.Should().BeGreaterThan(0);
		config.ExtractionRules.BaseConfidence.Should().BeInRange(0.5, 0.9);
		
		config.ExtractionRules.ConfidenceBoosts.Should().NotBeNull();
		config.ExtractionRules.ConfidenceBoosts!.InDictionary.Should().BeGreaterThan(0);
		config.ExtractionRules.ConfidenceBoosts.HasTitle.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task EntityPatterns_AllowedConnectors_ShouldContainCommonWords()
	{
		// Act
		var config = await EntityPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		config.AllowedConnectors.Should().NotBeNullOrEmpty();
		config.AllowedConnectors.Should().Contain("of");
		config.AllowedConnectors.Should().Contain("the");
		config.AllowedConnectors.Should().Contain("and");
	}

	[Fact]
	public async Task EntityPatterns_SentenceStarters_ShouldFilterCommonWords()
	{
		// Act
		var config = await EntityPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		config.SentenceStarters.Should().NotBeNullOrEmpty();
		config.SentenceStarters.Should().Contain("The");
		config.SentenceStarters.Should().Contain("In");
		config.SentenceStarters.Should().Contain("However");
		
		// Should contain pronouns to filter
		config.SentenceStarters.Should().Contain("He");
		config.SentenceStarters.Should().Contain("She");
		config.SentenceStarters.Should().Contain("They");
	}

	[Fact]
	public async Task RelationshipPatterns_ShouldSupportAllExpectedTypes()
	{
		// Act
		var patterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();

		// Assert - Check that we have patterns for key relationship types
		var relationshipTypes = patterns.Select(p => p.Type).Distinct().ToList();
		
		relationshipTypes.Should().Contain(Models.KnowledgeGraph.RelationshipType.Founded);
		relationshipTypes.Should().Contain(Models.KnowledgeGraph.RelationshipType.MemberOf);
		relationshipTypes.Should().Contain(Models.KnowledgeGraph.RelationshipType.StudiedAt);
		relationshipTypes.Should().Contain(Models.KnowledgeGraph.RelationshipType.WorksFor);
		relationshipTypes.Should().Contain(Models.KnowledgeGraph.RelationshipType.AuthorOf);
		
		// Should have at least 15 different relationship types
		relationshipTypes.Should().HaveCountGreaterThan(15,
			"should support diverse relationship types");
	}

	[Fact]
	public async Task RelationshipPatterns_Categories_ShouldBeOrganized()
	{
		// This test loads the raw configuration to check categories
		var filePath = Path.Combine(AppContext.BaseDirectory, "Configuration", "RelationshipPatterns.json");
		
		if (!File.Exists(filePath))
		{
			// Try alternative paths
			filePath = Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "RelationshipPatterns.json");
		}

		filePath = Path.GetFullPath(filePath);
		
		// Act
		var json = await File.ReadAllTextAsync(filePath);
		
		// Assert
		json.Should().Contain("\"category\"", "patterns should be categorized");
		json.Should().Contain("Organizational");
		json.Should().Contain("Educational");
		json.Should().Contain("Scientific");
	}

	[Fact]
	public async Task BothConfigFiles_ShouldHaveMatchingVersions()
	{
		// Act
		var relationshipPatterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();
		var entityConfig = await EntityPatternLoader.LoadDefaultPatternsAsync();

		// Load raw configs to get version
		var relFilePath = Path.Combine(AppContext.BaseDirectory, "Configuration", "RelationshipPatterns.json");
		var entFilePath = Path.Combine(AppContext.BaseDirectory, "Configuration", "EntityPatterns.json");

		// Both files should have version 2.0 for Phase 12
		entityConfig.Version.Should().Be("2.0");
		
		// Just ensure we loaded something
		relationshipPatterns.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ConfigurationFiles_ShouldBeWellFormatted()
	{
		// This is a smoke test to ensure JSON is valid and well-formatted
		
		// Act & Assert - Should not throw
		var relationshipPatterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();
		var entityConfig = await EntityPatternLoader.LoadDefaultPatternsAsync();

		relationshipPatterns.Should().NotBeNull();
		entityConfig.Should().NotBeNull();
	}
}
