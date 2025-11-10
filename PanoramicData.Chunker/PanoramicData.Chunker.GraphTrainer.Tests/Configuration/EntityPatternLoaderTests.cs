using FluentAssertions;
using PanoramicData.Chunker.Configuration;

namespace PanoramicData.Chunker.GraphTrainer.Tests.Configuration;

/// <summary>
/// Tests for EntityPatternLoader to ensure proper JSON handling.
/// </summary>
public class EntityPatternLoaderTests
{
	private readonly string _testDataDirectory;

	public EntityPatternLoaderTests()
	{
		_testDataDirectory = Path.Combine(Path.GetTempPath(), "GraphTrainerTests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_testDataDirectory);
	}

	[Fact]
	public async Task LoadDefaultPatternsAsync_ShouldLoadConfiguration()
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
	public async Task LoadPatternsAsync_WithValidFile_ShouldSucceed()
	{
		// Arrange
		var config = new EntityPatternsConfiguration
		{
			Version = "2.0",
			Description = "Test entity patterns",
			LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd"),
			ProperNounDictionary = new ProperNounDictionary
			{
				People = ["TestPerson", "AnotherPerson"],
				Places = ["TestPlace"],
				Organizations = ["TestOrg"]
			},
			TitlePrefixes = new Dictionary<string, List<string>>
			{
				["academic"] = ["Professor", "Dr"]
			},
			OrganizationalSuffixes = ["University", "Institute"],
			AllowedConnectors = ["of", "the"],
			SentenceStarters = ["The", "In"],
			ExtractionRules = new ExtractionRules
			{
				MinWordLength = 2,
				MinOccurrences = 1,
				BaseConfidence = 0.7,
				ConfidenceBoosts = new ConfidenceBoosts
				{
					InDictionary = 0.15,
					HasTitle = 0.10,
					MultiWord = 0.10,
					OrganizationalSuffix = 0.10,
					PerFrequency = 0.05,
					MaxFrequencyBoost = 0.20
				}
			}
		};

		var filePath = Path.Combine(_testDataDirectory, "test-entities.json");
		await EntityPatternLoader.SavePatternsAsync(config, filePath);

		// Act
		var loaded = await EntityPatternLoader.LoadPatternsAsync(filePath);

		// Assert
		loaded.Should().NotBeNull();
		loaded.Version.Should().Be("2.0");
		loaded.ProperNounDictionary.Should().NotBeNull();
		loaded.ProperNounDictionary!.People.Should().Contain("TestPerson");
		loaded.ProperNounDictionary.Places.Should().Contain("TestPlace");
		loaded.ProperNounDictionary.Organizations.Should().Contain("TestOrg");
	}

	[Fact]
	public async Task SavePatternsAsync_ShouldCreateValidJSON()
	{
		// Arrange
		var config = new EntityPatternsConfiguration
		{
			Version = "2.0",
			Description = "Complete entity configuration",
			LastUpdated = "2025-01-15",
			ProperNounDictionary = new ProperNounDictionary
			{
				People = ["Darwin", "Jameson", "Henslow"],
				Places = ["Edinburgh", "Cambridge", "London"],
				Organizations = ["Plinian", "Royal", "Society"]
			},
			TitlePrefixes = new Dictionary<string, List<string>>
			{
				["academic"] = ["Professor", "Dr", "PhD"],
				["military"] = ["Captain", "General"],
				["ships"] = ["HMS", "USS"]
			},
			OrganizationalSuffixes =
			[
				"University", "College", "Institute", "Society",
				"Association", "Foundation", "Company"
			],
			AllowedConnectors = ["of", "the", "and", "in", "at"],
			SentenceStarters =
			[
				"The", "In", "On", "At", "For", "With",
				"However", "Therefore", "Thus"
			],
			ExtractionRules = new ExtractionRules
			{
				MinWordLength = 2,
				MinOccurrences = 1,
				BaseConfidence = 0.7,
				ConfidenceBoosts = new ConfidenceBoosts
				{
					InDictionary = 0.15,
					HasTitle = 0.10,
					MultiWord = 0.10,
					OrganizationalSuffix = 0.10,
					PerFrequency = 0.05,
					MaxFrequencyBoost = 0.20
				}
			},
			TrainingNotes = new EntityTrainingNotes
			{
				Instructions = "Add common proper nouns",
				DictionaryMaintenance = "Review regularly",
				CategoryClassification = "Classify by type",
				PerformanceMonitoring = "Track metrics"
			}
		};

		var filePath = Path.Combine(_testDataDirectory, "full-entity-config.json");

		// Act
		await EntityPatternLoader.SavePatternsAsync(config, filePath);

		// Assert
		File.Exists(filePath).Should().BeTrue();

		// Verify it can be loaded back
		var loaded = await EntityPatternLoader.LoadPatternsAsync(filePath);
		loaded.Should().NotBeNull();
		loaded.ProperNounDictionary!.People.Should().Contain("Darwin");
		loaded.TitlePrefixes!["academic"].Should().Contain("Professor");
		loaded.ExtractionRules!.BaseConfidence.Should().Be(0.7);
	}

	[Fact]
	public async Task CreateTemplateAsync_ShouldCreateValidTemplate()
	{
		// Arrange
		var filePath = Path.Combine(_testDataDirectory, "entity-template.json");

		// Act
		await EntityPatternLoader.CreateTemplateAsync(filePath);

		// Assert
		File.Exists(filePath).Should().BeTrue();

		var config = await EntityPatternLoader.LoadPatternsAsync(filePath);
		config.Should().NotBeNull();
		config.Version.Should().Be("2.0");
		config.ProperNounDictionary.Should().NotBeNull();
		config.ExtractionRules.Should().NotBeNull();
	}

	[Fact]
	public void GetAllProperNouns_ShouldReturnAllEntries()
	{
		// Arrange
		var config = new EntityPatternsConfiguration
		{
			Version = "2.0",
			ProperNounDictionary = new ProperNounDictionary
			{
				People = ["Person1", "Person2"],
				Places = ["Place1"],
				Organizations = ["Org1", "Org2", "Org3"]
			}
		};

		// Act
		var allNouns = config.GetAllProperNouns();

		// Assert
		allNouns.Should().HaveCount(6);
		allNouns.Should().Contain("Person1");
		allNouns.Should().Contain("Place1");
		allNouns.Should().Contain("Org1");
	}

	[Fact]
	public void GetAllTitlePrefixes_ShouldReturnAllTitles()
	{
		// Arrange
		var config = new EntityPatternsConfiguration
		{
			Version = "2.0",
			TitlePrefixes = new Dictionary<string, List<string>>
			{
				["academic"] = ["Professor", "Dr"],
				["military"] = ["Captain", "General"],
				["ships"] = ["HMS"]
			}
		};

		// Act
		var allTitles = config.GetAllTitlePrefixes();

		// Assert
		allTitles.Should().HaveCount(5);
		allTitles.Should().Contain("Professor");
		allTitles.Should().Contain("Captain");
		allTitles.Should().Contain("HMS");
	}

	[Fact]
	public async Task LoadPatternsAsync_WithMissingOptionalFields_ShouldSucceed()
	{
		// Arrange - minimal valid configuration
		var config = new EntityPatternsConfiguration
		{
			Version = "1.0"
			// All other fields are optional
		};

		var filePath = Path.Combine(_testDataDirectory, "minimal-config.json");
		await EntityPatternLoader.SavePatternsAsync(config, filePath);

		// Act
		var loaded = await EntityPatternLoader.LoadPatternsAsync(filePath);

		// Assert
		loaded.Should().NotBeNull();
		loaded.Version.Should().Be("1.0");
	}

	[Fact]
	public async Task ExtractionRules_AllProperties_ShouldSerializeCorrectly()
	{
		// Arrange
		var config = new EntityPatternsConfiguration
		{
			Version = "2.0",
			ExtractionRules = new ExtractionRules
			{
				MinWordLength = 3,
				MinOccurrences = 2,
				BaseConfidence = 0.8,
				ConfidenceBoosts = new ConfidenceBoosts
				{
					InDictionary = 0.20,
					HasTitle = 0.15,
					MultiWord = 0.12,
					OrganizationalSuffix = 0.11,
					PerFrequency = 0.03,
					MaxFrequencyBoost = 0.25
				}
			}
		};

		var filePath = Path.Combine(_testDataDirectory, "extraction-rules.json");
		await EntityPatternLoader.SavePatternsAsync(config, filePath);

		// Act
		var loaded = await EntityPatternLoader.LoadPatternsAsync(filePath);

		// Assert
		loaded.ExtractionRules.Should().NotBeNull();
		loaded.ExtractionRules!.MinWordLength.Should().Be(3);
		loaded.ExtractionRules.MinOccurrences.Should().Be(2);
		loaded.ExtractionRules.BaseConfidence.Should().Be(0.8);
		loaded.ExtractionRules.ConfidenceBoosts.Should().NotBeNull();
		loaded.ExtractionRules.ConfidenceBoosts!.InDictionary.Should().Be(0.20);
		loaded.ExtractionRules.ConfidenceBoosts.HasTitle.Should().Be(0.15);
	}

	[Fact]
	public async Task ProperNounDictionary_CaseInsensitive_ShouldWork()
	{
		// Arrange
		var config = new EntityPatternsConfiguration
		{
			Version = "2.0",
			ProperNounDictionary = new ProperNounDictionary
			{
				People = ["Darwin", "darwin", "DARWIN"] // Duplicates with different cases
			}
		};

		var filePath = Path.Combine(_testDataDirectory, "case-test.json");
		await EntityPatternLoader.SavePatternsAsync(config, filePath);

		// Act
		var loaded = await EntityPatternLoader.LoadPatternsAsync(filePath);
		var allNouns = loaded.GetAllProperNouns();

		// Assert
		// HashSet with OrdinalIgnoreCase should deduplicate
		allNouns.Should().HaveCount(1);
		allNouns.Should().Contain("Darwin");
	}
}
