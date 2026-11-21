using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Chunks PPTX documents by extracting slides, titles, content, notes, tables, and images.
/// Uses OpenXML SDK for robust PPTX parsing and presentation structure analysis.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PptxDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public partial class PptxDocumentChunker(ITokenCounter tokenCounter, ILogger<PptxDocumentChunker>? logger = null) : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;
	private PresentationDocument? _document;
	private PresentationPart? _presentationPart;

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Pptx;

	/// <inheritdoc/>
	public Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
		=> OpenXmlStreamSupport.IsZipThatOpensAsync(documentStream, CanOpenPresentation, cancellationToken);

	/// <inheritdoc/>
	public Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken)
		=> ChunkRunner.RunAsync("PPTX", documentStream, options, _chunks, logger, () => ExtractPptxAsync(documentStream, cancellationToken));

	private static bool CanOpenPresentation(Stream documentStream)
		=> OpenXmlStreamSupport.Opens(documentStream, stream => PresentationDocument.Open(stream, false), doc => doc.PresentationPart != null);

	private async Task ExtractPptxAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		_sequenceNumber = 0;

		try
		{
			var memoryStream = await OpenXmlStreamSupport.CopyToSeekableAsync(documentStream, cancellationToken);

			var slideIds = OpenSlideIds(memoryStream);
			logger?.LogInformation("Opened PPTX document with {SlideCount} slides", slideIds.Count);

			var slideNumber = 1;
			foreach (var slideId in slideIds)
			{
				var slidePart = (SlidePart)_presentationPart!.GetPartById(slideId.RelationshipId!);
				ExtractSlideChunks(slidePart, slideNumber++);
			}
		}
		finally
		{
			_document?.Dispose();
		}
	}

	private List<SlideId> OpenSlideIds(MemoryStream memoryStream)
	{
		_document = PresentationDocument.Open(memoryStream, false);
		_presentationPart = _document.PresentationPart;

		return _presentationPart?.Presentation == null
			? throw new InvalidOperationException("Invalid PPTX document: missing presentation part")
			: _presentationPart.Presentation.SlideIdList?.ChildElements.OfType<SlideId>().ToList() ?? [];
	}

	private static ChunkMetadata CreateMetadata(int slideNumber, string hierarchy, params string[] tags) => new()
	{
		DocumentType = "PPTX",
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		PageNumber = slideNumber,
		CreatedAt = DateTimeOffset.UtcNow
	};

	private void AddChunk(ChunkerBase? chunk)
	{
		if (chunk != null)
		{
			_chunks.Add(chunk);
		}
	}
}
