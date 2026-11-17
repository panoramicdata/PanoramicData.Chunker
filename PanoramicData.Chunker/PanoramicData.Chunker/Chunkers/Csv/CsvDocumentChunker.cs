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
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		try
		{
			var originalPosition = documentStream.Position;

			// Read the first few lines to check whether it looks like CSV
			using var reader = new StreamReader(documentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

			var linesRead = 0;
			var hasDelimiters = false;

			while (linesRead < 10 && await reader.ReadLineAsync(cancellationToken) is { } line)
			{
				if (!string.IsNullOrWhiteSpace(line) && ContainsDelimiter(line))
				{
					hasDelimiters = true;
					break;
				}

				linesRead++;
			}

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
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;
		_chunks.Clear();

		try
		{
			using var reader = new StreamReader(documentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			var content = await reader.ReadToEndAsync(cancellationToken);

			var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
				.Where(l => !string.IsNullOrWhiteSpace(l))
				.ToList();

			if (lines.Count == 0)
			{
				logger?.LogInformation("CSV document has no content");
				return CreateEmptyResult(startTime);
			}

			var delimiter = DetectDelimiter(lines);
			logger?.LogInformation("Detected CSV delimiter: '{Delimiter}'", delimiter);

			var headers = ParseCsvLine(lines[0], delimiter);
			var hasHeader = DetectHeaderRow(headers);
			logger?.LogInformation("CSV has {ColumnCount} columns, header detected: {HasHeader}", headers.Count, hasHeader);

			var documentChunk = CreateDocumentChunk(delimiter, lines.Count, headers, hasHeader, reader.CurrentEncoding.WebName, content.Length);
			_chunks.Add(documentChunk);

			var dataRows = AddRowChunks(hasHeader ? lines.Skip(1) : lines, headers, delimiter, documentChunk.Id, hasHeader);

			HierarchyBuilder.BuildHierarchy(_chunks);
			logger?.LogInformation("Extracted {ChunkCount} chunks from CSV ({DataRows} data rows)", _chunks.Count, dataRows);

			return ChunkResultHelper.CreateSuccess(_chunks, startTime, options);
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking CSV document");
			return ChunkResultHelper.CreateFailure("CSV", ex);
		}
	}

	private int AddRowChunks(IEnumerable<string> dataLines, List<string> headers, char delimiter, Guid documentId, bool hasHeader)
	{
		var rowNumber = 0;

		foreach (var line in dataLines)
		{
			rowNumber++;
			var fields = ParseCsvLine(line, delimiter);

			// Rows that do not match the header's column count are skipped
			if (hasHeader && fields.Count != headers.Count)
			{
				logger?.LogWarning("Skipping row {RowNumber} with {FieldCount} fields (expected {ColumnCount})", rowNumber, fields.Count, headers.Count);
				continue;
			}

			_chunks.Add(CreateRowChunk(rowNumber, line, fields, headers, delimiter, documentId, hasHeader));
		}

		return rowNumber;
	}

	private CsvDocumentChunk CreateDocumentChunk(char delimiter, int totalRows, List<string> headers, bool hasHeader, string encoding, int contentLength) => new()
	{
		Delimiter = delimiter,
		TotalRows = totalRows,
		ColumnCount = headers.Count,
		HasHeaderRow = hasHeader,
		Headers = hasHeader ? headers : [],
		Encoding = encoding,
		ParentId = null,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "csv-document",
		Metadata = CreateMetadata("csv", "csv", "tabular"),
		QualityMetrics = new ChunkQualityMetrics
		{
			TokenCount = 0,
			CharacterCount = contentLength,
			WordCount = 0,
			SemanticCompleteness = 1.0
		}
	};

	private CsvRowChunk CreateRowChunk(
		int rowNumber,
		string rawRow,
		List<string> fields,
		List<string> headers,
		char delimiter,
		Guid documentId,
		bool hasHeader)
	{
		// With a header, each field becomes a "name: value" pair for context
		var content = hasHeader && headers.Count > 0
			? string.Join(", ", headers.Zip(fields, (header, field) => $"{header}: {field}"))
			: string.Join(", ", fields);

		return new CsvRowChunk
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
			SerializedTable = SerializeRowAsMarkdown(headers, fields, hasHeader),
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
			Metadata = CreateMetadata($"csv/row{rowNumber}", "csv-row", "tabular"),
			QualityMetrics = CalculateQualityMetrics(content)
		};
	}

	private static ChunkMetadata CreateMetadata(string hierarchy, params string[] tags) => new()
	{
		DocumentType = "CSV",
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		CreatedAt = DateTimeOffset.UtcNow
	};

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = text.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Length,
		SemanticCompleteness = 1.0
	};

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
