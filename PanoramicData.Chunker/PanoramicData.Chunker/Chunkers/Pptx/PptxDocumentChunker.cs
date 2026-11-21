using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using A = DocumentFormat.OpenXml.Drawing;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;
using P = DocumentFormat.OpenXml.Presentation;

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

	private void ExtractSlideChunks(SlidePart slidePart, int slideNumber)
	{
		var slide = slidePart.Slide;
		if (slide?.CommonSlideData == null)
		{
			return;
		}

		var shapeTree = slide.CommonSlideData.ShapeTree;
		var shapes = (shapeTree?.Elements<P.Shape>() ?? []).ToList();
		var titleShape = shapes.FirstOrDefault(s => IsTitlePlaceholder(GetPlaceholderType(s)));
		var slideTitle = titleShape == null ? null : GetShapeText(titleShape);

		var slideChunk = CreateSlideChunk(slidePart, slide, shapes.Count, slideTitle, slideNumber);
		_chunks.Add(slideChunk);

		if (titleShape != null && !string.IsNullOrWhiteSpace(slideTitle))
		{
			AddChunk(CreateTitleChunk(titleShape, slideNumber, slideChunk.Id, false));
		}

		foreach (var shape in shapes)
		{
			ExtractShapeChunk(shape, slideNumber, slideChunk.Id);
		}

		ExtractSlideAttachments(slidePart, slide, slideNumber, slideChunk.Id);
	}

	private void ExtractSlideAttachments(SlidePart slidePart, Slide slide, int slideNumber, Guid slideId)
	{
		foreach (var table in slide.CommonSlideData?.ShapeTree?.Descendants<A.Table>() ?? [])
		{
			AddChunk(CreateTableChunk(table, slideNumber, slideId));
		}

		if (slidePart.NotesSlidePart != null)
		{
			AddChunk(ExtractNotesChunk(slidePart.NotesSlidePart, slideNumber, slideId));
		}

		ExtractImagesFromSlide(slide, slideNumber, slideId);
	}

	private PptxSlideChunk CreateSlideChunk(SlidePart slidePart, Slide slide, int shapeCount, string? slideTitle, int slideNumber) => new()
	{
		SlideNumber = slideNumber,
		Title = slideTitle,
		LayoutName = slidePart.SlideLayoutPart?.SlideLayout?.CommonSlideData?.Name?.Value,
		HasNotes = slidePart.NotesSlidePart != null,
		ShapeCount = shapeCount,
		HasAnimations = slide.Timing != null,
		HasTransitions = slide.Transition != null,
		ParentId = null,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "slide",
		Metadata = CreateMetadata(slideNumber, $"slide{slideNumber}", "slide", $"slide{slideNumber}")
	};

	private void ExtractShapeChunk(P.Shape shape, int slideNumber, Guid slideId)
	{
		var placeholderType = GetPlaceholderType(shape);

		// The title was already processed
		if (IsTitlePlaceholder(placeholderType))
		{
			return;
		}

		if (placeholderType == "subTitle")
		{
			AddChunk(CreateTitleChunk(shape, slideNumber, slideId, true));
			return;
		}

		var text = GetShapeText(shape);
		if (!string.IsNullOrWhiteSpace(text))
		{
			AddChunk(CreateContentChunk(shape, text, slideNumber, slideId, placeholderType));
		}
	}

	private static bool IsTitlePlaceholder(string? placeholderType) => placeholderType is "title" or "ctrTitle";

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => ChunkFactory.CreateQualityMetrics(_tokenCounter, text, ChunkFactory.CountSpaceSeparatedWords(text));
}
