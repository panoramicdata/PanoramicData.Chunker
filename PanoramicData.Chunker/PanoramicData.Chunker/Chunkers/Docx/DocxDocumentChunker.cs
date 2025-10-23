using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Chunks DOCX documents by extracting paragraphs, headings, lists, tables, and images.
/// Uses OpenXML SDK for robust DOCX parsing and document structure analysis.
/// </summary>
public partial class DocxDocumentChunker : IDocumentChunker
{
	private readonly ILogger<DocxDocumentChunker>? _logger;
	private readonly ITokenCounter _tokenCounter;
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;
	private WordprocessingDocument? _document;
	private MainDocumentPart? _mainPart;

	/// <summary>
	/// Initializes a new instance of the <see cref="DocxDocumentChunker"/> class.
	/// </summary>
	/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
	/// <param name="logger">Optional logger for diagnostic information.</param>
	public DocxDocumentChunker(ITokenCounter tokenCounter, ILogger<DocxDocumentChunker>? logger = null)
	{
		_tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
		_logger = logger;
	}

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Docx;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
	{
		try
		{
			// Save position to restore later
			var originalPosition = documentStream.Position;

			// DOCX files are ZIP archives with a specific signature
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

			// Try to open as DOCX to verify structure
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
			// Open DOCX document (must be in a memory stream for OpenXML SDK)
			var memoryStream = new MemoryStream();
			await documentStream.CopyToAsync(memoryStream, cancellationToken);
			memoryStream.Position = 0;

			_document = WordprocessingDocument.Open(memoryStream, false);
			_mainPart = _document.MainDocumentPart;

			if (_mainPart?.Document?.Body == null)
			{
				throw new InvalidOperationException("Invalid DOCX document: missing body");
			}

			_logger?.LogInformation("Opened DOCX document with {ElementCount} body elements",
				_mainPart.Document.Body.ChildElements.Count);

			// Extract chunks from the document body
			ExtractChunks(_mainPart.Document.Body);

			// Build hierarchy
			HierarchyBuilder.BuildHierarchy(_chunks);

			_logger?.LogInformation("Extracted {ChunkCount} chunks from DOCX document", _chunks.Count);

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
			_logger?.LogError(ex, "Error chunking DOCX document");
			return new ChunkingResult
			{
				Chunks = [],
				Statistics = new ChunkingStatistics(),
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk DOCX document: {ex.Message}"
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

	private void ExtractChunks(Body body)
	{
		Guid? currentSectionId = null;
		var currentDepth = 0;

		foreach (var element in body.ChildElements)
		{
			// Process paragraphs
			if (element is Paragraph paragraph)
			{
				var chunk = ProcessParagraph(paragraph, currentSectionId, currentDepth);
				if (chunk != null)
				{
					_chunks.Add(chunk);

					// If this is a section (heading), update current section context
					if (chunk is DocxSectionChunk section)
					{
						currentSectionId = section.Id;
						currentDepth = section.HeadingLevel;
					}
				}
			}
			// Process tables
			else if (element is Table table)
			{
				var tableChunk = ProcessTable(table, currentSectionId, currentDepth);
				if (tableChunk != null)
				{
					_chunks.Add(tableChunk);
				}
			}
		}
	}

	private ChunkerBase? ProcessParagraph(Paragraph paragraph, Guid? parentId, int parentDepth)
	{
		// Get paragraph text
		var text = GetParagraphText(paragraph);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null; // Skip empty paragraphs
		}

		// Get paragraph style
		var styleName = GetParagraphStyleName(paragraph);

		// Check if this is a heading
		var headingLevel = GetHeadingLevel(styleName);
		if (headingLevel.HasValue)
		{
			return CreateSectionChunk(paragraph, text, styleName, headingLevel.Value, parentId, parentDepth);
		}

		// Check if this is a list item
		var numberingInfo = GetNumberingInfo(paragraph);
		if (numberingInfo != null)
		{
			return CreateListItemChunk(paragraph, text, styleName, numberingInfo, parentId, parentDepth);
		}

		// Check if this is a code block (by style or monospace font)
		if (IsCodeParagraph(paragraph, styleName))
		{
			return CreateCodeBlockChunk(paragraph, text, styleName, parentId, parentDepth);
		}

		// Default: normal paragraph
		return CreateParagraphChunk(paragraph, text, styleName, parentId, parentDepth);
	}

	private DocxSectionChunk CreateSectionChunk(
		Paragraph paragraph,
		string text,
		string? styleName,
		int headingLevel,
		Guid? parentId,
		int parentDepth)
	{
		// Adjust parent ID based on heading hierarchy
		var adjustedParentId = AdjustParentForHeading(parentId, headingLevel, parentDepth);

		var numberingInfo = GetNumberingInfo(paragraph);

		var chunk = new DocxSectionChunk
		{
			Content = text,
			HeadingLevel = headingLevel,
			StyleName = styleName,
			HasNumbering = numberingInfo != null,
			NumberingText = numberingInfo?.NumberingText,
			ParentId = adjustedParentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = $"Heading{headingLevel}",
			Metadata = new ChunkMetadata
			{
				DocumentType = "DOCX",
				SourceId = string.Empty,
				Hierarchy = $"h{headingLevel}",
				Tags = [$"heading{headingLevel}", "section"],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics
		chunk.QualityMetrics = CalculateQualityMetrics(text);

		return chunk;
	}

	private DocxParagraphChunk CreateParagraphChunk(
		Paragraph paragraph,
		string text,
		string? styleName,
		Guid? parentId,
		int parentDepth)
	{
		var alignment = GetParagraphAlignment(paragraph);
		var indentationLevel = GetIndentationLevel(paragraph);
		var hasFormatting = HasFormatting(paragraph);

		var chunk = new DocxParagraphChunk
		{
			Content = text,
			StyleName = styleName,
			Alignment = alignment,
			IndentationLevel = indentationLevel,
			HasFormatting = hasFormatting,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "paragraph",
			Metadata = new ChunkMetadata
			{
				DocumentType = "DOCX",
				SourceId = string.Empty,
				Hierarchy = "paragraph",
				Tags = ["paragraph"],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Extract annotations (hyperlinks, formatting)
		chunk.Annotations = ExtractAnnotations(paragraph);

		// Calculate quality metrics
		chunk.QualityMetrics = CalculateQualityMetrics(text);

		return chunk;
	}

	private DocxListItemChunk CreateListItemChunk(
		Paragraph paragraph,
		string text,
		string? styleName,
		NumberingInfo numberingInfo,
		Guid? parentId,
		int parentDepth)
	{
		var chunk = new DocxListItemChunk
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
			Metadata = new ChunkMetadata
			{
				DocumentType = "DOCX",
				SourceId = string.Empty,
				Hierarchy = "listItem",
				Tags = ["list", numberingInfo.IsNumbered ? "numbered" : "bullet"],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Extract annotations
		chunk.Annotations = ExtractAnnotations(paragraph);

		// Calculate quality metrics
		chunk.QualityMetrics = CalculateQualityMetrics(text);

		return chunk;
	}

	private DocxCodeBlockChunk CreateCodeBlockChunk(
		Paragraph paragraph,
		string text,
		string? styleName,
		Guid? parentId,
		int parentDepth)
	{
		var isMonospace = IsMonospaceParagraph(paragraph);

		var chunk = new DocxCodeBlockChunk
		{
			Content = text,
			StyleName = styleName,
			IsMonospace = isMonospace,
			Language = null, // We can't reliably detect language in DOCX
			HasSyntaxHighlighting = false,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "codeBlock",
			Metadata = new ChunkMetadata
			{
				DocumentType = "DOCX",
				SourceId = string.Empty,
				Hierarchy = "code",
				Tags = ["code", "preformatted"],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics
		chunk.QualityMetrics = CalculateQualityMetrics(text);

		return chunk;
	}

	private DocxTableChunk? ProcessTable(Table table, Guid? parentId, int parentDepth)
	{
		var rows = table.Elements<TableRow>().ToList();
		if (rows.Count == 0)
		{
			return null;
		}

		// Extract table properties
		var tableProperties = table.GetFirstChild<TableProperties>();
		var tableStyle = tableProperties?.TableStyle?.Val?.Value;
		var caption = tableProperties?.TableCaption?.Val?.Value;
		var description = tableProperties?.TableDescription?.Val?.Value;

		// Determine if first row is header
		var hasHeaderRow = rows.Count > 0 && IsHeaderRow(rows[0]);

		// Extract headers
		var headers = new List<string>();
		var dataRows = rows;
		if (hasHeaderRow)
		{
			headers = ExtractRowCells(rows[0]);
			dataRows = rows.Skip(1).ToList();
		}

		// Extract data rows
		var tableData = new List<List<string>>();
		foreach (var row in dataRows)
		{
			var cells = ExtractRowCells(row);
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

		var chunk = new DocxTableChunk
		{
			Content = serializedTable,
			TableStyle = tableStyle,
			Caption = caption,
			Description = description,
			HasBandedRows = false, // Could be detected from style if needed
			HasBandedColumns = false,
			Position = "inline",
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			SerializedTable = serializedTable,
			SerializationFormat = TableSerializationFormat.Markdown,
			TableInfo = new TableMetadata
			{
				RowCount = tableData.Count,
				ColumnCount = headers.Count > 0 ? headers.Count : (tableData.Count > 0 ? tableData[0].Count : 0),
				Headers = [.. headers],
				HasHeaderRow = hasHeaderRow,
				HasMergedCells = HasMergedCells(table),
				PreferredFormat = TableSerializationFormat.Markdown
			},
			Metadata = new ChunkMetadata
			{
				DocumentType = "DOCX",
				SourceId = string.Empty,
				Hierarchy = "table",
				Tags = ["table"],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics
		var allText = string.Join(" ", headers) + " " + string.Join(" ", tableData.SelectMany(r => r));
		chunk.QualityMetrics = CalculateQualityMetrics(allText);

		return chunk;
	}

	private static string GetParagraphText(Paragraph paragraph)
	{
		var text = paragraph.InnerText;
		return WhitespaceRegex().Replace(text, " ").Trim();
	}

	private static string? GetParagraphStyleName(Paragraph paragraph)
	{
		var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>();
		var styleId = paragraphProperties?.ParagraphStyleId?.Val?.Value;
		return styleId;
	}

	private static int? GetHeadingLevel(string? styleName)
	{
		if (string.IsNullOrEmpty(styleName))
		{
			return null;
		}

		// Common heading style names
		return styleName.ToLowerInvariant() switch
		{
			"heading1" or "heading 1" or "h1" => 1,
			"heading2" or "heading 2" or "h2" => 2,
			"heading3" or "heading 3" or "h3" => 3,
			"heading4" or "heading 4" or "h4" => 4,
			"heading5" or "heading 5" or "h5" => 5,
			"heading6" or "heading 6" or "h6" => 6,
			"title" => 1, // Treat title as H1
			"subtitle" => 2, // Treat subtitle as H2
			_ => null
		};
	}

	private NumberingInfo? GetNumberingInfo(Paragraph paragraph)
	{
		var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>();
		var numberingProperties = paragraphProperties?.NumberingProperties;

		if (numberingProperties == null)
		{
			return null;
		}

		var numId = numberingProperties.NumberingId?.Val?.Value;
		var levelIndex = numberingProperties.NumberingLevelReference?.Val?.Value ?? 0;

		if (!numId.HasValue)
		{
			return null;
		}

		// Get numbering definition to determine format
		var numberingPart = _mainPart?.NumberingDefinitionsPart;
		if (numberingPart == null)
		{
			return new NumberingInfo
			{
				IsNumbered = true,
				Level = levelIndex,
				NumberingText = null,
				Format = "unknown"
			};
		}

		// Try to determine if this is numbered or bullet
		// This is simplified - a full implementation would parse the numbering definitions
		var isNumbered = true; // Default to numbered

		return new NumberingInfo
		{
			IsNumbered = isNumbered,
			Level = levelIndex,
			NumberingText = null, // Would need complex logic to extract actual number
			Format = isNumbered ? "decimal" : "bullet"
		};
	}

	private static bool IsCodeParagraph(Paragraph paragraph, string? styleName)
	{
		// Check style name
		if (!string.IsNullOrEmpty(styleName))
		{
			var lowerStyle = styleName.ToLowerInvariant();
			if (lowerStyle.Contains("code") || lowerStyle.Contains("html") || lowerStyle.Contains("preformatted"))
			{
				return true;
			}
		}

		// Check for monospace font
		return IsMonospaceParagraph(paragraph);
	}

	private static bool IsMonospaceParagraph(Paragraph paragraph)
	{
		var runs = paragraph.Elements<Run>().ToList();
		if (runs.Count == 0)
		{
			return false;
		}

		// Check if all runs use monospace fonts
		var monospaceFonts = new[] { "courier", "consolas", "monaco", "menlo", "source code pro", "monospace" };

		foreach (var run in runs)
		{
			var runProperties = run.RunProperties;
			var fontName = runProperties?.RunFonts?.Ascii?.Value?.ToLowerInvariant();

			if (fontName != null && monospaceFonts.Any(f => fontName.Contains(f)))
			{
				return true;
			}
		}

		return false;
	}

	private static string? GetParagraphAlignment(Paragraph paragraph)
	{
		var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>();
		var justification = paragraphProperties?.Justification?.Val?.Value;

		return justification?.ToString().ToLowerInvariant();
	}

	private static int GetIndentationLevel(Paragraph paragraph)
	{
		var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>();
		var indentation = paragraphProperties?.Indentation;

		if (indentation?.Left?.Value != null)
		{
			// Convert from twentieths of a point to a simple level (every 720 = 1 level)
			var leftIndent = int.Parse(indentation.Left.Value);
			return leftIndent / 720;
		}

		return 0;
	}

	private static bool HasFormatting(Paragraph paragraph)
	{
		var runs = paragraph.Elements<Run>();

		foreach (var run in runs)
		{
			var runProperties = run.RunProperties;
			if (runProperties != null)
			{
				if (runProperties.Bold != null || runProperties.Italic != null ||
					runProperties.Underline != null || runProperties.Strike != null ||
					runProperties.Color != null || runProperties.Highlight != null)
				{
					return true;
				}
			}
		}

		return false;
	}

	private static List<ContentAnnotation> ExtractAnnotations(Paragraph paragraph)
	{
		var annotations = new List<ContentAnnotation>();

		// Extract hyperlinks
		var hyperlinks = paragraph.Descendants<Hyperlink>();
		foreach (var hyperlink in hyperlinks)
		{
			var relationshipId = hyperlink.Id?.Value;
			if (relationshipId != null)
			{
				var text = hyperlink.InnerText;
				// Would need to resolve relationship to get actual URL
				annotations.Add(new ContentAnnotation
				{
					Type = AnnotationType.Link,
					Attributes = new Dictionary<string, string>
					{
						["text"] = text,
						["relationshipId"] = relationshipId
					}
				});
			}
		}

		// Extract formatted text (bold, italic, etc.)
		var runs = paragraph.Elements<Run>();
		foreach (var run in runs)
		{
			var runProperties = run.RunProperties;
			if (runProperties != null)
			{
				var text = run.InnerText;

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

	private static bool IsHeaderRow(TableRow row)
	{
		// Check if cells in the row have table header properties
		var cells = row.Elements<TableCell>();
		return cells.Any(cell =>
		{
			var cellProperties = cell.GetFirstChild<TableCellProperties>();
			var runProperties = cell.Descendants<RunProperties>().FirstOrDefault();

			// Header rows typically have bold text
			return runProperties?.Bold != null;
		});
	}

	private static List<string> ExtractRowCells(TableRow row)
	{
		var cells = new List<string>();

		foreach (var cell in row.Elements<TableCell>())
		{
			var cellText = cell.InnerText;
			cellText = WhitespaceRegex().Replace(cellText, " ").Trim();
			cells.Add(cellText);
		}

		return cells;
	}

	private static bool HasMergedCells(Table table)
	{
		// Check if any cells have gridSpan or vMerge properties
		foreach (var row in table.Elements<TableRow>())
		{
			foreach (var cell in row.Elements<TableCell>())
			{
				var cellProperties = cell.GetFirstChild<TableCellProperties>();
				if (cellProperties?.GridSpan != null || cellProperties?.VerticalMerge != null)
				{
					return true;
				}
			}
		}

		return false;
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

	private Guid? AdjustParentForHeading(Guid? currentParentId, int headingLevel, int currentDepth)
	{
		if (headingLevel <= currentDepth)
		{
			// This heading is at the same level or higher, so find the appropriate parent
			// For simplicity, set parent to null if it's a top-level heading
			if (headingLevel == 1)
			{
				return null;
			}

			// Find the nearest parent heading at a higher level
			var parentHeading = _chunks
				.OfType<DocxSectionChunk>()
				.Where(s => s.HeadingLevel < headingLevel)
				.LastOrDefault();

			return parentHeading?.Id;
		}

		return currentParentId;
	}

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new ChunkQualityMetrics
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

	private class NumberingInfo
	{
		public bool IsNumbered { get; set; }
		public int Level { get; set; }
		public string? NumberingText { get; set; }
		public string? Format { get; set; }
	}
}
