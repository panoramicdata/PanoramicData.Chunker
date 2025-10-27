using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Csv;

/// <summary>
/// Chunks CSV documents by detecting delimiters, parsing rows, and preserving header context.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CsvDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public partial class CsvDocumentChunker(ITokenCounter tokenCounter, ILogger<CsvDocumentChunker>? logger = null) : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Csv;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
	{
		try
		{
			// Save position to restore later
			var originalPosition = documentStream.Position;

			// Read first few lines to check if it looks like CSV
			using var reader = new StreamReader(documentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

			var linesRead = 0;
			var hasDelimiters = false;

			while (linesRead < 10 && !reader.EndOfStream)
			{
				var line = await reader.ReadLineAsync(cancellationToken);
				if (string.IsNullOrWhiteSpace(line))
				{
					linesRead++;
					continue;
				}

				// Check for common delimiters
				if (line.Contains(',') || line.Contains('\t') || line.Contains(';') || line.Contains('|'))
				{
					hasDelimiters = true;
					break;
				}

				linesRead++;
			}

			// Restore position
			documentStream.Position = originalPosition;

			return hasDelimiters;
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
			// Read all content
			using var reader = new StreamReader(documentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			var content = await reader.ReadToEndAsync(cancellationToken);

			if (string.IsNullOrWhiteSpace(content))
			{
				logger?.LogInformation("Empty CSV document");
				return CreateEmptyResult(startTime);
			}

			// Split into lines
			var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
				.Where(l => !string.IsNullOrWhiteSpace(l))
				.ToList();

			if (lines.Count == 0)
			{
				logger?.LogInformation("CSV document has no valid lines");
				return CreateEmptyResult(startTime);
			}

			// Detect delimiter
			var delimiter = DetectDelimiter(lines);
			logger?.LogInformation("Detected CSV delimiter: '{Delimiter}'", delimiter);

			// Parse header
			var headers = ParseCsvLine(lines[0], delimiter);
			var hasHeader = DetectHeaderRow(headers);

			logger?.LogInformation("CSV has {ColumnCount} columns, header detected: {HasHeader}",
				headers.Count, hasHeader);

			// Create document chunk
			var documentChunk = new CsvDocumentChunk
			{
				Delimiter = delimiter,
				TotalRows = lines.Count,
				ColumnCount = headers.Count,
				HasHeaderRow = hasHeader,
				Headers = hasHeader ? headers : [],
				Encoding = reader.CurrentEncoding.WebName,
				ParentId = null,
				SequenceNumber = _sequenceNumber++,
				SpecificType = "csv-document",
				Metadata = new ChunkMetadata
				{
					DocumentType = "CSV",
					SourceId = string.Empty,
					Hierarchy = "csv",
					Tags = ["csv", "tabular"],
					CreatedAt = DateTimeOffset.UtcNow
				},
				QualityMetrics = new ChunkQualityMetrics
				{
					TokenCount = 0,
					CharacterCount = content.Length,
					WordCount = 0,
					SemanticCompleteness = 1.0
				}
			};

			_chunks.Add(documentChunk);

			// Process data rows
			var dataLines = hasHeader ? lines.Skip(1) : lines;
			var rowNumber = 1;

			foreach (var line in dataLines)
			{
				var fields = ParseCsvLine(line, delimiter);

				// Skip rows that don't match column count
				if (fields.Count != headers.Count && hasHeader)
				{
					logger?.LogWarning("Skipping row {RowNumber} with {FieldCount} fields (expected {ColumnCount})",
						rowNumber, fields.Count, headers.Count);
					rowNumber++;
					continue;
				}

				var rowChunk = CreateRowChunk(rowNumber, line, fields, headers, delimiter, documentChunk.Id, hasHeader);
				_chunks.Add(rowChunk);
				rowNumber++;
			}

			// Build hierarchy
			HierarchyBuilder.BuildHierarchy(_chunks);

			logger?.LogInformation("Extracted {ChunkCount} chunks from CSV ({DataRows} data rows)",
				_chunks.Count, rowNumber - 1);

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
			logger?.LogError(ex, "Error chunking CSV document");
			return new ChunkingResult
			{
				Chunks = [],
				Statistics = new ChunkingStatistics(),
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk CSV document: {ex.Message}"
					}
				],
				Success = false
			};
		}
	}

	private CsvRowChunk CreateRowChunk(
		int rowNumber,
		string rawRow,
		List<string> fields,
		List<string> headers,
		char delimiter,
		Guid documentId,
		bool hasHeader)
	{
		// Build content with header context if available
		var contentBuilder = new StringBuilder();

		if (hasHeader && headers.Count > 0)
		{
			// Create key-value pairs for better context
			for (var i = 0; i < Math.Min(fields.Count, headers.Count); i++)
			{
				_ = contentBuilder.Append($"{headers[i]}: {fields[i]}");
				if (i < Math.Min(fields.Count, headers.Count) - 1)
				{
					_ = contentBuilder.Append(", ");
				}
			}
		}
		else
		{
			// Just join fields with commas
			_ = contentBuilder.AppendJoin(", ", fields);
		}

		var content = contentBuilder.ToString();

		// Build serialized table (single row with header)
		var serializedTable = SerializeRowAsMarkdown(headers, fields, hasHeader);

		var chunk = new CsvRowChunk
		{
			Content = content,
			RowNumber = rowNumber,
			RawRow = rawRow,
			Fields = fields,
			HeaderNames = hasHeader ? headers : [],
			HasQuotedFields = rawRow.Contains('"'),
			Delimiter = delimiter,
			ParentId = documentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "csv-row",
			SerializedTable = serializedTable,
			SerializationFormat = TableSerializationFormat.Markdown,
			TableInfo = new TableMetadata
			{
				RowCount = 1,
				ColumnCount = fields.Count,
				Headers = hasHeader ? [.. headers] : [],
				HasHeaderRow = hasHeader,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			Metadata = new ChunkMetadata
			{
				DocumentType = "CSV",
				SourceId = string.Empty,
				Hierarchy = $"csv/row{rowNumber}",
				Tags = ["csv-row", "tabular"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			// Calculate quality metrics
			QualityMetrics = CalculateQualityMetrics(content)
		};

		return chunk;
	}

	private static char DetectDelimiter(List<string> lines)
	{
		// Try common delimiters and count occurrences in first few lines
		var delimiters = new[] { ',', '\t', ';', '|' };
		var scores = new Dictionary<char, int>();

		var sampleLines = lines.Take(Math.Min(5, lines.Count)).ToList();

		foreach (var delimiter in delimiters)
		{
			var counts = sampleLines.Select(line => CountDelimiter(line, delimiter)).ToList();

			// Check if counts are consistent (same number of delimiters per line)
			if (counts.Count > 0 && counts.Distinct().Count() == 1 && counts[0] > 0)
			{
				scores[delimiter] = counts[0] * 100; // Bonus for consistency
			}
			else if (counts.Count > 0)
			{
				scores[delimiter] = counts.Max();
			}
		}

		// Return delimiter with highest score, default to comma
		return scores.Count > 0 ? scores.OrderByDescending(kvp => kvp.Value).First().Key : ',';
	}

	private static int CountDelimiter(string line, char delimiter)
	{
		var count = 0;
		var inQuotes = false;

		foreach (var ch in line)
		{
			if (ch == '"')
			{
				inQuotes = !inQuotes;
			}
			else if (ch == delimiter && !inQuotes)
			{
				count++;
			}
		}

		return count;
	}

	private static bool DetectHeaderRow(List<string> fields)
	{
		// Heuristic: if most fields are non-numeric, likely a header
		var nonNumericCount = 0;

		foreach (var field in fields)
		{
			if (!string.IsNullOrWhiteSpace(field) && !IsNumeric(field))
			{
				nonNumericCount++;
			}
		}

		// If more than 70% are non-numeric, treat as header
		return fields.Count > 0 && (double)nonNumericCount / fields.Count >= 0.7;
	}

	private static bool IsNumeric(string value) => double.TryParse(value, out _) ||
			   DateTime.TryParse(value, out _) ||
			   value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
		 value.Equals("false", StringComparison.OrdinalIgnoreCase);

	private static List<string> ParseCsvLine(string line, char delimiter)
	{
		var fields = new List<string>();
		var currentField = new StringBuilder();
		var inQuotes = false;

		for (var i = 0; i < line.Length; i++)
		{
			var ch = line[i];

			if (ch == '"')
			{
				if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
				{
					// Escaped quote
					_ = currentField.Append('"');
					i++; // Skip next quote
				}
				else
				{
					// Toggle quotes
					inQuotes = !inQuotes;
				}
			}
			else if (ch == delimiter && !inQuotes)
			{
				// End of field
				fields.Add(currentField.ToString().Trim());
				_ = currentField.Clear();
			}
			else
			{
				_ = currentField.Append(ch);
			}
		}

		// Add last field
		fields.Add(currentField.ToString().Trim());

		return fields;
	}

	private static string SerializeRowAsMarkdown(List<string> headers, List<string> fields, bool hasHeader)
	{
		var sb = new StringBuilder();

		if (hasHeader && headers.Count > 0)
		{
			// Header row
			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", headers);
			_ = sb.AppendLine(" |");

			// Separator
			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", headers.Select(_ => "---"));
			_ = sb.AppendLine(" |");
		}

		// Data row
		_ = sb.Append("| ");
		_ = sb.AppendJoin(" | ", fields);
		_ = sb.AppendLine(" |");

		return sb.ToString();
	}

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = text.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Length,
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
}
