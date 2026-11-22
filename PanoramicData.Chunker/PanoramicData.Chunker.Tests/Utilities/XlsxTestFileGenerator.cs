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
	private static readonly object _generationLock = new();

	public static void GenerateAllTestFiles()
	{
		lock (_generationLock)
		{
			GenerateAllTestFilesCore();
		}
	}

	private static void GenerateAllTestFilesCore()
	{
		_ = Directory.CreateDirectory(_testDataPath);

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

	private static void CreateSimpleSpreadsheet()
		=> CreateSingleSheetWorkbook("simple.xlsx", "Sheet1",
			CreateValueRow(1, "Name", "Age", "City"),
			CreateValueRow(2, "Alice", "30", "New York"),
			CreateValueRow(3, "Bob", "25", "London"),
			CreateValueRow(4, "Charlie", "35", "Tokyo"));

	private static void CreateEmptySpreadsheet()
		=> CreateSingleSheetWorkbook("empty.xlsx", "Sheet1");

	private static void CreateMultipleWorksheets()
		=> CreateWorkbook("multiple-sheets.xlsx", (workbookPart, sheets) =>
		{
			AddWorksheet(workbookPart, sheets, 1, "Sales",
				CreateValueRow(1, "Product", "Quantity", "Price"),
				CreateValueRow(2, "Widget A", "100", "9.99"));

			AddWorksheet(workbookPart, sheets, 2, "Expenses",
				CreateValueRow(1, "Category", "Amount"),
				CreateValueRow(2, "Office Supplies", "250.00"));

			AddWorksheet(workbookPart, sheets, 3, "Summary",
				CreateValueRow(1, "Total Revenue", "999.00"));
		});

	private static void CreateWithFormulas()
		=> CreateSingleSheetWorkbook("with-formulas.xlsx", "Calculations",
			CreateValueRow(1, "Item", "Price", "Quantity", "Total"),
			CreateRow(2, CreateCell("A2", "Item 1"), CreateCell("B2", "10.50"), CreateCell("C2", "5"), CreateFormulaCell("D2", "B2*C2")),
			CreateRow(3, CreateCell("A3", "Item 2"), CreateCell("B3", "20.00"), CreateCell("C3", "3"), CreateFormulaCell("D3", "B3*C3")),
			CreateRow(4, CreateCell("A4", "Total"), CreateCell("B4", ""), CreateCell("C4", ""), CreateFormulaCell("D4", "SUM(D2:D3)")));

	private static void CreateWithTable()
		=> CreateSingleSheetWorkbook("with-table.xlsx", "TableData",
			[
				CreateValueRow(1, "Employee", "Department", "Salary"),
				.. Enumerable.Range(2, 5).Select(i => CreateValueRow(
					(uint)i,
					$"Employee {i - 1}",
					i % 2 == 0 ? "Sales" : "Engineering",
					(50000 + (i * 5000)).ToString()))
			]);

	private static void CreateLargeSpreadsheet()
		=> CreateSingleSheetWorkbook("large.xlsx", "LargeData",
			[
				CreateValueRow(1, "ID", "Name", "Value", "Date", "Status"),
				.. Enumerable.Range(2, 1000).Select(i => CreateValueRow(
					(uint)i,
					i.ToString(),
					$"Item {i - 1}",
					(i * 10.5).ToString("F2"),
					$"2025-01-{(i % 28) + 1:D2}",
					i % 3 == 0 ? "Active" : "Inactive"))
			]);

	private static void CreateWithMergedCells()
		=> CreateWorkbook("with-merged-cells.xlsx", (workbookPart, sheets) =>
		{
			var worksheetPart = AddWorksheet(workbookPart, sheets, 1, "MergedCells",
				CreateValueRow(1, "Quarterly Sales Report"),
				CreateValueRow(2, "Quarter", "Sales", "Expenses", "Profit"),
				CreateValueRow(3, "Q1", "100000", "60000", "40000"));

			var mergeCells = new MergeCells();
			mergeCells.Append(new MergeCell { Reference = "A1:D1" });
			worksheetPart.Worksheet!.Append(mergeCells);
		});

	private static void CreateWithDataTypes()
		=> CreateSingleSheetWorkbook("with-data-types.xlsx", "DataTypes",
			CreateValueRow(1, "Text", "Number", "Date", "Boolean", "Currency"),
			CreateValueRow(2, "Sample Text", "123.45", "2025-01-23", "TRUE", "$1,234.56"));

	private static void CreateSingleSheetWorkbook(string fileName, string sheetName, params Row[] rows)
		=> CreateWorkbook(fileName, (workbookPart, sheets) => AddWorksheet(workbookPart, sheets, 1, sheetName, rows));

	private static void CreateWorkbook(string fileName, Action<WorkbookPart, Sheets> build)
	{
		using var document = SpreadsheetDocument.Create(Path.Combine(_testDataPath, fileName), SpreadsheetDocumentType.Workbook);

		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();
		build(workbookPart, workbookPart.Workbook.AppendChild(new Sheets()));

		workbookPart.Workbook.Save();
	}

	private static WorksheetPart AddWorksheet(WorkbookPart workbookPart, Sheets sheets, uint sheetId, string name, params Row[] rows)
	{
		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet(new SheetData());
		worksheetPart.Worksheet.GetFirstChild<SheetData>()!.Append(rows);

		sheets.Append(new Sheet
		{
			Id = workbookPart.GetIdOfPart(worksheetPart),
			SheetId = sheetId,
			Name = name
		});

		return worksheetPart;
	}

	private static Row CreateRow(uint index, params Cell[] cells)
	{
		var row = new Row { RowIndex = index };
		row.Append(cells);
		return row;
	}

	private static Row CreateValueRow(uint index, params string[] values)
		=> CreateRow(index, [.. values.Select((value, column) => CreateCell($"{(char)('A' + column)}{index}", value))]);

	private static Cell CreateCell(string reference, string value) => new()
	{
		CellReference = reference,
		DataType = CellValues.InlineString,
		InlineString = new InlineString(new Text(value))
	};

	private static Cell CreateFormulaCell(string reference, string formula) => new()
	{
		CellReference = reference,
		CellFormula = new CellFormula(formula)
	};
}
