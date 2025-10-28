using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text;
using System.Text.RegularExpressions;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

/// <summary>
/// Chunks XLSX documents by extracting worksheets, tables, rows, formulas, and charts.
/// Uses OpenXML SDK for robust Excel parsing and spreadsheet structure analysis.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="XlsxDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public partial class XlsxDocumentChunker(ITokenCounter tokenCounter, ILogger<XlsxDocumentChunker>? logger = null) : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;
	private SpreadsheetDocument? _document;
	private WorkbookPart? _workbookPart;
	private SharedStringTablePart? _sharedStringTable;

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Xlsx;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		try
		{
			// Save position to restore later
			var originalPosition = documentStream.Position;

			// XLSX files are ZIP archives with a specific signature
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

			// Try to open as XLSX to verify structure
			try
			{
				using var doc = SpreadsheetDocument.Open(documentStream, false);
				return doc.WorkbookPart != null;
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
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;
		_chunks.Clear();

		try
		{
			// Open XLSX document (must be in a memory stream for OpenXML SDK)
			var memoryStream = new MemoryStream();
			await documentStream.CopyToAsync(memoryStream, cancellationToken);
			memoryStream.Position = 0;

			_document = SpreadsheetDocument.Open(memoryStream, false);
			_workbookPart = _document.WorkbookPart;
			_sharedStringTable = _workbookPart?.SharedStringTablePart;

			if (_workbookPart?.Workbook == null)
			{
				throw new InvalidOperationException("Invalid XLSX document: missing workbook part");
			}

			var sheets = _workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList() ?? [];

			logger?.LogInformation("Opened XLSX document with {SheetCount} worksheets", sheets.Count);

			// Extract chunks from each worksheet
			var sheetIndex = 0;
			foreach (var sheet in sheets)
			{
				if (sheet.Id?.Value == null)
				{
					continue;
				}

				var worksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id.Value);
				ExtractWorksheetChunks(worksheetPart, sheet, sheetIndex++);
			}

			// Build hierarchy
			HierarchyBuilder.BuildHierarchy(_chunks);

			logger?.LogInformation("Extracted {ChunkCount} chunks from XLSX document", _chunks.Count);

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
			logger?.LogError(ex, "Error chunking XLSX document");
			return new ChunkingResult
			{
				Chunks = [],
				Statistics = new ChunkingStatistics(),
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk XLSX document: {ex.Message}"
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

	private void ExtractWorksheetChunks(WorksheetPart worksheetPart, Sheet sheet, int sheetIndex)
	{
		var worksheet = worksheetPart.Worksheet;
		if (worksheet == null)
		{
			return;
		}

		var sheetName = sheet.Name?.Value ?? $"Sheet{sheetIndex + 1}";
		var isHidden = sheet.State?.Value == SheetStateValues.Hidden;

		// Get sheet data
		var sheetData = worksheet.GetFirstChild<SheetData>();
		if (sheetData == null)
		{
			return;
		}

		var rows = sheetData.Elements<Row>().ToList();
		if (rows.Count == 0)
		{
			return;
		}

		// Calculate used range
		var firstRow = rows.First();
		var lastRow = rows.Last();
		var firstRowIndex = firstRow.RowIndex?.Value ?? 1;
		var lastRowIndex = lastRow.RowIndex?.Value ?? (uint)rows.Count;
		var usedRange = $"A{firstRowIndex}:{GetLastColumnReference(rows)}{lastRowIndex}";

		// Determine column count
		var maxColumns = rows.SelectMany(r => r.Elements<Cell>()).Select(c => GetColumnIndex(c.CellReference?.Value ?? "A1")).Max();

		// Create worksheet chunk
		var worksheetChunk = new XlsxWorksheetChunk
		{
			SheetName = sheetName,
			SheetIndex = sheetIndex,
			RowCount = rows.Count,
			ColumnCount = maxColumns,
			IsHidden = isHidden,
			UsedRange = usedRange,
			ParentId = null,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "worksheet",
			Metadata = new ChunkMetadata
			{
				DocumentType = "XLSX",
				SourceId = string.Empty,
				Hierarchy = sheetName,
				Tags = ["worksheet", sheetName],
				SheetName = sheetName,
				CreatedAt = DateTimeOffset.UtcNow
			},
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 0, // Worksheet chunks don't have direct content
				CharacterCount = 0,
				WordCount = 0,
				SemanticCompleteness = 1.0
			}
		};

		_chunks.Add(worksheetChunk);

		// Determine if we should treat this as a table
		if (ShouldTreatAsTable(rows))
		{
			ExtractTableChunks(rows, sheetName, worksheetChunk.Id);
		}
		else
		{
			// Extract individual rows
			ExtractRowChunks(rows, sheetName, worksheetChunk.Id);
		}

		// Extract formulas
		ExtractFormulaChunks(rows, sheetName, worksheetChunk.Id);
	}

	private bool ShouldTreatAsTable(List<Row> rows)
	{
		// Heuristic: if first row looks like headers and has reasonable number of rows, treat as table
		if (rows.Count < 2)
		{
			return false;
		}

		var firstRow = rows[0];
		var firstRowCells = firstRow.Elements<Cell>().ToList();

		if (firstRowCells.Count == 0)
		{
			return false;
		}

		// Check if first row has mostly text values (likely headers)
		var textCount = 0;
		foreach (var cell in firstRowCells)
		{
			var value = GetCellValue(cell);
			if (!string.IsNullOrWhiteSpace(value) && !double.TryParse(value, out _))
			{
				textCount++;
			}
		}

		// If more than 60% are text, treat as headers
		return textCount >= firstRowCells.Count * 0.6;
	}

	private void ExtractTableChunks(List<Row> rows, string sheetName, Guid worksheetId)
	{
		if (rows.Count < 2)
		{
			return;
		}

		// Extract headers from first row
		var headerRow = rows[0];
		var headers = new List<string>();
		foreach (var cell in headerRow.Elements<Cell>())
		{
			var value = GetCellValue(cell);
			headers.Add(value ?? string.Empty);
		}

		// Extract data rows
		var tableData = new List<List<string>>();
		foreach (var row in rows.Skip(1))
		{
			var rowData = new List<string>();
			foreach (var cell in row.Elements<Cell>())
			{
				var value = GetCellValue(cell);
				rowData.Add(value ?? string.Empty);
			}

			// Pad row to match header count
			while (rowData.Count < headers.Count)
			{
				rowData.Add(string.Empty);
			}

			tableData.Add(rowData);
		}

		// Serialize table
		var serializedTable = SerializeTableAsMarkdown(headers, tableData);

		// Calculate range
		var firstCellRef = headerRow.Elements<Cell>().FirstOrDefault()?.CellReference?.Value ?? "A1";
		var lastRow = rows.Last();
		var lastCellRef = lastRow.Elements<Cell>().LastOrDefault()?.CellReference?.Value ?? $"A{rows.Count}";
		var range = $"{firstCellRef}:{lastCellRef}";

		var tableChunk = new XlsxTableChunk
		{
			Content = serializedTable,
			SheetName = sheetName,
			Range = range,
			IsFormattedTable = false, // We're detecting data ranges, not formal Excel tables
			ParentId = worksheetId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			SerializedTable = serializedTable,
			SerializationFormat = TableSerializationFormat.Markdown,
			TableInfo = new TableMetadata
			{
				RowCount = tableData.Count,
				ColumnCount = headers.Count,
				Headers = [.. headers],
				HasHeaderRow = true,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			Metadata = new ChunkMetadata
			{
				DocumentType = "XLSX",
				SourceId = string.Empty,
				Hierarchy = $"{sheetName}/table",
				Tags = ["table", sheetName],
				SheetName = sheetName,
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics
		var allText = string.Join(" ", headers) + " " + string.Join(" ", tableData.SelectMany(r => r));
		tableChunk.QualityMetrics = CalculateQualityMetrics(allText);

		_chunks.Add(tableChunk);
	}

	private void ExtractRowChunks(List<Row> rows, string sheetName, Guid worksheetId)
	{
		foreach (var row in rows)
		{
			var rowNumber = (int)(row.RowIndex?.Value ?? 0);
			if (rowNumber == 0)
			{
				continue;
			}

			var cells = new List<XlsxCellData>();
			var rowText = new StringBuilder();

			foreach (var cell in row.Elements<Cell>())
			{
				var cellRef = cell.CellReference?.Value ?? string.Empty;
				var columnName = GetColumnName(cellRef);
				var value = GetCellValue(cell);
				var formula = cell.CellFormula?.Text;
				var cellType = DetermineCellType(cell);

				cells.Add(new XlsxCellData
				{
					CellReference = cellRef,
					ColumnName = columnName,
					Value = value ?? string.Empty,
					CellType = cellType,
					Formula = formula
				});

				if (!string.IsNullOrWhiteSpace(value))
				{
					_ = rowText.Append(value);
					_ = rowText.Append(' ');
				}
			}

			// Skip empty rows
			if (cells.Count == 0 || rowText.Length == 0)
			{
				continue;
			}

			var rowChunk = new XlsxRowChunk
			{
				Content = rowText.ToString().Trim(),
				RowNumber = rowNumber,
				SheetName = sheetName,
				Cells = cells,
				IsHeaderRow = rowNumber == 1,
				IsHidden = row.Hidden?.Value ?? false,
				Height = row.Height?.Value,
				ParentId = worksheetId,
				SequenceNumber = _sequenceNumber++,
				SpecificType = "row",
				Metadata = new ChunkMetadata
				{
					DocumentType = "XLSX",
					SourceId = string.Empty,
					Hierarchy = $"{sheetName}/row{rowNumber}",
					Tags = ["row", sheetName],
					SheetName = sheetName,
					CreatedAt = DateTimeOffset.UtcNow
				}
			};

			// Calculate quality metrics
			rowChunk.QualityMetrics = CalculateQualityMetrics(rowChunk.Content);

			_chunks.Add(rowChunk);
		}
	}

	private void ExtractFormulaChunks(List<Row> rows, string sheetName, Guid worksheetId)
	{
		foreach (var row in rows)
		{
			foreach (var cell in row.Elements<Cell>())
			{
				var formula = cell.CellFormula;
				if (formula == null || string.IsNullOrWhiteSpace(formula.Text))
				{
					continue;
				}

				var cellRef = cell.CellReference?.Value ?? string.Empty;
				var calculatedValue = GetCellValue(cell);
				var formulaText = formula.Text;

				// Extract formula type (SUM, AVERAGE, etc.)
				var formulaType = ExtractFormulaType(formulaText);

				// Extract referenced cells
				var referencedCells = ExtractReferencedCells(formulaText);

				var formulaChunk = new XlsxFormulaChunk
				{
					Content = formulaText,
					CellReference = cellRef,
					SheetName = sheetName,
					Formula = formulaText,
					CalculatedValue = calculatedValue,
					FormulaType = formulaType,
					ReferencedCells = referencedCells,
					IsArrayFormula = false,
					ParentId = worksheetId,
					SequenceNumber = _sequenceNumber++,
					SpecificType = "formula",
					Metadata = new ChunkMetadata
					{
						DocumentType = "XLSX",
						SourceId = string.Empty,
						Hierarchy = $"{sheetName}/formula/{cellRef}",
						Tags = ["formula", formulaType ?? "unknown", sheetName],
						SheetName = sheetName,
						CreatedAt = DateTimeOffset.UtcNow
					},
					// Calculate quality metrics
					QualityMetrics = CalculateQualityMetrics(formulaText)
				};

				_chunks.Add(formulaChunk);
			}
		}
	}

	private string? GetCellValue(Cell cell)
	{
		if (cell.CellValue == null)
		{
			return cell.InnerText;
		}

		var value = cell.CellValue.Text;

		// Check if this is a shared string
		if (cell.DataType?.Value == CellValues.SharedString)
		{
			if (_sharedStringTable != null && int.TryParse(value, out var index))
			{
				var sharedStringItem = _sharedStringTable.SharedStringTable.Elements<SharedStringItem>().ElementAtOrDefault(index);
				if (sharedStringItem != null)
				{
					return sharedStringItem.InnerText;
				}
			}
		}

		return value;
	}

	private static XlsxCellType DetermineCellType(Cell cell)
	{
		if (cell.CellFormula != null)
		{
			return XlsxCellType.Formula;
		}

		if (cell.DataType?.Value == CellValues.SharedString || cell.DataType?.Value == CellValues.InlineString)
		{
			return XlsxCellType.Text;
		}

		if (cell.DataType?.Value == CellValues.Boolean)
		{
			return XlsxCellType.Boolean;
		}

		if (cell.DataType?.Value == CellValues.Error)
		{
			return XlsxCellType.Error;
		}

		if (cell.DataType?.Value == CellValues.Date)
		{
			return XlsxCellType.Date;
		}

		if (cell.DataType?.Value == CellValues.Number || cell.CellValue != null)
		{
			return XlsxCellType.Number;
		}

		return XlsxCellType.Empty;
	}

	private static string GetColumnName(string cellReference)
	{
		if (string.IsNullOrEmpty(cellReference))
		{
			return "A";
		}

		return ColumnNameRegex().Match(cellReference).Value;
	}

	private static int GetColumnIndex(string cellReference)
	{
		var columnName = GetColumnName(cellReference);
		var index = 0;

		for (var i = 0; i < columnName.Length; i++)
		{
			index *= 26;
			index += columnName[i] - 'A' + 1;
		}

		return index;
	}

	private static string GetLastColumnReference(List<Row> rows)
	{
		var maxColumnIndex = 0;

		foreach (var row in rows)
		{
			foreach (var cell in row.Elements<Cell>())
			{
				var cellRef = cell.CellReference?.Value;
				if (!string.IsNullOrEmpty(cellRef))
				{
					var columnIndex = GetColumnIndex(cellRef);
					if (columnIndex > maxColumnIndex)
					{
						maxColumnIndex = columnIndex;
					}
				}
			}
		}

		return GetColumnNameFromIndex(maxColumnIndex);
	}

	private static string GetColumnNameFromIndex(int index)
	{
		var columnName = string.Empty;

		while (index > 0)
		{
			var modulo = (index - 1) % 26;
			columnName = Convert.ToChar('A' + modulo) + columnName;
			index = (index - modulo) / 26;
		}

		return string.IsNullOrEmpty(columnName) ? "A" : columnName;
	}

	private static string? ExtractFormulaType(string formula)
	{
		var match = FormulaTypeRegex().Match(formula);
		return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
	}

	private static List<string> ExtractReferencedCells(string formula)
	{
		var references = new List<string>();
		var matches = CellReferenceRegex().Matches(formula);

		foreach (Match match in matches)
		{
			references.Add(match.Value);
		}

		return references;
	}

	private static string SerializeTableAsMarkdown(List<string> headers, List<List<string>> rows)
	{
		var sb = new StringBuilder();

		if (headers.Count > 0)
		{
			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", headers);
			_ = sb.AppendLine(" |");

			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", headers.Select(_ => "---"));
			_ = sb.AppendLine(" |");
		}

		foreach (var row in rows)
		{
			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", row);
			_ = sb.AppendLine(" |");
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

	[GeneratedRegex(@"[A-Z]+", RegexOptions.None)]
	private static partial Regex ColumnNameRegex();

	[GeneratedRegex(@"^=?([A-Z]+)\(", RegexOptions.None)]
	private static partial Regex FormulaTypeRegex();

	[GeneratedRegex(@"[A-Z]+\d+(?::[A-Z]+\d+)?", RegexOptions.None)]
	private static partial Regex CellReferenceRegex();
}
