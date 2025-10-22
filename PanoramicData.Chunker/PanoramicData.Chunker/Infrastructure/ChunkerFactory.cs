using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Chunkers.PlainText;
using PanoramicData.Chunker.Chunkers.Pptx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;

namespace PanoramicData.Chunker.Infrastructure;

/// <summary>
/// Factory for creating document chunkers.
/// </summary>
public class ChunkerFactory : IChunkerFactory
{
	private readonly Dictionary<DocumentType, IDocumentChunker> _chunkers = [];
	private readonly ITokenCounter _defaultTokenCounter;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChunkerFactory"/> class.
	/// </summary>
	/// <param name="tokenCounter">Optional token counter to use for chunkers. If not provided, uses CharacterBasedTokenCounter.</param>
	public ChunkerFactory(ITokenCounter? tokenCounter = null)
	{
		_defaultTokenCounter = tokenCounter ?? new CharacterBasedTokenCounter();
		RegisterDefaultChunkers();
	}

	/// <summary>
	/// Registers default chunkers for supported document types.
	/// </summary>
	private void RegisterDefaultChunkers()
	{
		RegisterChunker(new MarkdownDocumentChunker(_defaultTokenCounter));
		RegisterChunker(new HtmlDocumentChunker(_defaultTokenCounter));
		RegisterChunker(new PlainTextDocumentChunker(_defaultTokenCounter));
		RegisterChunker(new DocxDocumentChunker(_defaultTokenCounter));
		RegisterChunker(new PptxDocumentChunker(_defaultTokenCounter));
	}

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
	public async Task<IDocumentChunker> GetChunkerForStreamAsync(Stream stream, string? fileNameHint = null, CancellationToken cancellationToken = default)
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

			if (type != DocumentType.Auto && _chunkers.TryGetValue(type, out var chunker))
			{
				return chunker;
			}
		}

		// Content-based detection - try each registered chunker
		foreach (var chunker in _chunkers.Values)
		{
			if (await chunker.CanHandleAsync(stream, cancellationToken))
			{
				return chunker;
			}
		}

		throw new NotSupportedException("Unable to detect document type from stream content. Please specify the document type explicitly or provide a filename hint with a recognized extension.");
	}

	/// <summary>
	/// Get a chunker by auto-detecting the document type from the stream (synchronous wrapper).
	/// </summary>
	public IDocumentChunker GetChunkerForStream(Stream stream, string? fileNameHint = null)
		=> GetChunkerForStreamAsync(stream, fileNameHint).GetAwaiter().GetResult();

	/// <summary>
	/// Register a custom chunker.
	/// </summary>
	public void RegisterChunker(IDocumentChunker chunker)
		=> _chunkers[chunker.SupportedType] = chunker;

	/// <summary>
	/// Get all registered document types.
	/// </summary>
	public IReadOnlyCollection<DocumentType> GetSupportedTypes()
		=> _chunkers.Keys.ToList().AsReadOnly();
}
