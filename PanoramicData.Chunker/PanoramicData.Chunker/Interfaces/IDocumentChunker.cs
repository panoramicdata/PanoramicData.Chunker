using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Core interface for document chunkers.
/// </summary>
public interface IDocumentChunker
{
	/// <summary>
	/// Supported document type.
	/// </summary>
	DocumentType SupportedType { get; }

	/// <summary>
	/// Chunk a document asynchronously.
	/// </summary>
	/// <param name="documentStream">The document stream to chunk.</param>
	/// <param name="options">Chunking options.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Chunking result with chunks and statistics.</returns>
	Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken);

	/// <summary>
	/// Validate if the stream contains a valid document of this type.
	/// </summary>
	/// <param name="documentStream">The stream to validate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the stream can be handled by this chunker.</returns>
	Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken);
}
