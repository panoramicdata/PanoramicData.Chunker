using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models.Llm;
using System.Diagnostics;
using System.Net.Http.Json;

namespace PanoramicData.Chunker.LLM.Providers;

/// <summary>
/// Ollama LLM provider implementation.
/// </summary>
public class OllamaLlmProvider : ILlmProvider
{
	private readonly HttpClient _httpClient;
	private readonly OllamaOptions _options;
	private readonly ILogger<OllamaLlmProvider> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="OllamaLlmProvider"/> class.
	/// </summary>
	/// <param name="options">Ollama configuration options.</param>
	/// <param name="logger">Logger instance.</param>
	/// <param name="httpClient">Optional HTTP client (for testing).</param>
	public OllamaLlmProvider(
		OllamaOptions options,
		ILogger<OllamaLlmProvider> logger,
		HttpClient? httpClient = null)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds) };
	}

	/// <inheritdoc/>
	public string ProviderName => "Ollama";

	/// <inheritdoc/>
	public async Task<LlmResponse> GenerateAsync(
		LlmRequest request,
		CancellationToken cancellationToken = default)
	{
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var requestBody = new
			{
				model = request.Model,
				prompt = request.Prompt,
				system = request.SystemPrompt,
				stream = false,
				options = new
				{
					temperature = request.Temperature,
					num_predict = request.MaxTokens
				}
			};

			var response = await _httpClient.PostAsJsonAsync(
				$"{_options.BaseUrl}/api/generate",
				requestBody,
				cancellationToken);

			_ = response.EnsureSuccessStatusCode();

			var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);

			stopwatch.Stop();

			_logger.LogDebug(
				"Ollama generation completed in {Duration}ms for model {Model}",
				stopwatch.ElapsedMilliseconds,
				request.Model);

			return new LlmResponse
			{
				Text = result?.Response ?? string.Empty,
				Model = request.Model,
				TokensUsed = (result?.PromptEvalCount ?? 0) + (result?.EvalCount ?? 0),
				Duration = stopwatch.Elapsed,
				Success = true
			};
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			_logger.LogError(ex, "Ollama generation failed for model {Model}", request.Model);

			return new LlmResponse
			{
				Text = string.Empty,
				Model = request.Model,
				TokensUsed = 0,
				Duration = stopwatch.Elapsed,
				Success = false,
				ErrorMessage = ex.Message
			};
		}
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<LlmResponse>> GenerateBatchAsync(
		IEnumerable<LlmRequest> requests,
		CancellationToken cancellationToken = default)
	{
		// Process sequentially for now (Ollama doesn't have native batch API)
		var responses = new List<LlmResponse>();

		foreach (var request in requests)
		{
			var response = await GenerateAsync(request, cancellationToken);
			responses.Add(response);
		}

		return responses;
	}

	/// <inheritdoc/>
	public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var response = await _httpClient.GetAsync(
				$"{_options.BaseUrl}/api/tags",
				cancellationToken);

			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Ollama availability check failed");
			return false;
		}
	}

	private record OllamaGenerateResponse
	{
		[System.Text.Json.Serialization.JsonPropertyName("response")]
		public string? Response { get; init; }

		[System.Text.Json.Serialization.JsonPropertyName("prompt_eval_count")]
		public int? PromptEvalCount { get; init; }

		[System.Text.Json.Serialization.JsonPropertyName("eval_count")]
		public int? EvalCount { get; init; }

		[System.Text.Json.Serialization.JsonPropertyName("done")]
		public bool Done { get; init; }
	}
}
