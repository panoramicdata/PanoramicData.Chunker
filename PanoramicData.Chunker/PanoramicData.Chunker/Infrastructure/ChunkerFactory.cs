using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;

namespace PanoramicData.Chunker.Infrastructure;

/// <summary>
/// Factory for creating document chunkers.
/// </summary>
public class ChunkerFactory : IChunkerFactory
{
	private readonly Dictionary<DocumentType, IDocumentChunker> _chunkers = new();

	/// <summary>
	/// Get a chunker for the specified document type.
	/// </summary>
	public IDocumentChunker GetChunker(DocumentType type)
	{
		if (_chunkers.TryGetValue(type, out var chunker))
		{
			return chunker;
		}

		throw new NotSupportedException($"Document type '{type}' is not currently supported. No chunker registered for this type.");
	}

	/// <summary>
	/// Get a chunker by auto-detecting the document type from the stream.
	/// </summary>
	public IDocumentChunker GetChunkerForStream(Stream stream, string? fileNameHint = null)
	{
		// Try to detect from filename extension first
		if (!string.IsNullOrEmpty(fileNameHint))
		{
			var extension = Path.GetExtension(fileNameHint).ToLowerInvariant();
			var type = extension switch
			{
				".md" or ".markdown" => DocumentType.Markdown,
				".html" or ".htm" => DocumentType.Html,
				".docx" => DocumentType.Docx,
				".pptx" => DocumentType.Pptx,
				".xlsx" => DocumentType.Xlsx,
				".pdf" => DocumentType.Pdf,
				".txt" => DocumentType.PlainText,
				".csv" => DocumentType.Csv,
				".json" => DocumentType.Json,
				".xml" => DocumentType.Xml,
				".rtf" => DocumentType.Rtf,
				".eml" or ".msg" => DocumentType.Email,
				_ => DocumentType.Auto
			};

			if (type != DocumentType.Auto && _chunkers.ContainsKey(type))
			{
				return GetChunker(type);
			}
		}

		// TODO: Implement content-based detection
		// For now, throw an exception
		throw new NotSupportedException("Auto-detection from stream content is not yet implemented. Please specify the document type explicitly or provide a filename hint.");
	}

	/// <summary>
	/// Register a custom chunker.
	/// </summary>
	public void RegisterChunker(IDocumentChunker chunker)
	{
		_chunkers[chunker.SupportedType] = chunker;
	}
}
