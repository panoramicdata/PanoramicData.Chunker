namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Token counting methods for different embedding models.
/// </summary>
public enum TokenCountingMethod
{
	/// <summary>
	/// Simple character-based counting (chars / 4).
	/// </summary>
	CharacterBased,

	/// <summary>
	/// OpenAI CL100K tokenizer (GPT-4, GPT-3.5-turbo).
	/// </summary>
	OpenAI_CL100K,

	/// <summary>
	/// OpenAI P50K tokenizer (GPT-3).
	/// </summary>
	OpenAI_P50K,

	/// <summary>
	/// OpenAI R50K tokenizer (GPT-2).
	/// </summary>
	OpenAI_R50K,

	/// <summary>
	/// Anthropic Claude tokenizer.
	/// </summary>
	Claude,

	/// <summary>
	/// Cohere tokenizer.
	/// </summary>
	Cohere,

	/// <summary>
	/// Custom tokenizer provided via ITokenCounter.
	/// </summary>
	Custom
}
