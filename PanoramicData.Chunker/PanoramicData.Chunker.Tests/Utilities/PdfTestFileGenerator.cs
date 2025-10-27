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
	public static void GenerateAllTestFiles()
	{
		// Configure QuestPDF license (community license)
		QuestPDF.Settings.License = LicenseType.Community;

		_ = Directory.CreateDirectory(_testDataPath);

		// If files already exist, skip generation
		if (File.Exists(Path.Combine(_testDataPath, "simple.pdf")))
		{
			return;
		}

		CreateSimplePdf();
		CreateEmptyPdf();
		CreateMultiPagePdf();
		CreateWithHeadings();
		CreateWithLists();
		CreateWithTables();
		CreateLargePdf();
	}

	/// <summary>
	/// Create a simple single-page PDF.
	/// </summary>
	private static void CreateSimplePdf()
	{
		var filePath = Path.Combine(_testDataPath, "simple.pdf");

		Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);
				page.DefaultTextStyle(x => x.FontSize(12));

				_ = page.Header()
					.Text("Simple PDF Document")
					.SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

				page.Content()
					.PaddingVertical(10)
					.Column(x =>
					{
						x.Spacing(20);

						_ = x.Item().Text("Introduction");
						_ = x.Item().Text("This is a simple PDF document with basic text content.");

						_ = x.Item().Text("Body");
						_ = x.Item().Text("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.");

						_ = x.Item().Text("Conclusion");
						_ = x.Item().Text("This document demonstrates basic PDF text extraction.");
					});

				page.Footer()
					.AlignCenter()
					.Text(x =>
					{
						_ = x.Span("Page ");
						_ = x.CurrentPageNumber();
					});
			});
		})
		.GeneratePdf(filePath);
	}

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

	/// <summary>
	/// Create a multi-page PDF.
	/// </summary>
	private static void CreateMultiPagePdf()
	{
		var filePath = Path.Combine(_testDataPath, "multi-page.pdf");

		Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);
				page.DefaultTextStyle(x => x.FontSize(12));

				_ = page.Header()
					.Text("Multi-Page Document")
					.SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

				page.Content()
					.PaddingVertical(10)
					.Column(x =>
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
					});

				page.Footer()
					.AlignCenter()
					.Text(x =>
					{
						_ = x.Span("Page ");
						_ = x.CurrentPageNumber();
						_ = x.Span(" of ");
						_ = x.TotalPages();
					});
			});
		})
		.GeneratePdf(filePath);
	}

	/// <summary>
	/// Create a PDF with headings.
	/// </summary>
	private static void CreateWithHeadings()
	{
		var filePath = Path.Combine(_testDataPath, "with-headings.pdf");

		Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);
				page.DefaultTextStyle(x => x.FontSize(12));

				_ = page.Header()
					.Text("Document with Headings")
					.SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

				page.Content()
					.PaddingVertical(10)
					.Column(x =>
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
					});

				page.Footer()
					.AlignCenter()
					.Text(x =>
					{
						_ = x.Span("Page ");
						_ = x.CurrentPageNumber();
					});
			});
		})
		.GeneratePdf(filePath);
	}

	/// <summary>
	/// Create a PDF with lists.
	/// </summary>
	private static void CreateWithLists()
	{
		var filePath = Path.Combine(_testDataPath, "with-lists.pdf");

		Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);
				page.DefaultTextStyle(x => x.FontSize(12));

				_ = page.Header()
					.Text("Document with Lists")
					.SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

				page.Content()
					.PaddingVertical(10)
					.Column(x =>
					{
						x.Spacing(15);

						_ = x.Item().Text("Unordered List:").SemiBold();
						_ = x.Item().Text("� First item");
						_ = x.Item().Text("� Second item");
						_ = x.Item().Text("� Third item");

						_ = x.Item().PaddingTop(10).Text("Ordered List:").SemiBold();
						_ = x.Item().Text("1. First item");
						_ = x.Item().Text("2. Second item");
						_ = x.Item().Text("3. Third item");
					});

				page.Footer()
					.AlignCenter()
					.Text(x =>
					{
						_ = x.Span("Page ");
						_ = x.CurrentPageNumber();
					});
			});
		})
		.GeneratePdf(filePath);
	}

	/// <summary>
	/// Create a PDF with a table.
	/// </summary>
	private static void CreateWithTables()
	{
		var filePath = Path.Combine(_testDataPath, "with-tables.pdf");

		Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);
				page.DefaultTextStyle(x => x.FontSize(12));

				_ = page.Header()
					.Text("Document with Table")
					.SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

				page.Content()
					.PaddingVertical(10)
					.Column(x =>
					{
						x.Spacing(15);

						_ = x.Item().Text("Employee Data Table:").SemiBold();

						x.Item().Table(table =>
						{
							table.ColumnsDefinition(columns =>
							{
								columns.RelativeColumn();
								columns.RelativeColumn();
								columns.RelativeColumn();
							});

							// Header
							table.Header(header =>
							{
								_ = header.Cell().Element(CellStyle).Text("Name").SemiBold();
								_ = header.Cell().Element(CellStyle).Text("Department").SemiBold();
								_ = header.Cell().Element(CellStyle).Text("Salary").SemiBold();

								static IContainer CellStyle(IContainer container)
								{
									return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
								}
							});

							// Data rows
							_ = table.Cell().Element(CellStyle).Text("Alice");
							_ = table.Cell().Element(CellStyle).Text("Engineering");
							_ = table.Cell().Element(CellStyle).Text("$75,000");

							_ = table.Cell().Element(CellStyle).Text("Bob");
							_ = table.Cell().Element(CellStyle).Text("Sales");
							_ = table.Cell().Element(CellStyle).Text("$60,000");

							_ = table.Cell().Element(CellStyle).Text("Charlie");
							_ = table.Cell().Element(CellStyle).Text("Marketing");
							_ = table.Cell().Element(CellStyle).Text("$65,000");

							static IContainer CellStyle(IContainer container)
							{
								return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
							}
						});
					});

				page.Footer()
					.AlignCenter()
					.Text(x =>
					{
						_ = x.Span("Page ");
						_ = x.CurrentPageNumber();
					});
			});
		})
		.GeneratePdf(filePath);
	}

	/// <summary>
	/// Create a large PDF for performance testing.
	/// </summary>
	private static void CreateLargePdf()
	{
		var filePath = Path.Combine(_testDataPath, "large.pdf");

		Document.Create(container =>
		{
			_ = container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(20);
				page.PageColor(Colors.White);
				page.DefaultTextStyle(x => x.FontSize(12));

				_ = page.Header()
					.Text("Large PDF Document")
					.SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

				page.Content()
					.PaddingVertical(10)
					.Column(x =>
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
					});

				page.Footer()
					.AlignCenter()
					.Text(x =>
					{
						_ = x.Span("Page ");
						_ = x.CurrentPageNumber();
						_ = x.Span(" of ");
						_ = x.TotalPages();
					});
			});
		})
		.GeneratePdf(filePath);
	}
}
