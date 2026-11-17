using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
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
			var originalPosition = documentStream.Position;

			// XLSX files are ZIP archives with a specific signature
			var buffer = new byte[4];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			documentStream.Position = originalPosition;

			return bytesRead == 4 && HasZipSignature(buffer) && CanOpenWorkbook(documentStream);
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

			var sheets = OpenWorkbook(memoryStream);
			logger?.LogInformation("Opened XLSX document with {SheetCount} worksheets", sheets.Count);

			ExtractSheets(sheets);
			HierarchyBuilder.BuildHierarchy(_chunks);
			logger?.LogInformation("Extracted {ChunkCount} chunks from XLSX document", _chunks.Count);

			return ChunkResultHelper.CreateSuccess(_chunks, startTime, options);
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking XLSX document");
			return ChunkResultHelper.CreateFailure("XLSX", ex);
		}
		finally
		{
			_document?.Dispose();
		}
	}

	private static bool HasZipSignature(byte[] buffer)
		=> buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04;

	private static bool CanOpenWorkbook(Stream documentStream)
	{
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

	private List<Sheet> OpenWorkbook(MemoryStream memoryStream)
	{
		_document = SpreadsheetDocument.Open(memoryStream, false);
		_workbookPart = _document.WorkbookPart;
		_sharedStringTable = _workbookPart?.SharedStringTablePart;

		return _workbookPart?.Workbook == null
			? throw new InvalidOperationException("Invalid XLSX document: missing workbook part")
			: _workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList() ?? [];
	}

	private void ExtractSheets(List<Sheet> sheets)
	{
		var sheetIndex = 0;
		foreach (var sheet in sheets.Where(s => s.Id?.Value != null))
		{
			var worksheetPart = (WorksheetPart)_workbookPart!.GetPartById(sheet.Id!.Value!);
			ExtractWorksheetChunks(worksheetPart, sheet, sheetIndex++);
		}
	}
}
