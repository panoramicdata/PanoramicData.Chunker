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
	public Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
		=> OpenXmlStreamSupport.IsZipThatOpensAsync(documentStream, CanOpenWorkbook, cancellationToken);

	/// <inheritdoc/>
	public Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken)
		=> ChunkRunner.RunAsync("XLSX", documentStream, options, _chunks, logger, () => ExtractAsync(documentStream, cancellationToken));

	private async Task ExtractAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		_sequenceNumber = 0;

		try
		{
			var memoryStream = await OpenXmlStreamSupport.CopyToSeekableAsync(documentStream, cancellationToken);

			var sheets = OpenWorkbook(memoryStream);
			logger?.LogInformation("Opened XLSX document with {SheetCount} worksheets", sheets.Count);

			ExtractSheets(sheets);
		}
		finally
		{
			_document?.Dispose();
		}
	}

	private static bool CanOpenWorkbook(Stream documentStream)
		=> OpenXmlStreamSupport.Opens(documentStream, stream => SpreadsheetDocument.Open(stream, false), doc => doc.WorkbookPart != null);

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
