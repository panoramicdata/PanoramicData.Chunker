using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for chunk validation.
/// </summary>
public interface IChunkValidator
{
	/// <summary>
	/// Validate a collection of chunks.
	/// </summary>
	/// <param name="chunks">The chunks to validate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Validation result.</returns>
	Task<ValidationResult> ValidateAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken);
}
