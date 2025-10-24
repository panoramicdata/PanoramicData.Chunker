using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text;
using System.Text.RegularExpressions;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Chunks PPTX documents by extracting slides, titles, content, notes, tables, and images.
/// Uses OpenXML SDK for robust PPTX parsing and presentation structure analysis.
/// </summary>
public partial class PptxDocumentChunker : IDocumentChunker
{
	private readonly ILogger<PptxDocumentChunker>? _logger;
	private readonly ITokenCounter _tokenCounter;
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;
	private PresentationDocument? _document;
	private PresentationPart? _presentationPart;

	/// <summary>
	/// Initializes a new instance of the <see cref="PptxDocumentChunker"/> class.
	/// </summary>
	/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
	/// <param name="logger">Optional logger for diagnostic information.</param>
	public PptxDocumentChunker(ITokenCounter tokenCounter, ILogger<PptxDocumentChunker>? logger = null)
	{
		_tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
		_logger = logger;
	}

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Pptx;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
	{
		try
		{
			// Save position to restore later
			var originalPosition = documentStream.Position;

			// PPTX files are ZIP archives with a specific signature
			var buffer = new byte[4];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			// Restore position
			documentStream.Position = originalPosition;

			if (bytesRead < 4)
			{
				return false;
			}

			// Check for ZIP signature (PK\x03\x04)
			if (buffer[0] != 0x50 || buffer[1] != 0x4B || buffer[2] != 0x03 || buffer[3] != 0x04)
			{
				return false;
			}

			// Try to open as PPTX to verify structure
			try
			{
				using var doc = PresentationDocument.Open(documentStream, false);
				return doc.PresentationPart != null;
			}
			catch
			{
				return false;
			}
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
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;
		_chunks.Clear();

		try
		{
			// Open PPTX document (must be in a memory stream for OpenXML SDK)
			var memoryStream = new MemoryStream();
			await documentStream.CopyToAsync(memoryStream, cancellationToken);
			memoryStream.Position = 0;

			_document = PresentationDocument.Open(memoryStream, false);
			_presentationPart = _document.PresentationPart;

			if (_presentationPart?.Presentation == null)
			{
				throw new InvalidOperationException("Invalid PPTX document: missing presentation part");
			}

			var slideIds = _presentationPart.Presentation.SlideIdList?.ChildElements.OfType<SlideId>().ToList()
				?? [];

			_logger?.LogInformation("Opened PPTX document with {SlideCount} slides", slideIds.Count);

			// Extract chunks from each slide
			var slideNumber = 1;
			foreach (var slideId in slideIds)
			{
				var slidePart = (SlidePart)_presentationPart.GetPartById(slideId.RelationshipId!);
				ExtractSlideChunks(slidePart, slideNumber++);
			}

			// Build hierarchy
			HierarchyBuilder.BuildHierarchy(_chunks);

			_logger?.LogInformation("Extracted {ChunkCount} chunks from PPTX document", _chunks.Count);

			// Calculate statistics
			var statistics = CalculateStatistics(_chunks, startTime);

			// Perform validation if requested
			var validationResult = options.ValidateChunks
				? ValidateChunks(_chunks)
				: null;

			return new ChunkingResult
			{
				Chunks = _chunks,
				Statistics = statistics,
				ValidationResult = validationResult,
				Warnings = [],
				Success = true
			};
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error chunking PPTX document");
			return new ChunkingResult
			{
				Chunks = [],
				Statistics = new ChunkingStatistics(),
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk PPTX document: {ex.Message}"
					}
				],
				Success = false
			};
		}
		finally
		{
			_document?.Dispose();
		}
	}

	private void ExtractSlideChunks(SlidePart slidePart, int slideNumber)
	{
		var slide = slidePart.Slide;
		if (slide?.CommonSlideData == null)
		{
			return;
		}

		// Extract layout name
		var layoutPart = slidePart.SlideLayoutPart;
		var layoutName = layoutPart?.SlideLayout?.CommonSlideData?.Name?.Value;

		// Count shapes
		var shapes = slide.CommonSlideData.ShapeTree?.Elements<P.Shape>() ?? [];
		var shapeCount = shapes.Count();

		// Check for animations and transitions
		var hasAnimations = slide.Timing != null;
		var hasTransitions = slide.Transition != null;

		// Extract slide title
		string? slideTitle = null;
		var titleShape = shapes.FirstOrDefault(s => GetPlaceholderType(s) == "title" || GetPlaceholderType(s) == "ctrTitle");
		if (titleShape != null)
		{
			slideTitle = GetShapeText(titleShape);
		}

		// Check for speaker notes
		var hasNotes = slidePart.NotesSlidePart != null;

		// Create slide chunk
		var slideChunk = new PptxSlideChunk
		{
			SlideNumber = slideNumber,
			Title = slideTitle,
			LayoutName = layoutName,
			HasNotes = hasNotes,
			ShapeCount = shapeCount,
			HasAnimations = hasAnimations,
			HasTransitions = hasTransitions,
			ParentId = null,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "slide",
			Metadata = new ChunkMetadata
			{
				DocumentType = "PPTX",
				SourceId = string.Empty,
				Hierarchy = $"slide{slideNumber}",
				Tags = ["slide", $"slide{slideNumber}"],
				PageNumber = slideNumber,
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		_chunks.Add(slideChunk);

		// Extract title chunk if present
		if (titleShape != null && !string.IsNullOrWhiteSpace(slideTitle))
		{
			var titleChunk = CreateTitleChunk(titleShape, slideNumber, slideChunk.Id, false);
			if (titleChunk != null)
			{
				_chunks.Add(titleChunk);
			}
		}

		// Extract content from all shapes
		foreach (var shape in shapes)
		{
			var placeholderType = GetPlaceholderType(shape);

			// Skip title (already processed)
			if (placeholderType == "title" || placeholderType == "ctrTitle")
			{
				continue;
			}

			// Check if this is a subtitle
			if (placeholderType == "subTitle")
			{
				var subtitleChunk = CreateTitleChunk(shape, slideNumber, slideChunk.Id, true);
				if (subtitleChunk != null)
				{
					_chunks.Add(subtitleChunk);
				}
				continue;
			}

			// Extract text content
			var text = GetShapeText(shape);
			if (!string.IsNullOrWhiteSpace(text))
			{
				var contentChunk = CreateContentChunk(shape, text, slideNumber, slideChunk.Id, placeholderType);
				if (contentChunk != null)
				{
					_chunks.Add(contentChunk);
				}
			}
		}

		// Extract tables
		var tables = slide.CommonSlideData.ShapeTree?.Descendants<A.Table>() ?? [];
		foreach (var table in tables)
		{
			var tableChunk = CreateTableChunk(table, slideNumber, slideChunk.Id);
			if (tableChunk != null)
			{
				_chunks.Add(tableChunk);
			}
		}

		// Extract speaker notes
		if (hasNotes && slidePart.NotesSlidePart != null)
		{
			var notesChunk = ExtractNotesChunk(slidePart.NotesSlidePart, slideNumber, slideChunk.Id);
			if (notesChunk != null)
			{
				_chunks.Add(notesChunk);
			}
		}

		// Extract images
		ExtractImagesFromSlide(slide, slideNumber, slideChunk.Id);
	}

	private PptxTitleChunk? CreateTitleChunk(P.Shape shape, int slideNumber, Guid slideId, bool isSubtitle)
	{
		var text = GetShapeText(shape);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var placeholderType = GetPlaceholderType(shape);
		var hasFormatting = HasFormatting(shape);

		var chunk = new PptxTitleChunk
		{
			Content = text,
			SlideNumber = slideNumber,
			IsSubtitle = isSubtitle,
			PlaceholderType = placeholderType,
			HasFormatting = hasFormatting,
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = isSubtitle ? "subtitle" : "title",
			Metadata = new ChunkMetadata
			{
				DocumentType = "PPTX",
				SourceId = string.Empty,
				Hierarchy = $"slide{slideNumber}/{(isSubtitle ? "subtitle" : "title")}",
				Tags = [isSubtitle ? "subtitle" : "title", $"slide{slideNumber}"],
				PageNumber = slideNumber,
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Extract annotations
		chunk.Annotations = ExtractAnnotations(shape);

		// Calculate quality metrics
		chunk.QualityMetrics = CalculateQualityMetrics(text);

		return chunk;
	}

	private PptxContentChunk? CreateContentChunk(
		P.Shape shape,
		string text,
		int slideNumber,
		Guid slideId,
		string? placeholderType)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var shapeName = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value;
		var hasFormatting = HasFormatting(shape);

		var chunk = new PptxContentChunk
		{
			Content = text,
			SlideNumber = slideNumber,
			SourceType = placeholderType ?? "textBox",
			PlaceholderType = placeholderType,
			HasFormatting = hasFormatting,
			ShapeName = shapeName,
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "content",
			Metadata = new ChunkMetadata
			{
				DocumentType = "PPTX",
				SourceId = string.Empty,
				Hierarchy = $"slide{slideNumber}/content",
				Tags = ["content", $"slide{slideNumber}"],
				PageNumber = slideNumber,
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Extract annotations
		chunk.Annotations = ExtractAnnotations(shape);

		// Calculate quality metrics
		chunk.QualityMetrics = CalculateQualityMetrics(text);

		return chunk;
	}

	private PptxTableChunk? CreateTableChunk(A.Table table, int slideNumber, Guid slideId)
	{
		var rows = table.Elements<A.TableRow>().ToList();
		if (rows.Count == 0)
		{
			return null;
		}

		// Determine if first row is header (simplified check)
		var hasFirstRowHeader = rows.Count > 0;

		// Extract headers (from first row)
		var headers = new List<string>();
		var dataRows = rows;
		if (hasFirstRowHeader && rows.Count > 0)
		{
			headers = ExtractTableRowCells(rows[0]);
			dataRows = rows.Skip(1).ToList();
		}

		// Extract data rows
		var tableData = new List<List<string>>();
		foreach (var row in dataRows)
		{
			var cells = ExtractTableRowCells(row);
			if (cells.Count > 0)
			{
				tableData.Add(cells);
			}
		}

		if (headers.Count == 0 && tableData.Count == 0)
		{
			return null;
		}

		// Serialize table
		var serializedTable = SerializeTableAsMarkdown(headers, tableData);

		var chunk = new PptxTableChunk
		{
			Content = serializedTable,
			SlideNumber = slideNumber,
			HasFirstRowHeader = hasFirstRowHeader,
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			SerializedTable = serializedTable,
			SerializationFormat = TableSerializationFormat.Markdown,
			TableInfo = new TableMetadata
			{
				RowCount = tableData.Count,
				ColumnCount = headers.Count > 0 ? headers.Count : (tableData.Count > 0 ? tableData[0].Count : 0),
				Headers = [.. headers],
				HasHeaderRow = hasFirstRowHeader,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			Metadata = new ChunkMetadata
			{
				DocumentType = "PPTX",
				SourceId = string.Empty,
				Hierarchy = $"slide{slideNumber}/table",
				Tags = ["table", $"slide{slideNumber}"],
				PageNumber = slideNumber,
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics
		var allText = string.Join(" ", headers) + " " + string.Join(" ", tableData.SelectMany(r => r));
		chunk.QualityMetrics = CalculateQualityMetrics(allText);

		return chunk;
	}

	private PptxNotesChunk? ExtractNotesChunk(NotesSlidePart notesSlidePart, int slideNumber, Guid slideId)
	{
		var notesSlide = notesSlidePart.NotesSlide;
		if (notesSlide?.CommonSlideData == null)
		{
			return null;
		}

		var notesText = new StringBuilder();
		var shapes = notesSlide.CommonSlideData.ShapeTree?.Elements<P.Shape>() ?? [];

		foreach (var shape in shapes)
		{
			var placeholderType = GetPlaceholderType(shape);
			// Skip the slide image placeholder
			if (placeholderType == "sldImg")
			{
				continue;
			}

			var text = GetShapeText(shape);
			if (!string.IsNullOrWhiteSpace(text))
			{
				notesText.AppendLine(text);
			}
		}

		var notesContent = notesText.ToString().Trim();
		if (string.IsNullOrWhiteSpace(notesContent))
		{
			return null;
		}

		var chunk = new PptxNotesChunk
		{
			Content = notesContent,
			SlideNumber = slideNumber,
			NotesLength = notesContent.Length,
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "notes",
			Metadata = new ChunkMetadata
			{
				DocumentType = "PPTX",
				SourceId = string.Empty,
				Hierarchy = $"slide{slideNumber}/notes",
				Tags = ["notes", "speakerNotes", $"slide{slideNumber}"],
				PageNumber = slideNumber,
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics
		chunk.QualityMetrics = CalculateQualityMetrics(notesContent);

		return chunk;
	}

	private void ExtractImagesFromSlide(Slide slide, int slideNumber, Guid slideId)
	{
		// Extract pictures
		var pictures = slide.Descendants<P.Picture>();
		foreach (var picture in pictures)
		{
			var blip = picture.Descendants<A.Blip>().FirstOrDefault();
			if (blip?.Embed?.Value != null)
			{
				var chunk = CreateImageChunk("image", slideNumber, slideId, null, null);
				if (chunk != null)
				{
					_chunks.Add(chunk);
				}
			}
		}

		// Extract charts
		var graphicFrames = slide.Descendants<P.GraphicFrame>();
		foreach (var frame in graphicFrames)
		{
			var graphicData = frame.Descendants<A.GraphicData>().FirstOrDefault();
			if (graphicData?.Uri?.Value?.Contains("chart") == true)
			{
				var chunk = CreateImageChunk("chart", slideNumber, slideId, "chart", null);
				if (chunk != null)
				{
					chunk.IsChart = true;
					_chunks.Add(chunk);
				}
			}
		}
	}

	private PptxImageChunk? CreateImageChunk(
		string visualType,
		int slideNumber,
		Guid slideId,
		string? chartType,
		string? position)
	{
		var chunk = new PptxImageChunk
		{
			SlideNumber = slideNumber,
			VisualType = visualType,
			IsChart = visualType == "chart",
			ChartType = chartType,
			Position = position,
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = visualType,
			MimeType = "image/unknown",
			BinaryReference = Guid.NewGuid().ToString(), // Placeholder
			Metadata = new ChunkMetadata
			{
				DocumentType = "PPTX",
				SourceId = string.Empty,
				Hierarchy = $"slide{slideNumber}/{visualType}",
				Tags = [visualType, $"slide{slideNumber}"],
				PageNumber = slideNumber,
				HasImage = true,
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		return chunk;
	}

	private static string GetShapeText(P.Shape shape)
	{
		var textBody = shape.TextBody;
		if (textBody == null)
		{
			return string.Empty;
		}

		var sb = new StringBuilder();
		var paragraphs = textBody.Elements<A.Paragraph>();

		foreach (var paragraph in paragraphs)
		{
			var runs = paragraph.Elements<A.Run>();
			foreach (var run in runs)
			{
				var text = run.Text?.Text;
				if (!string.IsNullOrEmpty(text))
				{
					sb.Append(text);
				}
			}

			sb.AppendLine();
		}

		return WhitespaceRegex().Replace(sb.ToString(), " ").Trim();
	}

	private static string? GetPlaceholderType(P.Shape shape)
	{
		var placeholder = shape.NonVisualShapeProperties?
			.ApplicationNonVisualDrawingProperties?
			.PlaceholderShape;

		return placeholder?.Type?.Value.ToString();
	}

	private static bool HasFormatting(P.Shape shape)
	{
		var textBody = shape.TextBody;
		if (textBody == null)
		{
			return false;
		}

		var runs = textBody.Descendants<A.Run>();
		foreach (var run in runs)
		{
			var runProperties = run.RunProperties;
			if (runProperties != null)
			{
				if (runProperties.Bold != null || runProperties.Italic != null ||
					runProperties.Underline != null || runProperties.Strike != null)
				{
					return true;
				}
			}
		}

		return false;
	}

	private static List<ContentAnnotation> ExtractAnnotations(P.Shape shape)
	{
		var annotations = new List<ContentAnnotation>();
		var textBody = shape.TextBody;
		if (textBody == null)
		{
			return annotations;
		}

		var runs = textBody.Descendants<A.Run>();
		foreach (var run in runs)
		{
			var runProperties = run.RunProperties;
			if (runProperties != null)
			{
				var text = run.Text?.Text ?? string.Empty;

				if (runProperties.Bold != null)
				{
					annotations.Add(new ContentAnnotation
					{
						Type = AnnotationType.Bold,
						Attributes = new Dictionary<string, string> { ["text"] = text }
					});
				}

				if (runProperties.Italic != null)
				{
					annotations.Add(new ContentAnnotation
					{
						Type = AnnotationType.Italic,
						Attributes = new Dictionary<string, string> { ["text"] = text }
					});
				}

				if (runProperties.Underline != null)
				{
					annotations.Add(new ContentAnnotation
					{
						Type = AnnotationType.Underline,
						Attributes = new Dictionary<string, string> { ["text"] = text }
					});
				}
			}
		}

		return annotations;
	}

	private static List<string> ExtractTableRowCells(A.TableRow row)
	{
		var cells = new List<string>();

		foreach (var cell in row.Elements<A.TableCell>())
		{
			var sb = new StringBuilder();
			var textBody = cell.TextBody;
			if (textBody != null)
			{
				foreach (var paragraph in textBody.Elements<A.Paragraph>())
				{
					foreach (var run in paragraph.Elements<A.Run>())
					{
						var text = run.Text?.Text;
						if (!string.IsNullOrEmpty(text))
						{
							sb.Append(text);
						}
					}
				}
			}

			var cellText = WhitespaceRegex().Replace(sb.ToString(), " ").Trim();
			cells.Add(cellText);
		}

		return cells;
	}

	private static string SerializeTableAsMarkdown(List<string> headers, List<List<string>> rows)
	{
		var sb = new StringBuilder();

		if (headers.Count > 0)
		{
			sb.Append("| ");
			sb.AppendJoin(" | ", headers);
			sb.AppendLine(" |");

			sb.Append("| ");
			sb.AppendJoin(" | ", headers.Select(_ => "---"));
			sb.AppendLine(" |");
		}

		foreach (var row in rows)
		{
			sb.Append("| ");
			sb.AppendJoin(" | ", row);
			sb.AppendLine(" |");
		}

		return sb.ToString();
	}

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
		SemanticCompleteness = 1.0
	};

	private static ChunkingStatistics CalculateStatistics(List<ChunkerBase> chunks, DateTime startTime)
	{
		var processingTime = DateTime.UtcNow - startTime;

		return new ChunkingStatistics
		{
			TotalChunks = chunks.Count,
			StructuralChunks = chunks.OfType<StructuralChunk>().Count(),
			ContentChunks = chunks.OfType<ContentChunk>().Count(),
			VisualChunks = chunks.OfType<VisualChunk>().Count(),
			TableChunks = chunks.OfType<TableChunk>().Count(),
			MaxDepth = chunks.Count != 0 ? chunks.Max(c => c.Depth) : 0,
			ProcessingTime = processingTime,
			AverageTokensPerChunk = chunks.Count != 0 ? (int)chunks.Average(c => c.QualityMetrics?.TokenCount ?? 0) : 0,
			TotalTokens = chunks.Sum(c => c.QualityMetrics?.TokenCount ?? 0),
			MaxTokensInChunk = chunks.Count != 0 ? chunks.Max(c => c.QualityMetrics?.TokenCount ?? 0) : 0,
			MinTokensInChunk = chunks.Count != 0 ? chunks.Min(c => c.QualityMetrics?.TokenCount ?? 0) : 0
		};
	}

	private static ValidationResult ValidateChunks(List<ChunkerBase> chunks)
	{
		var issues = new List<ValidationIssue>();
		var chunkIds = new HashSet<Guid>(chunks.Select(c => c.Id));

		// Check for orphaned chunks
		foreach (var chunk in chunks.Where(c => c.ParentId.HasValue))
		{
			if (!chunkIds.Contains(chunk.ParentId!.Value))
			{
				issues.Add(new ValidationIssue
				{
					Severity = ValidationSeverity.Warning,
					Message = $"Chunk {chunk.Id} references non-existent parent {chunk.ParentId.Value}",
					ChunkId = chunk.Id,
					Code = "ORPHANED_CHUNK"
				});
			}
		}

		return new ValidationResult
		{
			IsValid = issues.Count == 0,
			Issues = issues,
			HasOrphanedChunks = issues.Any(i => i.Code == "ORPHANED_CHUNK")
		};
	}

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
}
