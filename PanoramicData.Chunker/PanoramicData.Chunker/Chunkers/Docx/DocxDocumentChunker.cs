using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Chunks DOCX documents by extracting paragraphs, headings, lists, tables, and images.
/// Uses OpenXML SDK for robust DOCX parsing and document structure analysis.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DocxDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public partial class DocxDocumentChunker(ITokenCounter tokenCounter, ILogger<DocxDocumentChunker>? logger = null) : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;
	private WordprocessingDocument? _document;
	private MainDocumentPart? _mainPart;

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Docx;

	/// <inheritdoc/>
	public Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
		=> OpenXmlStreamSupport.IsZipThatOpensAsync(documentStream, CanOpenDocument, cancellationToken);

	/// <inheritdoc/>
	public Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken)
		=> ChunkRunner.RunAsync("DOCX", documentStream, options, _chunks, logger, () => ExtractAsync(documentStream, cancellationToken));

	private async Task ExtractAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		_sequenceNumber = 0;

		try
		{
			var memoryStream = await OpenXmlStreamSupport.CopyToSeekableAsync(documentStream, cancellationToken);

			var body = OpenBody(memoryStream);
			logger?.LogInformation("Opened DOCX document with {ElementCount} body elements", body.ChildElements.Count);

			ExtractChunks(body);
		}
		finally
		{
			_document?.Dispose();
		}
	}

	private static bool CanOpenDocument(Stream documentStream)
		=> OpenXmlStreamSupport.Opens(documentStream, stream => WordprocessingDocument.Open(stream, false), doc => doc.MainDocumentPart != null);

	private Body OpenBody(MemoryStream memoryStream)
	{
		_document = WordprocessingDocument.Open(memoryStream, false);
		_mainPart = _document.MainDocumentPart;

		return _mainPart?.Document?.Body
			?? throw new InvalidOperationException("Invalid DOCX document: missing body");
	}

	private static ChunkMetadata CreateMetadata(string hierarchy, params string[] tags) => new()
	{
		DocumentType = "DOCX",
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		CreatedAt = DateTimeOffset.UtcNow
	};

	private void ExtractChunks(Body body)
	{
		Guid? currentSectionId = null;
		var currentDepth = 0;

		foreach (var element in body.ChildElements)
		{
			ChunkerBase? chunk = element switch
			{
				Paragraph paragraph => ProcessParagraph(paragraph, currentSectionId, currentDepth),
				Table table => ProcessTable(table, currentSectionId),
				_ => null
			};

			if (chunk == null)
			{
				continue;
			}

			_chunks.Add(chunk);

			if (chunk is DocxSectionChunk section)
			{
				currentSectionId = section.Id;
				currentDepth = section.HeadingLevel;
			}
		}
	}

	private ChunkerBase? ProcessParagraph(Paragraph paragraph, Guid? parentId, int parentDepth)
	{
		var text = GetParagraphText(paragraph);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var styleName = GetParagraphStyleName(paragraph);

		var headingLevel = GetHeadingLevel(styleName);
		if (headingLevel.HasValue)
		{
			return CreateSectionChunk(paragraph, text, styleName, headingLevel.Value, parentId, parentDepth);
		}

		var numberingInfo = GetNumberingInfo(paragraph);
		if (numberingInfo != null)
		{
			return CreateListItemChunk(paragraph, text, styleName, numberingInfo, parentId);
		}

		return IsCodeParagraph(paragraph, styleName)
			? CreateCodeBlockChunk(paragraph, text, styleName, parentId)
			: CreateParagraphChunk(paragraph, text, styleName, parentId);
	}

	private DocxSectionChunk CreateSectionChunk(
		Paragraph paragraph,
		string text,
		string? styleName,
		int headingLevel,
		Guid? parentId,
		int parentDepth)
	{
		var numberingInfo = GetNumberingInfo(paragraph);

		return new DocxSectionChunk
		{
			Content = text,
			HeadingLevel = headingLevel,
			StyleName = styleName,
			HasNumbering = numberingInfo != null,
			NumberingText = numberingInfo?.NumberingText,
			ParentId = AdjustParentForHeading(parentId, headingLevel, parentDepth),
			SequenceNumber = _sequenceNumber++,
			SpecificType = $"Heading{headingLevel}",
			Metadata = CreateMetadata($"h{headingLevel}", $"heading{headingLevel}", "section"),
			QualityMetrics = CalculateQualityMetrics(text)
		};
	}

	private DocxParagraphChunk CreateParagraphChunk(Paragraph paragraph, string text, string? styleName, Guid? parentId) => new()
	{
		Content = text,
		StyleName = styleName,
		Alignment = GetParagraphAlignment(paragraph),
		IndentationLevel = GetIndentationLevel(paragraph),
		HasFormatting = HasFormatting(paragraph),
		ParentId = parentId,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "paragraph",
		Metadata = CreateMetadata("paragraph", "paragraph"),
		Annotations = ExtractAnnotations(paragraph),
		QualityMetrics = CalculateQualityMetrics(text)
	};

	private DocxListItemChunk CreateListItemChunk(
		Paragraph paragraph,
		string text,
		string? styleName,
		NumberingInfo numberingInfo,
		Guid? parentId) => new()
		{
			Content = text,
			StyleName = styleName,
			IsNumbered = numberingInfo.IsNumbered,
			ListLevel = numberingInfo.Level,
			NumberingText = numberingInfo.NumberingText,
			NumberingFormat = numberingInfo.Format,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "listItem",
			Metadata = CreateMetadata("listItem", "list", numberingInfo.IsNumbered ? "numbered" : "bullet"),
			Annotations = ExtractAnnotations(paragraph),
			QualityMetrics = CalculateQualityMetrics(text)
		};

	private DocxCodeBlockChunk CreateCodeBlockChunk(Paragraph paragraph, string text, string? styleName, Guid? parentId) => new()
	{
		Content = text,
		StyleName = styleName,
		IsMonospace = IsMonospaceParagraph(paragraph),
		Language = null,
		HasSyntaxHighlighting = false,
		ParentId = parentId,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "codeBlock",
		Metadata = CreateMetadata("code", "code", "preformatted"),
		QualityMetrics = CalculateQualityMetrics(text)
	};

	private Guid? AdjustParentForHeading(Guid? currentParentId, int headingLevel, int currentDepth)
	{
		if (headingLevel > currentDepth)
		{
			return currentParentId;
		}

		// A top-level heading has no parent; otherwise use the nearest higher-level heading
		return headingLevel == 1
			? null
			: _chunks.OfType<DocxSectionChunk>().LastOrDefault(s => s.HeadingLevel < headingLevel)?.Id;
	}

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
		SemanticCompleteness = 1.0
	};
}
