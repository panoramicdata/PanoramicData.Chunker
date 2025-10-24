using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Utility class to generate test XLSX files for integration testing.
/// </summary>
public static class XlsxTestFileGenerator
{
	private static readonly string _testDataPath = Path.Combine("TestData", "Xlsx");

	/// <summary>
	/// Generate all test XLSX files.
	/// </summary>
	public static void GenerateAllTestFiles()
	{
		Directory.CreateDirectory(_testDataPath);

		// If files already exist, skip generation
		if (File.Exists(Path.Combine(_testDataPath, "simple.xlsx")))
		{
			return;
		}

		CreateSimpleSpreadsheet();
		CreateEmptySpreadsheet();
		CreateMultipleWorksheets();
		CreateWithFormulas();
		CreateWithTable();
		CreateLargeSpreadsheet();
		CreateWithMergedCells();
		CreateWithDataTypes();
	}

	/// <summary>
	/// Create a simple spreadsheet with basic data.
	/// </summary>
	private static void CreateSimpleSpreadsheet()
	{
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		// Add WorkbookPart
		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		// Add WorksheetPart
		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());

		// Add Sheets to the Workbook
		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1,
			Name = "Sheet1"
		};
		sheets.Append(sheet);

		// Get the SheetData
		var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

		// Add header row
		var headerRow = new Row { RowIndex = 1 };
		headerRow.Append(
			CreateCell("A1", "Name"),
			CreateCell("B1", "Age"),
			CreateCell("C1", "City")
		);
		sheetData.Append(headerRow);

		// Add data rows
		var row2 = new Row { RowIndex = 2 };
		row2.Append(
			CreateCell("A2", "Alice"),
			CreateCell("B2", "30"),
			CreateCell("C2", "New York")
		);
		sheetData.Append(row2);

		var row3 = new Row { RowIndex = 3 };
		row3.Append(
			CreateCell("A3", "Bob"),
			CreateCell("B3", "25"),
			CreateCell("C3", "London")
		);
		sheetData.Append(row3);

		var row4 = new Row { RowIndex = 4 };
		row4.Append(
			CreateCell("A4", "Charlie"),
			CreateCell("B4", "35"),
			CreateCell("C4", "Tokyo")
		);
		sheetData.Append(row4);

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create an empty spreadsheet.
	/// </summary>
	private static void CreateEmptySpreadsheet()
	{
		var filePath = Path.Combine(_testDataPath, "empty.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1,
			Name = "Sheet1"
		};
		sheets.Append(sheet);

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create a spreadsheet with multiple worksheets.
	/// </summary>
	private static void CreateMultipleWorksheets()
	{
		var filePath = Path.Combine(_testDataPath, "multiple-sheets.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();
		var sheets = workbookPart.Workbook.AppendChild(new Sheets());

		// Sheet 1: Sales Data
		var worksheetPart1 = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart1.Worksheet = new Worksheet(new SheetData());
		var sheetData1 = worksheetPart1.Worksheet.GetFirstChild<SheetData>()!;

		var header1 = new Row { RowIndex = 1 };
		header1.Append(
			CreateCell("A1", "Product"),
			CreateCell("B1", "Quantity"),
			CreateCell("C1", "Price")
		);
		sheetData1.Append(header1);

		var data1 = new Row { RowIndex = 2 };
		data1.Append(
			CreateCell("A2", "Widget A"),
			CreateCell("B2", "100"),
			CreateCell("C2", "9.99")
		);
		sheetData1.Append(data1);

		var sheet1 = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart1),
			SheetId = 1,
			Name = "Sales"
		};
		sheets.Append(sheet1);

		// Sheet 2: Expenses
		var worksheetPart2 = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart2.Worksheet = new Worksheet(new SheetData());
		var sheetData2 = worksheetPart2.Worksheet.GetFirstChild<SheetData>()!;

		var header2 = new Row { RowIndex = 1 };
		header2.Append(
			CreateCell("A1", "Category"),
			CreateCell("B1", "Amount")
		);
		sheetData2.Append(header2);

		var data2 = new Row { RowIndex = 2 };
		data2.Append(
			CreateCell("A2", "Office Supplies"),
			CreateCell("B2", "250.00")
		);
		sheetData2.Append(data2);

		var sheet2 = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart2),
			SheetId = 2,
			Name = "Expenses"
		};
		sheets.Append(sheet2);

		// Sheet 3: Summary
		var worksheetPart3 = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart3.Worksheet = new Worksheet(new SheetData());
		var sheetData3 = worksheetPart3.Worksheet.GetFirstChild<SheetData>()!;

		var summaryRow = new Row { RowIndex = 1 };
		summaryRow.Append(
			CreateCell("A1", "Total Revenue"),
			CreateCell("B1", "999.00")
		);
		sheetData3.Append(summaryRow);

		var sheet3 = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart3),
			SheetId = 3,
			Name = "Summary"
		};
		sheets.Append(sheet3);

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create a spreadsheet with formulas.
	/// </summary>
	private static void CreateWithFormulas()
	{
		var filePath = Path.Combine(_testDataPath, "with-formulas.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1,
			Name = "Calculations"
		};
		sheets.Append(sheet);

		var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

		// Header
		var header = new Row { RowIndex = 1 };
		header.Append(
			CreateCell("A1", "Item"),
			CreateCell("B1", "Price"),
			CreateCell("C1", "Quantity"),
			CreateCell("D1", "Total")
		);
		sheetData.Append(header);

		// Data rows with formula in column D
		var row2 = new Row { RowIndex = 2 };
		row2.Append(
			CreateCell("A2", "Item 1"),
			CreateCell("B2", "10.50"),
			CreateCell("C2", "5"),
			CreateFormulaCell("D2", "B2*C2")
		);
		sheetData.Append(row2);

		var row3 = new Row { RowIndex = 3 };
		row3.Append(
			CreateCell("A3", "Item 2"),
			CreateCell("B3", "20.00"),
			CreateCell("C3", "3"),
			CreateFormulaCell("D3", "B3*C3")
		);
		sheetData.Append(row3);

		// Total row
		var totalRow = new Row { RowIndex = 4 };
		totalRow.Append(
			CreateCell("A4", "Total"),
			CreateCell("B4", ""),
			CreateCell("C4", ""),
			CreateFormulaCell("D4", "SUM(D2:D3)")
		);
		sheetData.Append(totalRow);

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create a spreadsheet with an Excel table.
	/// </summary>
	private static void CreateWithTable()
	{
		var filePath = Path.Combine(_testDataPath, "with-table.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1,
			Name = "TableData"
		};
		sheets.Append(sheet);

		var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

		// Add table data
		var header = new Row { RowIndex = 1 };
		header.Append(
			CreateCell("A1", "Employee"),
			CreateCell("B1", "Department"),
			CreateCell("C1", "Salary")
		);
		sheetData.Append(header);

		for (var i = 2; i <= 6; i++)
		{
			var row = new Row { RowIndex = (uint)i };
			row.Append(
				CreateCell($"A{i}", $"Employee {i - 1}"),
				CreateCell($"B{i}", i % 2 == 0 ? "Sales" : "Engineering"),
				CreateCell($"C{i}", (50000 + (i * 5000)).ToString())
			);
			sheetData.Append(row);
		}

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create a large spreadsheet for performance testing.
	/// </summary>
	private static void CreateLargeSpreadsheet()
	{
		var filePath = Path.Combine(_testDataPath, "large.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1,
			Name = "LargeData"
		};
		sheets.Append(sheet);

		var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

		// Header
		var header = new Row { RowIndex = 1 };
		header.Append(
			CreateCell("A1", "ID"),
			CreateCell("B1", "Name"),
			CreateCell("C1", "Value"),
			CreateCell("D1", "Date"),
			CreateCell("E1", "Status")
		);
		sheetData.Append(header);

		// Generate 1000 rows
		for (var i = 2; i <= 1001; i++)
		{
			var row = new Row { RowIndex = (uint)i };
			row.Append(
				CreateCell($"A{i}", i.ToString()),
				CreateCell($"B{i}", $"Item {i - 1}"),
				CreateCell($"C{i}", (i * 10.5).ToString("F2")),
				CreateCell($"D{i}", $"2025-01-{(i % 28) + 1:D2}"),
				CreateCell($"E{i}", i % 3 == 0 ? "Active" : "Inactive")
			);
			sheetData.Append(row);
		}

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create a spreadsheet with merged cells.
	/// </summary>
	private static void CreateWithMergedCells()
	{
		var filePath = Path.Combine(_testDataPath, "with-merged-cells.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1,
			Name = "MergedCells"
		};
		sheets.Append(sheet);

		var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

		// Title row (merged across columns)
		var titleRow = new Row { RowIndex = 1 };
		titleRow.Append(CreateCell("A1", "Quarterly Sales Report"));
		sheetData.Append(titleRow);

		// Add merge cells
		var mergeCells = new MergeCells();
		mergeCells.Append(new MergeCell { Reference = "A1:D1" });
		worksheetPart.Worksheet.Append(mergeCells);

		// Header row
		var header = new Row { RowIndex = 2 };
		header.Append(
			CreateCell("A2", "Quarter"),
			CreateCell("B2", "Sales"),
			CreateCell("C2", "Expenses"),
			CreateCell("D2", "Profit")
		);
		sheetData.Append(header);

		// Data rows
		var row3 = new Row { RowIndex = 3 };
		row3.Append(
			CreateCell("A3", "Q1"),
			CreateCell("B3", "100000"),
			CreateCell("C3", "60000"),
			CreateCell("D3", "40000")
		);
		sheetData.Append(row3);

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create a spreadsheet with different data types.
	/// </summary>
	private static void CreateWithDataTypes()
	{
		var filePath = Path.Combine(_testDataPath, "with-data-types.xlsx");

		using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = 1,
			Name = "DataTypes"
		};
		sheets.Append(sheet);

		var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

		// Header
		var header = new Row { RowIndex = 1 };
		header.Append(
			CreateCell("A1", "Text"),
			CreateCell("B1", "Number"),
			CreateCell("C1", "Date"),
			CreateCell("D1", "Boolean"),
			CreateCell("E1", "Currency")
		);
		sheetData.Append(header);

		// Data row
		var row2 = new Row { RowIndex = 2 };
		row2.Append(
			CreateCell("A2", "Sample Text"),
			CreateCell("B2", "123.45"),
			CreateCell("C2", "2025-01-23"),
			CreateCell("D2", "TRUE"),
			CreateCell("E2", "$1,234.56")
		);
		sheetData.Append(row2);

		workbookPart.Workbook.Save();
	}

	/// <summary>
	/// Create a cell with text value.
	/// </summary>
	private static Cell CreateCell(string reference, string value) => new()
	{
		CellReference = reference,
		DataType = CellValues.InlineString,
		InlineString = new InlineString(new Text(value))
	};

	/// <summary>
	/// Create a cell with a formula.
	/// </summary>
	private static Cell CreateFormulaCell(string reference, string formula) => new()
	{
		CellReference = reference,
		CellFormula = new CellFormula(formula)
	};
}
