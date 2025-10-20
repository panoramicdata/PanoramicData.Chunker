using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.Chunker.Serialization;

/// <summary>
/// JSON serializer for chunk collections using System.Text.Json.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonChunkSerializer"/> class.
/// </remarks>
/// <param name="options">Optional JSON serializer options. If not provided, uses default configuration.</param>
public class JsonChunkSerializer(JsonSerializerOptions? options = null) : IChunkSerializer
{
	private readonly JsonSerializerOptions _options = options ?? CreateDefaultOptions();

	/// <summary>
	/// Serializes chunks to JSON format.
	/// </summary>
	public async Task SerializeAsync(
		IEnumerable<ChunkerBase> chunks,
		Stream output,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(chunks);
		ArgumentNullException.ThrowIfNull(output);

		await JsonSerializer.SerializeAsync(output, chunks, _options, cancellationToken);
	}

	/// <summary>
	/// Deserializes chunks from JSON format.
	/// </summary>
	public async Task<IEnumerable<ChunkerBase>> DeserializeAsync(
		Stream input,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(input);

		var chunks = await JsonSerializer.DeserializeAsync<List<ChunkerBase>>(input, _options, cancellationToken);
		return chunks ?? [];
	}

	/// <summary>
	/// Serializes chunks to JSON string.
	/// </summary>
	public string SerializeToString(IEnumerable<ChunkerBase> chunks)
	{
		ArgumentNullException.ThrowIfNull(chunks);
		return JsonSerializer.Serialize(chunks, _options);
	}

	/// <summary>
	/// Deserializes chunks from JSON string.
	/// </summary>
	public IEnumerable<ChunkerBase> DeserializeFromString(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return [];
		}

		var chunks = JsonSerializer.Deserialize<List<ChunkerBase>>(json, _options);
		return chunks ?? [];
	}

	/// <summary>
	/// Serializes a chunking result to JSON format.
	/// </summary>
	public async Task SerializeResultAsync(
		ChunkingResult result,
		Stream output,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(result);
		ArgumentNullException.ThrowIfNull(output);

		await JsonSerializer.SerializeAsync(output, result, _options, cancellationToken);
	}

	/// <summary>
	/// Deserializes a chunking result from JSON format.
	/// </summary>
	public async Task<ChunkingResult?> DeserializeResultAsync(
		Stream input,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(input);

		return await JsonSerializer.DeserializeAsync<ChunkingResult>(input, _options, cancellationToken);
	}

	/// <summary>
	/// Creates default JSON serializer options with polymorphic type handling.
	/// </summary>
	private static JsonSerializerOptions CreateDefaultOptions()
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			Converters =
			{
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
			},
			// Configure polymorphic serialization for chunk types
			TypeInfoResolver = new PolymorphicTypeResolver()
		};

		return options;
	}
}
