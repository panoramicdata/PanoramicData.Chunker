using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Utility class to generate test PDF files for integration testing.
/// </summary>
public static class PdfTestFileGenerator
{
	private static readonly string _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");

	/// <summary>
	/// Generate all test PDF files.
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
		// Configure QuestPDF license (community license)
		QuestPDF.Settings.License = LicenseType.Community;

		_ = Directory.CreateDirectory(_testDataPath);

		// If files already exist, skip generation
		if (File.Exists(Path.Combine(_testDataPath, "large.pdf")))
		{
			return;
		}

		BuildPdf("simple.pdf", "Simple PDF Document", ComposeSimpleColumn, ComposePageNumber);
		CreateEmptyPdf();
		BuildPdf("multi-page.pdf", "Multi-Page Document", ComposeMultiPageColumn, ComposePageNumberOfTotal);
		BuildPdf("with-headings.pdf", "Document with Headings", ComposeHeadingsColumn, ComposePageNumber);
		BuildPdf("with-lists.pdf", "Document with Lists", ComposeListsColumn, ComposePageNumber);
		BuildPdf("with-tables.pdf", "Document with Table", ComposeEmployeeColumn, ComposePageNumber);
		BuildPdf("large.pdf", "Large PDF Document", ComposeLargeColumn, ComposePageNumberOfTotal);
	}

	private static void BuildPdf(string fileName, string title, Action<ColumnDescriptor> composeContent, Action<TextDescriptor> composeFooter)
		=> Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);
				page.DefaultTextStyle(x => x.FontSize(12));

				_ = page.Header()
					.Text(title)
					.SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

				page.Content()
					.PaddingVertical(10)
					.Column(composeContent);

				page.Footer()
					.AlignCenter()
					.Text(composeFooter);
			});
		})
		.GeneratePdf(Path.Combine(_testDataPath, fileName));

	/// <summary>
	/// Create an empty PDF.
	/// </summary>
	private static void CreateEmptyPdf()
	{
		var filePath = Path.Combine(_testDataPath, "empty.pdf");

		Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);

				_ = page.Content().Text("");
			});
		})
		.GeneratePdf(filePath);
	}

	private static void ComposeSimpleColumn(ColumnDescriptor x)
	{
		x.Spacing(20);

		_ = x.Item().Text("Introduction");
		_ = x.Item().Text("This is a simple PDF document with basic text content.");

		_ = x.Item().Text("Body");
		_ = x.Item().Text("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.");

		_ = x.Item().Text("Conclusion");
		_ = x.Item().Text("This document demonstrates basic PDF text extraction.");
	}

	private static void ComposeMultiPageColumn(ColumnDescriptor x)
	{
		x.Spacing(20);

		// Page 1 content
		_ = x.Item().Text("Chapter 1: Introduction").FontSize(16).SemiBold();
		_ = x.Item().Text("This is the first page of a multi-page document.");
		_ = x.Item().Text("Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
			"Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
			"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.");

		x.Item().PageBreak();

		// Page 2 content
		_ = x.Item().Text("Chapter 2: Middle Content").FontSize(16).SemiBold();
		_ = x.Item().Text("This is the second page with more content.");
		_ = x.Item().Text("Duis aute irure dolor in reprehenderit in voluptate velit esse " +
			"cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat " +
			"non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");

		x.Item().PageBreak();

		// Page 3 content
		_ = x.Item().Text("Chapter 3: Conclusion").FontSize(16).SemiBold();
		_ = x.Item().Text("This is the final page of the document.");
		_ = x.Item().Text("Thank you for reading this multi-page PDF document.");
	}

	private static void ComposeHeadingsColumn(ColumnDescriptor x)
	{
		x.Spacing(15);

		_ = x.Item().Text("Heading Level 1").FontSize(18).SemiBold();
		_ = x.Item().Text("This is content under heading level 1.");

		_ = x.Item().Text("Heading Level 2").FontSize(16).SemiBold();
		_ = x.Item().Text("This is content under heading level 2.");

		_ = x.Item().Text("Heading Level 3").FontSize(14).SemiBold();
		_ = x.Item().Text("This is content under heading level 3.");

		_ = x.Item().Text("Another Level 1 Heading").FontSize(18).SemiBold();
		_ = x.Item().Text("More content to demonstrate heading hierarchy.");
	}

	private static void ComposeListsColumn(ColumnDescriptor x)
	{
		x.Spacing(15);

		_ = x.Item().Text("Unordered List:").SemiBold();
		_ = x.Item().Text("\u2022 First item");
		_ = x.Item().Text("\u2022 Second item");
		_ = x.Item().Text("\u2022 Third item");

		_ = x.Item().PaddingTop(10).Text("Ordered List:").SemiBold();
		_ = x.Item().Text("1. First item");
		_ = x.Item().Text("2. Second item");
		_ = x.Item().Text("3. Third item");
	}

	private static void ComposeEmployeeColumn(ColumnDescriptor x)
	{
		x.Spacing(15);

		_ = x.Item().Text("Employee Data Table:").SemiBold();

		x.Item().Table(ComposeEmployeeTable);
	}

	private static void ComposeEmployeeTable(TableDescriptor table)
	{
		table.ColumnsDefinition(columns =>
		{
			columns.RelativeColumn();
			columns.RelativeColumn();
			columns.RelativeColumn();
		});

		table.Header(header =>
		{
			foreach (var title in new[] { "Name", "Department", "Salary" })
			{
				_ = header.Cell().Element(HeaderCellStyle).Text(title).SemiBold();
			}
		});

		string[] values = ["Alice", "Engineering", "$75,000", "Bob", "Sales", "$60,000", "Charlie", "Marketing", "$65,000"];
		foreach (var value in values)
		{
			_ = table.Cell().Element(DataCellStyle).Text(value);
		}
	}

	private static void ComposeLargeColumn(ColumnDescriptor x)
	{
		x.Spacing(10);

		// Generate 50 paragraphs
		for (var i = 1; i <= 50; i++)
		{
			_ = x.Item().Text($"Section {i}").FontSize(14).SemiBold();
			_ = x.Item().Text($"This is paragraph {i}. Lorem ipsum dolor sit amet, " +
				"consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore " +
				"et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation " +
				"ullamco laboris nisi ut aliquip ex ea commodo consequat.");
		}
	}

	private static void ComposePageNumber(TextDescriptor x)
	{
		_ = x.Span("Page ");
		_ = x.CurrentPageNumber();
	}

	private static void ComposePageNumberOfTotal(TextDescriptor x)
	{
		ComposePageNumber(x);
		_ = x.Span(" of ");
		_ = x.TotalPages();
	}

	private static IContainer HeaderCellStyle(IContainer container)
		=> container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);

	private static IContainer DataCellStyle(IContainer container)
		=> container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
}
