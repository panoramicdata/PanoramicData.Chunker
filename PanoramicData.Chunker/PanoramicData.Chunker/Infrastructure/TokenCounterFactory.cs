using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure.TokenCounters;
using PanoramicData.Chunker.Interfaces;

namespace PanoramicData.Chunker.Infrastructure;

/// <summary>
/// Factory for creating token counters based on the specified method.
/// </summary>
public static class TokenCounterFactory
{
	/// <summary>
	/// Creates a token counter based on the specified method.
	/// </summary>
	/// <param name="method">The token counting method to use.</param>
	/// <param name="customCounter">Optional custom counter to use when method is Custom.</param>
	/// <returns>An instance of ITokenCounter.</returns>
	/// <exception cref="NotSupportedException">Thrown when the method is not supported.</exception>
	public static ITokenCounter Create(TokenCountingMethod method, ITokenCounter? customCounter = null) => method switch
	{
		TokenCountingMethod.CharacterBased => new CharacterBasedTokenCounter(),
		TokenCountingMethod.OpenAI_CL100K => OpenAITokenCounter.ForGpt4(),
		TokenCountingMethod.OpenAI_P50K => OpenAITokenCounter.ForGpt3(),
		TokenCountingMethod.OpenAI_R50K => OpenAITokenCounter.ForGpt2(),
		TokenCountingMethod.Custom when customCounter != null => customCounter,
		TokenCountingMethod.Custom => throw new ArgumentException("Custom token counter must be provided when using TokenCountingMethod.Custom", nameof(customCounter)),
		TokenCountingMethod.Claude => throw new NotSupportedException("Claude tokenization is not yet implemented. Use CharacterBased or OpenAI methods."),
		TokenCountingMethod.Cohere => throw new NotSupportedException("Cohere tokenization is not yet implemented. Use CharacterBased or OpenAI methods."),
		_ => throw new NotSupportedException($"Token counting method '{method}' is not supported.")
	};

	/// <summary>
	/// Gets or creates a token counter from chunking options.
	/// If options.TokenCounter is set, returns it.
	/// Otherwise, creates a new counter based on options.TokenCountingMethod.
	/// </summary>
	/// <param name="options">The chunking options.</param>
	/// <returns>An instance of ITokenCounter.</returns>
	public static ITokenCounter GetOrCreate(ChunkingOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		// If a custom counter is provided, use it
		if (options.TokenCounter != null)
		{
			return options.TokenCounter;
		}

		// Otherwise, create based on method
		return Create(options.TokenCountingMethod);
	}
}
