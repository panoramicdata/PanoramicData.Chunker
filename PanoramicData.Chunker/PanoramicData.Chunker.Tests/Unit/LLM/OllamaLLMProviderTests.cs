using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.LLM.Providers;
using PanoramicData.Chunker.Models.Llm;
using System.Net;
using System.Text.Json;

namespace PanoramicData.Chunker.Tests.Unit.LLM;

public class OllamaLLMProviderTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void ProviderName_ShouldReturnOllama()
	{
		var provider = new OllamaLlmProvider(new OllamaOptions(), CreateLogger());

		_ = provider.ProviderName.Should().Be("Ollama");
	}

	[Fact]
	public async Task GenerateAsync_WithSuccessfulResponse_ShouldReturnLLMResponse()
	{
		var provider = CreateProvider(
			new OllamaOptions { BaseUrl = "http://localhost:11434" },
			() => Ok(JsonSerializer.Serialize(new
			{
				response = "This is a test response.",
				prompt_eval_count = 10,
				eval_count = 20,
				done = true
			})));

		var request = new LlmRequest
		{
			Prompt = "Test prompt",
			Model = "llama3",
			Temperature = 0.7,
			MaxTokens = 100
		};

		var result = await provider.GenerateAsync(request, CancellationToken);

		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		_ = result.Text.Should().Be("This is a test response.");
		_ = result.Model.Should().Be("llama3");
		_ = result.TokensUsed.Should().Be(30);
		_ = result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
		_ = result.ErrorMessage.Should().BeNull();

		_output.WriteLine($"Generated response: {result.Text}");
		_output.WriteLine($"Tokens used: {result.TokensUsed}");
	}

	[Fact]
	public async Task GenerateAsync_WithHttpError_ShouldReturnFailedResponse()
	{
		var provider = CreateProvider(
			new OllamaOptions(),
			() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.ServiceUnavailable,
				Content = new StringContent("Service unavailable")
			});

		var request = new LlmRequest
		{
			Prompt = "Test",
			Model = "llama3"
		};

		var result = await provider.GenerateAsync(request, CancellationToken);

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
		var provider = CreateProvider(
			new OllamaOptions(),
			() => Ok(JsonSerializer.Serialize(new
			{
				response = "Response",
				prompt_eval_count = 5,
				eval_count = 10,
				done = true
			})));

		var requests = new[]
		{
			new LlmRequest { Prompt = "Prompt 1", Model = "llama3" },
			new LlmRequest { Prompt = "Prompt 2", Model = "llama3" },
			new LlmRequest { Prompt = "Prompt 3", Model = "llama3" }
		};

		var results = await provider.GenerateBatchAsync(requests, CancellationToken);

		_ = results.Should().HaveCount(3);
		_ = results.Should().OnlyContain(r => r.Success);

		_output.WriteLine($"Processed {results.Count()} requests");
	}

	[Fact]
	public async Task IsAvailableAsync_WithSuccessfulConnection_ShouldReturnTrue()
	{
		var provider = CreateProvider(new OllamaOptions(), () => Ok("{\"models\":[]}"));

		var result = await provider.IsAvailableAsync(CancellationToken);

		_ = result.Should().BeTrue();

		_output.WriteLine("Ollama is available");
	}

	[Fact]
	public async Task IsAvailableAsync_WithFailedConnection_ShouldReturnFalse()
	{
		var provider = CreateProvider(
			new OllamaOptions(),
			() => throw new HttpRequestException("Connection refused"));

		var result = await provider.IsAvailableAsync(CancellationToken);

		_ = result.Should().BeFalse();

		_output.WriteLine("Ollama is not available");
	}

	[Fact]
	public void Constructor_WithNullOptions_ShouldThrow()
	{
		var logger = CreateLogger();

		var act = () => new OllamaLlmProvider(null!, logger);
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Constructor_WithNullLogger_ShouldThrow()
	{
		var options = new OllamaOptions();

		var act = () => new OllamaLlmProvider(options, null!);
		_ = act.Should().Throw<ArgumentNullException>();
	}

	private static ILogger<OllamaLlmProvider> CreateLogger() => new Mock<ILogger<OllamaLlmProvider>>().Object;

	private static HttpResponseMessage Ok(string content) => new()
	{
		StatusCode = HttpStatusCode.OK,
		Content = new StringContent(content)
	};

	private static OllamaLlmProvider CreateProvider(OllamaOptions options, Func<HttpResponseMessage> respond)
	{
		var handler = new Mock<HttpMessageHandler>();
		_ = handler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(respond);

		return new OllamaLlmProvider(options, CreateLogger(), new HttpClient(handler.Object));
	}
}
