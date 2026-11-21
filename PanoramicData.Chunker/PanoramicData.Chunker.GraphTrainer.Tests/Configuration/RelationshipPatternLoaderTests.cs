using AwesomeAssertions;
using PanoramicData.Chunker.Configuration;

namespace PanoramicData.Chunker.GraphTrainer.Tests.Configuration;

/// <summary>
/// Tests for RelationshipPatternLoader to ensure proper JSON handling.
/// </summary>
public class RelationshipPatternLoaderTests
{
	private readonly string _testDataDirectory;

	public RelationshipPatternLoaderTests()
	{
		_testDataDirectory = Path.Combine(Path.GetTempPath(), "GraphTrainerTests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_testDataDirectory);
	}

	[Fact]
	public async Task LoadDefaultPatternsAsync_ShouldLoadAllPatterns()
	{
		// Act
		var patterns = await RelationshipPatternLoader.LoadDefaultPatternsAsync();

		// Assert
		patterns.Should().NotBeNull();
		patterns.Should().HaveCountGreaterThan(30, "should have 35+ patterns");
		patterns.Should().AllSatisfy(p =>
		{
			p.Name.Should().NotBeNullOrEmpty();
			p.Regex.Should().NotBeNull();
			p.Type.Should().NotBe(default);
			p.Confidence.Should().BeInRange(0.0, 1.0);
		});
	}

	[Fact]
	public async Task LoadPatternsAsync_WithValidFile_ShouldSucceed()
	{
		// Arrange
		var config = new RelationshipPatternsConfiguration
		{
			Version = "2.0",
			Description = "Test patterns",
			LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd"),
			Patterns =
			[
				new RelationshipPatternDefinition
				{
					Name = "TestPattern",
					Regex = @"\b(test)\b",
					RelationshipType = "Founded",
					Confidence = 0.9,
					IsDirectional = true,
					Description = "Test pattern",
					Examples = ["Test example"],
					Category = "Test",
					Enabled = true
				}
			]
		};

		var filePath = Path.Combine(_testDataDirectory, "test-patterns.json");
		await RelationshipPatternLoader.SavePatternsAsync(config, filePath);

		// Act
		var patterns = await RelationshipPatternLoader.LoadPatternsAsync(filePath);

		// Assert
		patterns.Should().ContainSingle();
		patterns[0].Name.Should().Be("TestPattern");
		patterns[0].Confidence.Should().Be(0.9);
	}

	[Fact]
	public async Task ValidatePatternsAsync_WithValidFile_ShouldReturnValid()
	{
		// Act
		var result = await ValidateSinglePatternAsync("valid-patterns.json", "ValidPattern", @"\b(valid)\b", "Founded", 0.8);

		// Assert
		result.IsValid.Should().BeTrue();
		result.Errors.Should().BeEmpty();
		result.PatternCount.Should().Be(1);
		result.EnabledPatternCount.Should().Be(1);
	}

	[Fact]
	public async Task ValidatePatternsAsync_WithInvalidRegex_ShouldReturnErrors()
	{
		// Act
		var result = await ValidateSinglePatternAsync("invalid-patterns.json", "InvalidPattern", @"[invalid(regex", "Founded", 0.8);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().NotBeEmpty();
		result.Errors.Should().ContainMatch("*Invalid regex*");
	}

	[Fact]
	public async Task ValidatePatternsAsync_WithInvalidConfidence_ShouldReturnErrors()
	{
		// Act
		var result = await ValidateSinglePatternAsync("bad-confidence.json", "BadConfidence", @"\b(test)\b", "Founded", 1.5);

		AssertInvalidWithError(result, "*Confidence must be between 0.0 and 1.0*");
	}

	[Fact]
	public async Task ValidatePatternsAsync_WithInvalidRelationshipType_ShouldReturnErrors()
	{
		// Act
		var result = await ValidateSinglePatternAsync("bad-type.json", "BadType", @"\b(test)\b", "NonExistentType", 0.8);

		AssertInvalidWithError(result, "*Invalid relationship type*");
	}

	private static void AssertInvalidWithError(PatternValidationResult result, string errorPattern)
	{
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainMatch(errorPattern);
	}

	private async Task<PatternValidationResult> ValidateSinglePatternAsync(string fileName, string name, string regex, string relationshipType, double confidence)
	{
		var config = new RelationshipPatternsConfiguration
		{
			Version = "2.0",
			Patterns =
			[
				new RelationshipPatternDefinition
				{
					Name = name,
					Regex = regex,
					RelationshipType = relationshipType,
					Confidence = confidence,
					IsDirectional = true
				}
			]
		};

		var filePath = Path.Combine(_testDataDirectory, fileName);
		await RelationshipPatternLoader.SavePatternsAsync(config, filePath);

		return await RelationshipPatternLoader.ValidatePatternsAsync(filePath);
	}
	[Fact]
	public async Task CreateTemplateAsync_ShouldCreateValidTemplate()
	{
		// Arrange
		var filePath = Path.Combine(_testDataDirectory, "template.json");

		// Act
		await RelationshipPatternLoader.CreateTemplateAsync(filePath);

		// Assert
		File.Exists(filePath).Should().BeTrue();
		
		// The template file should be valid JSON even though it contains a disabled pattern
		var json = await File.ReadAllTextAsync(filePath);
		json.Should().Contain("\"Version\"");
		json.Should().Contain("\"Patterns\"");
		json.Should().Contain("CustomPattern");
		json.Should().Contain("\"Enabled\": false");
	}

	[Fact]
	public async Task SavePatternsAsync_ShouldCreateValidJSON()
	{
		// Arrange
		var config = new RelationshipPatternsConfiguration
		{
			Version = "2.0",
			Description = "Test configuration",
			LastUpdated = "2025-01-15",
			Patterns =
			[
				new RelationshipPatternDefinition
				{
					Name = "TestPattern",
					Regex = @"\b(founded|established)\b",
					RelationshipType = "Founded",
					Confidence = 0.95,
					IsDirectional = true,
					Description = "Test pattern for founded relationships",
					Examples = ["Company founded by Person", "Organization established by Founder"],
					Category = "Organizational",
					Enabled = true,
					RegexOptions = "IgnoreCase"
				}
			],
			Categories = new Dictionary<string, string>
			{
				["Organizational"] = "Patterns for organizational relationships"
			},
			TrainingNotes = new TrainingNotes
			{
				Instructions = "Add patterns here",
				PatternDesign = "Make patterns specific",
				ConfidenceScoring = "Use 0.9+ for high confidence",
				TestingStrategy = "Test against ground truth"
			}
		};

		var filePath = Path.Combine(_testDataDirectory, "full-config.json");

		// Act
		await RelationshipPatternLoader.SavePatternsAsync(config, filePath);

		// Assert
		File.Exists(filePath).Should().BeTrue();

		// Verify it can be loaded back
		var loaded = await RelationshipPatternLoader.LoadPatternsAsync(filePath);
		loaded.Should().ContainSingle();
		loaded[0].Name.Should().Be("TestPattern");
		loaded[0].Description.Should().Be("Test pattern for founded relationships");
	}

	[Fact]
	public async Task CompileRegex_WithDifferentOptions_ShouldWork()
	{
		// Arrange
		var pattern = new RelationshipPatternDefinition
		{
			Name = "Test",
			Regex = @"\b(test)\b",
			RelationshipType = "Founded",
			Confidence = 0.8,
			IsDirectional = true,
			RegexOptions = "IgnoreCase,Multiline"
		};

		// Act
		var regex = pattern.CompileRegex();

		// Assert
		regex.Should().NotBeNull();
		regex.Options.Should().HaveFlag(System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		regex.Options.Should().HaveFlag(System.Text.RegularExpressions.RegexOptions.Multiline);
	}
}
