using AwesomeAssertions;
using PanoramicData.Chunker.LLM;

namespace PanoramicData.Chunker.Tests.Unit.LLM;

public class PromptTemplateManagerTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;

	[Fact]
	public void GetTemplate_WithValidName_ShouldReturnTemplate()
	{
		// Arrange
		var manager = new PromptTemplateManager();

		// Act
		var template = manager.GetTemplate("summarize");

		// Assert
		_ = template.Should().NotBeNullOrEmpty();
		_ = template.Should().Contain("{content}");
		_ = template.Should().Contain("{max_words}");

		_output.WriteLine($"Summarize template:\n{template}");
	}

	[Fact]
	public void GetTemplate_WithInvalidName_ShouldThrow()
	{
		// Arrange
		var manager = new PromptTemplateManager();

		// Act & Assert
		var act = () => manager.GetTemplate("invalid_template");
		_ = act.Should().Throw<ArgumentException>()
			.WithMessage("*Unknown template*");
	}

	[Fact]
	public void RenderTemplate_WithVariables_ShouldSubstitute()
	{
		// Arrange
		var manager = new PromptTemplateManager();
		var variables = new Dictionary<string, string>
		{
			["content"] = "Test content here",
			["max_words"] = "50"
		};

		// Act
		var rendered = manager.RenderTemplate("summarize", variables);

		// Assert
		_ = rendered.Should().Contain("Test content here");
		_ = rendered.Should().Contain("50");
		_ = rendered.Should().NotContain("{content}");
		_ = rendered.Should().NotContain("{max_words}");

		_output.WriteLine($"Rendered template:\n{rendered}");
	}

	[Fact]
	public void GetTemplate_ForKeywordExtraction_ShouldContainExpectedPlaceholders()
	{
		// Arrange
		var manager = new PromptTemplateManager();

		// Act
		var template = manager.GetTemplate("extract_keywords");

		// Assert
		_ = template.Should().Contain("{content}");
		_ = template.Should().Contain("{max_keywords}");

		_output.WriteLine($"Keyword extraction template:\n{template}");
	}

	[Fact]
	public void GetTemplate_ForEntityExtraction_ShouldContainExpectedPlaceholders()
	{
		// Arrange
		var manager = new PromptTemplateManager();

		// Act
		var template = manager.GetTemplate("extract_entities");

		// Assert
		_ = template.Should().Contain("{content}");
		_ = template.Should().Contain("JSON");

		_output.WriteLine($"Entity extraction template:\n{template}");
	}

	[Fact]
	public void SetTemplate_ShouldAddOrUpdateTemplate()
	{
		// Arrange
		var manager = new PromptTemplateManager();
		var customTemplate = "Custom template with {variable}";

		// Act
		manager.SetTemplate("custom", customTemplate);
		var retrieved = manager.GetTemplate("custom");

		// Assert
		_ = retrieved.Should().Be(customTemplate);

		_output.WriteLine($"Custom template: {retrieved}");
	}

	[Fact]
	public void GetTemplateNames_ShouldReturnAllTemplates()
	{
		// Arrange
		var manager = new PromptTemplateManager();

		// Act
		var names = manager.GetTemplateNames().ToList();

		// Assert
		_ = names.Should().Contain("summarize");
		_ = names.Should().Contain("extract_keywords");
		_ = names.Should().Contain("extract_entities");
		names.Should().HaveCountGreaterThanOrEqualTo(3);

		_output.WriteLine($"Available templates: {string.Join(", ", names)}");
	}

	[Fact]
	public void RenderTemplate_WithMultipleVariables_ShouldSubstituteAll()
	{
		// Arrange
		var manager = new PromptTemplateManager();
		var variables = new Dictionary<string, string>
		{
			["content"] = "Sample text about artificial intelligence and machine learning.",
			["max_keywords"] = "5"
		};

		// Act
		var rendered = manager.RenderTemplate("extract_keywords", variables);

		// Assert
		_ = rendered.Should().Contain("Sample text about artificial intelligence and machine learning.");
		_ = rendered.Should().Contain("5");

		_output.WriteLine($"Rendered keyword extraction:\n{rendered}");
	}

	[Fact]
	public void SetTemplate_OverwritingExisting_ShouldUpdateTemplate()
	{
		// Arrange
		var manager = new PromptTemplateManager();
		var originalTemplate = manager.GetTemplate("summarize");
		var newTemplate = "New summarize template {content}";

		// Act
		manager.SetTemplate("summarize", newTemplate);
		var retrieved = manager.GetTemplate("summarize");

		// Assert
		_ = retrieved.Should().Be(newTemplate);
		_ = retrieved.Should().NotBe(originalTemplate);

		_output.WriteLine($"Updated template: {retrieved}");
	}
}
