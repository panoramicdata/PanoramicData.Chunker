using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Pdf;

/// <summary>
/// Chunks PDF documents by extracting text from pages and identifying paragraphs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PdfDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public class PdfDocumentChunker(ITokenCounter tokenCounter, ILogger<PdfDocumentChunker>? logger = null) : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Pdf;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
	{
		try
		{
			// Save position to restore later
			var originalPosition = documentStream.Position;

			// Check PDF signature (%PDF-)
			var buffer = new byte[5];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			// Restore position
			documentStream.Position = originalPosition;

			if (bytesRead < 5)
			{
				return false;
			}

			// PDF files start with %PDF-
			return buffer[0] == 0x25 && // %
				   buffer[1] == 0x50 && // P
				   buffer[2] == 0x44 && // D
				buffer[3] == 0x46 && // F
				   buffer[4] == 0x2D;   // -
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
			// Copy stream to memory if needed (PdfPig requires seekable stream)
			var workingStream = documentStream;
			if (!documentStream.CanSeek)
			{
				var memoryStream = new MemoryStream();
				await documentStream.CopyToAsync(memoryStream, cancellationToken);
				memoryStream.Position = 0;
				workingStream = memoryStream;
			}

			// Open PDF document
			using var pdf = PdfDocument.Open(workingStream);

			if (pdf.NumberOfPages == 0)
			{
				logger?.LogInformation("Empty PDF document");
				return CreateEmptyResult(startTime);
			}

			// Create document chunk
			var documentChunk = CreateDocumentChunk(pdf);
			_chunks.Add(documentChunk);

			logger?.LogInformation("Processing PDF with {PageCount} pages", pdf.NumberOfPages);

			// Process each page
			for (var pageIndex = 0; pageIndex < pdf.NumberOfPages; pageIndex++)
			{
				var page = pdf.GetPage(pageIndex + 1); // Pages are 1-based in PdfPig

				var pageChunk = CreatePageChunk(page, documentChunk.Id);
				_chunks.Add(pageChunk);

				// Extract paragraphs from page
				var paragraphs = ExtractParagraphsFromPage(page, pageChunk.Id);
				_chunks.AddRange(paragraphs);
			}

			// Build hierarchy
			HierarchyBuilder.BuildHierarchy(_chunks);

			logger?.LogInformation("Extracted {ChunkCount} chunks from PDF ({PageCount} pages)",
				_chunks.Count, pdf.NumberOfPages);

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
			logger?.LogError(ex, "Error chunking PDF document");
			return new ChunkingResult
			{
				Chunks = [],
				Statistics = new ChunkingStatistics(),
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk PDF document: {ex.Message}"
					}
				],
				Success = false
			};
		}
	}

	private PdfDocumentChunk CreateDocumentChunk(PdfDocument pdf)
	{
		var info = pdf.Information;

		var chunk = new PdfDocumentChunk
		{
			PdfVersion = pdf.Version.ToString(),
			PageCount = pdf.NumberOfPages,
			Title = info?.Title,
			Author = info?.Author,
			Subject = info?.Subject,
			CreationDate = TryParseDate(info?.CreationDate),
			ModificationDate = TryParseDate(info?.ModifiedDate),
			IsEncrypted = false, // PdfPig handles decryption automatically
			ParentId = null,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "pdf-document",
			Metadata = new ChunkMetadata
			{
				DocumentType = "PDF",
				SourceId = string.Empty,
				Hierarchy = "pdf",
				Tags = ["pdf", "document"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 0,
				CharacterCount = 0,
				WordCount = 0,
				SemanticCompleteness = 1.0
			}
		};

		return chunk;
	}

	private PdfPageChunk CreatePageChunk(Page page, Guid documentId)
	{
		var text = page.Text;
		var wordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

		var chunk = new PdfPageChunk
		{
			PageNumber = page.Number,
			Width = (double)page.Width,
			Height = (double)page.Height,
			Rotation = (int)page.Rotation.Value,
			ExtractedText = text,
			WordCount = wordCount,
			ParentId = documentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "pdf-page",
			Metadata = new ChunkMetadata
			{
				DocumentType = "PDF",
				SourceId = string.Empty,
				Hierarchy = $"pdf/page{page.Number}",
				Tags = ["pdf-page"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			// Calculate quality metrics
			QualityMetrics = CalculateQualityMetrics(text)
		};

		return chunk;
	}

	private List<PdfParagraphChunk> ExtractParagraphsFromPage(Page page, Guid pageId)
	{
		var paragraphs = new List<PdfParagraphChunk>();
		var text = page.Text;

		if (string.IsNullOrWhiteSpace(text))
		{
			return paragraphs;
		}

		// Split text into paragraphs (simple heuristic: double newline or significant spacing)
		var paragraphTexts = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);

		for (var i = 0; i < paragraphTexts.Length; i++)
		{
			var paragraphText = paragraphTexts[i].Trim();

			if (string.IsNullOrWhiteSpace(paragraphText))
			{
				continue;
			}

			// Detect if this is likely a heading (short text, often larger font)
			var isLikelyHeading = IsLikelyHeading(paragraphText);

			var chunk = new PdfParagraphChunk
			{
				Content = paragraphText,
				PageNumber = page.Number,
				ParagraphIndex = i,
				YPosition = 0, // Would need more complex analysis to determine exact position
				FontSize = null, // Would need letter-level analysis
				IsLikelyHeading = isLikelyHeading,
				ParentId = pageId,
				SequenceNumber = _sequenceNumber++,
				SpecificType = "pdf-paragraph",
				Metadata = new ChunkMetadata
				{
					DocumentType = "PDF",
					SourceId = string.Empty,
					Hierarchy = $"pdf/page{page.Number}/para{i}",
					Tags = isLikelyHeading ? ["pdf-paragraph", "heading"] : ["pdf-paragraph"],
					CreatedAt = DateTimeOffset.UtcNow
				},
				// Calculate quality metrics
				QualityMetrics = CalculateQualityMetrics(paragraphText)
			};

			paragraphs.Add(chunk);
		}

		return paragraphs;
	}

	private static bool IsLikelyHeading(string text)
	{
		// Heuristic: Short text (< 100 chars), no ending punctuation, often contains uppercase
		if (text.Length > 100)
		{
			return false;
		}

		// Check if ends with period (headings typically don't)
		if (text.EndsWith('.') || text.EndsWith(','))
		{
			return false;
		}

		// Check if has significant uppercase content
		var uppercaseRatio = text.Count(char.IsUpper) / (double)text.Length;
		if (uppercaseRatio > 0.3) // More than 30% uppercase
		{
			return true;
		}

		// Check for common heading patterns
		if (text.StartsWith("Chapter ", StringComparison.OrdinalIgnoreCase) ||
			text.StartsWith("Section ", StringComparison.OrdinalIgnoreCase) ||
			text.StartsWith("Part ", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
	}

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length,
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

	private static ChunkingResult CreateEmptyResult(DateTime startTime) => new()
	{
		Chunks = [],
		Statistics = new ChunkingStatistics
		{
			ProcessingTime = DateTime.UtcNow - startTime
		},
		Warnings = [],
		Success = true
	};

	private static DateTime? TryParseDate(string? dateString)
	{
		if (string.IsNullOrWhiteSpace(dateString))
		{
			return null;
		}

		// PDF dates are in format: D:YYYYMMDDHHmmSS
		if (dateString.StartsWith("D:") && dateString.Length >= 16)
		{
			var year = int.Parse(dateString.Substring(2, 4));
			var month = int.Parse(dateString.Substring(6, 2));
			var day = int.Parse(dateString.Substring(8, 2));
			var hour = int.Parse(dateString.Substring(10, 2));
			var minute = int.Parse(dateString.Substring(12, 2));
			var second = int.Parse(dateString.Substring(14, 2));

			return new DateTime(year, month, day, hour, minute, second);
		}

		// Fallback to standard parsing
		if (DateTime.TryParse(dateString, out var result))
		{
			return result;
		}

		return null;
	}
}
