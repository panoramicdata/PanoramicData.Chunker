using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for chunk serialization.
/// </summary>
public interface IChunkSerializer
{
	/// <summary>
	/// Serialize chunks to a stream.
	/// </summary>
	/// <param name="chunks">The chunks to serialize.</param>
	/// <param name="output">The output stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SerializeAsync(
		IEnumerable<ChunkerBase> chunks,
		Stream output,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Deserialize chunks from a stream.
	/// </summary>
	/// <param name="input">The input stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The deserialized chunks.</returns>
	Task<IEnumerable<ChunkerBase>> DeserializeAsync(
		Stream input,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Serialize a chunking result to a stream.
	/// </summary>
	/// <param name="result">The chunking result to serialize.</param>
	/// <param name="output">The output stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SerializeResultAsync(
		ChunkingResult result,
		Stream output,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Deserialize a chunking result from a stream.
	/// </summary>
	/// <param name="input">The input stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The deserialized chunking result.</returns>
	Task<ChunkingResult?> DeserializeResultAsync(
		Stream input,
		CancellationToken cancellationToken = default);
}
