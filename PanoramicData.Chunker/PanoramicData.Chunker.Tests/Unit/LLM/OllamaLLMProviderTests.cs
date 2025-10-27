using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.LLM.Providers;
using PanoramicData.Chunker.Models.LLM;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

namespace PanoramicData.Chunker.Tests.Unit.LLM;

public class OllamaLLMProviderTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;

	[Fact]
	public void ProviderName_ShouldReturnOllama()
	{
		// Arrange
		var options = new OllamaOptions();
		var logger = new Mock<ILogger<OllamaLLMProvider>>().Object;
		var provider = new OllamaLLMProvider(options, logger);

		// Act & Assert
		_ = provider.ProviderName.Should().Be("Ollama");
	}

	[Fact]
	public async Task GenerateAsync_WithSuccessfulResponse_ShouldReturnLLMResponse()
	{
		// Arrange
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		_ = mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(JsonSerializer.Serialize(new
				{
					response = "This is a test response.",
					prompt_eval_count = 10,
					eval_count = 20,
					done = true
				}))
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var options = new OllamaOptions { BaseUrl = "http://localhost:11434" };
		var logger = new Mock<ILogger<OllamaLLMProvider>>().Object;
		var provider = new OllamaLLMProvider(options, logger, httpClient);

		var request = new LLMRequest
		{
			Prompt = "Test prompt",
			Model = "llama3",
			Temperature = 0.7,
			MaxTokens = 100
		};

		// Act
		var result = await provider.GenerateAsync(request);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		_ = result.Text.Should().Be("This is a test response.");
		_ = result.Model.Should().Be("llama3");
		_ = result.TokensUsed.Should().Be(30); // 10 + 20
		_ = result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
		_ = result.ErrorMessage.Should().BeNull();

		_output.WriteLine($"Generated response: {result.Text}");
		_output.WriteLine($"Tokens used: {result.TokensUsed}");
	}

	[Fact]
	public async Task GenerateAsync_WithHttpError_ShouldReturnFailedResponse()
	{
		// Arrange
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		_ = mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.ServiceUnavailable,
				Content = new StringContent("Service unavailable")
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var options = new OllamaOptions();
		var logger = new Mock<ILogger<OllamaLLMProvider>>().Object;
		var provider = new OllamaLLMProvider(options, logger, httpClient);

		var request = new LLMRequest
		{
			Prompt = "Test",
			Model = "llama3"
		};

		// Act
		var result = await provider.GenerateAsync(request);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeFalse();
		_ = result.Text.Should().BeEmpty();
		_ = result.TokensUsed.Should().Be(0);
		_ = result.ErrorMessage.Should().NotBeNullOrEmpty();

		_output.WriteLine($"Error message: {result.ErrorMessage}");
	}

	[Fact]
	public async Task GenerateBatchAsync_ShouldProcessAllRequests()
	{
		// Arrange
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		_ = mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(JsonSerializer.Serialize(new
				{
					response = "Response",
					prompt_eval_count = 5,
					eval_count = 10,
					done = true
				}))
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var options = new OllamaOptions();
		var logger = new Mock<ILogger<OllamaLLMProvider>>().Object;
		var provider = new OllamaLLMProvider(options, logger, httpClient);

		var requests = new[]
		{
			new LLMRequest { Prompt = "Prompt 1", Model = "llama3" },
			new LLMRequest { Prompt = "Prompt 2", Model = "llama3" },
			new LLMRequest { Prompt = "Prompt 3", Model = "llama3" }
		};

		// Act
		var results = await provider.GenerateBatchAsync(requests);

		// Assert
		_ = results.Should().HaveCount(3);
		_ = results.Should().OnlyContain(r => r.Success);

		_output.WriteLine($"Processed {results.Count()} requests");
	}

	[Fact]
	public async Task IsAvailableAsync_WithSuccessfulConnection_ShouldReturnTrue()
	{
		// Arrange
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		_ = mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent("{\"models\":[]}")
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var options = new OllamaOptions();
		var logger = new Mock<ILogger<OllamaLLMProvider>>().Object;
		var provider = new OllamaLLMProvider(options, logger, httpClient);

		// Act
		var result = await provider.IsAvailableAsync();

		// Assert
		_ = result.Should().BeTrue();

		_output.WriteLine("Ollama is available");
	}

	[Fact]
	public async Task IsAvailableAsync_WithFailedConnection_ShouldReturnFalse()
	{
		// Arrange
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		_ = mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ThrowsAsync(new HttpRequestException("Connection refused"));

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var options = new OllamaOptions();
		var logger = new Mock<ILogger<OllamaLLMProvider>>().Object;
		var provider = new OllamaLLMProvider(options, logger, httpClient);

		// Act
		var result = await provider.IsAvailableAsync();

		// Assert
		_ = result.Should().BeFalse();

		_output.WriteLine("Ollama is not available");
	}

	[Fact]
	public void Constructor_WithNullOptions_ShouldThrow()
	{
		// Arrange
		var logger = new Mock<ILogger<OllamaLLMProvider>>().Object;

		// Act & Assert
		var act = () => new OllamaLLMProvider(null!, logger);
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Constructor_WithNullLogger_ShouldThrow()
	{
		// Arrange
		var options = new OllamaOptions();

		// Act & Assert
		var act = () => new OllamaLLMProvider(options, null!);
		_ = act.Should().Throw<ArgumentNullException>();
	}
}
