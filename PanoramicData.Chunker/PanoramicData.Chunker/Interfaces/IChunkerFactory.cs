using PanoramicData.Chunker.Configuration;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Factory for creating appropriate chunkers.
/// </summary>
public interface IChunkerFactory
{
	/// <summary>
	/// Get a chunker for the specified document type.
	/// </summary>
	/// <param name="type">The document type.</param>
	/// <returns>A document chunker instance.</returns>
	IDocumentChunker GetChunker(DocumentType type);

	/// <summary>
	/// Get a chunker by auto-detecting the document type from the stream asynchronously.
	/// </summary>
	/// <param name="stream">The document stream.</param>
	/// <param name="fileNameHint">Optional filename hint for extension-based detection.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A chunker capable of handling the detected document type.</returns>
	Task<IDocumentChunker> GetChunkerForStreamAsync(Stream stream, string? fileNameHint, CancellationToken cancellationToken);

	/// <summary>
	/// Register a custom chunker for a document type.
	/// </summary>
	/// <param name="chunker">The chunker to register.</param>
	void RegisterChunker(IDocumentChunker chunker);

	/// <summary>
	/// Get all registered/supported document types.
	/// </summary>
	/// <returns>Collection of supported document types.</returns>
	IReadOnlyCollection<DocumentType> GetSupportedTypes();
}
