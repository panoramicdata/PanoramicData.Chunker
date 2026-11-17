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
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		try
		{
			var originalPosition = documentStream.Position;

			// DOCX files are ZIP archives with a specific signature
			var buffer = new byte[4];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			documentStream.Position = originalPosition;

			return bytesRead == 4 && HasZipSignature(buffer) && CanOpenDocument(documentStream);
		}
		catch
		{
			return false;
		}
	}

	/// <inheritdoc/>
	public async Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;
		_chunks.Clear();

		try
		{
			// OpenXML SDK needs a seekable stream
			var memoryStream = new MemoryStream();
			await documentStream.CopyToAsync(memoryStream, cancellationToken);
			memoryStream.Position = 0;

			var body = OpenBody(memoryStream);
			logger?.LogInformation("Opened DOCX document with {ElementCount} body elements", body.ChildElements.Count);

			ExtractChunks(body);
			HierarchyBuilder.BuildHierarchy(_chunks);
			logger?.LogInformation("Extracted {ChunkCount} chunks from DOCX document", _chunks.Count);

			return ChunkResultHelper.CreateSuccess(_chunks, startTime, options);
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking DOCX document");
			return ChunkResultHelper.CreateFailure("DOCX", ex);
		}
		finally
		{
			_document?.Dispose();
		}
	}

	private static bool HasZipSignature(byte[] buffer)
		=> buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04;

	private static bool CanOpenDocument(Stream documentStream)
	{
		try
		{
			using var doc = WordprocessingDocument.Open(documentStream, false);
			return doc.MainDocumentPart != null;
		}
		catch
		{
			return false;
		}
	}

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
