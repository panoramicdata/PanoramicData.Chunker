using AwesomeAssertions;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Infrastructure.TokenCounters;

namespace PanoramicData.Chunker.Tests.Unit.Infrastructure;

public class TokenCounterFactoryTests
{
	[Fact]
	public void Create_CharacterBased_ShouldReturnCharacterBasedTokenCounter()
	{
		// Arrange & Act
		var counter = TokenCounterFactory.Create(TokenCountingMethod.CharacterBased);

		// Assert
		_ = counter.Should().BeOfType<CharacterBasedTokenCounter>();
	}

	[Fact]
	public void Create_OpenAI_CL100K_ShouldReturnOpenAITokenCounter()
	{
		// Arrange & Act
		var counter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_CL100K);

		// Assert
		_ = counter.Should().BeOfType<OpenAITokenCounter>();
		var openAICounter = (OpenAITokenCounter)counter;
		_ = openAICounter.EncodingName.Should().Be("cl100k_base");
	}

	[Fact]
	public void Create_OpenAI_P50K_ShouldReturnOpenAITokenCounter()
	{
		// Arrange & Act
		var counter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_P50K);

		// Assert
		_ = counter.Should().BeOfType<OpenAITokenCounter>();
		var openAICounter = (OpenAITokenCounter)counter;
		_ = openAICounter.EncodingName.Should().Be("p50k_base");
	}

	[Fact]
	public void Create_OpenAI_R50K_ShouldReturnOpenAITokenCounter()
	{
		// Arrange & Act
		var counter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_R50K);

		// Assert
		_ = counter.Should().BeOfType<OpenAITokenCounter>();
		var openAICounter = (OpenAITokenCounter)counter;
		_ = openAICounter.EncodingName.Should().Be("r50k_base");
	}

	[Fact]
	public void Create_Custom_WithCounter_ShouldReturnProvidedCounter()
	{
		// Arrange
		var customCounter = new CharacterBasedTokenCounter();

		// Act
		var counter = TokenCounterFactory.Create(TokenCountingMethod.Custom, customCounter);

		// Assert
		_ = counter.Should().BeSameAs(customCounter);
	}

	[Fact]
	public void Create_Custom_WithoutCounter_ShouldThrow()
	{
		// Arrange & Act
		var act = () => TokenCounterFactory.Create(TokenCountingMethod.Custom);

		// Assert
		_ = act.Should().Throw<ArgumentException>()
			.WithMessage("*Custom token counter must be provided*");
	}

	[Fact]
	public void Create_Claude_ShouldThrowNotSupported()
	{
		// Arrange & Act
		var act = () => TokenCounterFactory.Create(TokenCountingMethod.Claude);

		// Assert
		_ = act.Should().Throw<NotSupportedException>()
			.WithMessage("*Claude tokenization is not yet implemented*");
	}

	[Fact]
	public void Create_Cohere_ShouldThrowNotSupported()
	{
		// Arrange & Act
		var act = () => TokenCounterFactory.Create(TokenCountingMethod.Cohere);

		// Assert
		_ = act.Should().Throw<NotSupportedException>()
			.WithMessage("*Cohere tokenization is not yet implemented*");
	}

	[Fact]
	public void GetOrCreate_WithProvidedCounter_ShouldReturnIt()
	{
		// Arrange
		var customCounter = new CharacterBasedTokenCounter();
		var options = new ChunkingOptions
		{
			TokenCounter = customCounter
		};

		// Act
		var counter = TokenCounterFactory.GetOrCreate(options);

		// Assert
		_ = counter.Should().BeSameAs(customCounter);
	}

	[Fact]
	public void GetOrCreate_WithoutProvidedCounter_ShouldCreateBasedOnMethod()
	{
		// Arrange
		var options = new ChunkingOptions
		{
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
		};

		// Act
		var counter = TokenCounterFactory.GetOrCreate(options);

		// Assert
		_ = counter.Should().BeOfType<OpenAITokenCounter>();
	}

	[Fact]
	public void GetOrCreate_DefaultOptions_ShouldReturnCharacterBased()
	{
		// Arrange
		var options = new ChunkingOptions();

		// Act
		var counter = TokenCounterFactory.GetOrCreate(options);

		// Assert
		_ = counter.Should().BeOfType<CharacterBasedTokenCounter>();
	}

	[Fact]
	public void GetOrCreate_NullOptions_ShouldThrow()
	{
		// Arrange & Act
		var act = () => TokenCounterFactory.GetOrCreate(null!);

		// Assert
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(TokenCountingMethod.CharacterBased)]
	[InlineData(TokenCountingMethod.OpenAI_CL100K)]
	[InlineData(TokenCountingMethod.OpenAI_P50K)]
	[InlineData(TokenCountingMethod.OpenAI_R50K)]
	public void Create_SupportedMethods_ShouldReturnWorkingCounter(TokenCountingMethod method)
	{
		// Arrange & Act
		var counter = TokenCounterFactory.Create(method);
		var testText = "Hello, world!";

		// Assert
		_ = counter.Should().NotBeNull();
		var tokenCount = counter.CountTokens(testText);
		_ = tokenCount.Should().BePositive();
	}

	[Fact]
	public void GetOrCreate_WithCustomCounterInOptions_ShouldIgnoreMethod()
	{
		// Arrange
		var customCounter = new CharacterBasedTokenCounter();
		var options = new ChunkingOptions
		{
			TokenCounter = customCounter,
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K // Should be ignored
		};

		// Act
		var counter = TokenCounterFactory.GetOrCreate(options);

		// Assert
		_ = counter.Should().BeSameAs(customCounter);
		_ = counter.Should().NotBeOfType<OpenAITokenCounter>();
	}
}
