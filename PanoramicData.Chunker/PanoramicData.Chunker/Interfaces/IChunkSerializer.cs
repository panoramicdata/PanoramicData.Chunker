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
	/// <typeparam name="T">The chunk collection type.</typeparam>
	/// <param name="input">The input stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The deserialized chunks.</returns>
	Task<T> DeserializeAsync<T>(
		Stream input,
		CancellationToken cancellationToken = default)
		where T : IEnumerable<ChunkerBase>;
}
