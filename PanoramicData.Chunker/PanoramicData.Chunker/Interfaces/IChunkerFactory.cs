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
	/// Get a chunker by auto-detecting the document type from the stream.
	/// </summary>
	/// <param name="stream">The document stream.</param>
	/// <param name="fileNameHint">Optional filename hint for type detection.</param>
	/// <returns>A document chunker instance.</returns>
	IDocumentChunker GetChunkerForStream(Stream stream, string? fileNameHint = null);

	/// <summary>
	/// Register a custom chunker.
	/// </summary>
	/// <param name="chunker">The chunker to register.</param>
	void RegisterChunker(IDocumentChunker chunker);
}
