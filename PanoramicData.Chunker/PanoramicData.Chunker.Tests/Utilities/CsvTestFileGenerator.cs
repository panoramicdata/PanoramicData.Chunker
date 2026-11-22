using System.Text;

namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Utility class to generate test CSV files for integration testing.
/// </summary>
public static class CsvTestFileGenerator
{
	private static readonly string _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Csv");

	/// <summary>
	/// Generate all test CSV files.
	/// </summary>
	public static void GenerateAllTestFiles()
		=> TestFileGeneration.Run(_testDataPath, "simple.csv", GenerateAllTestFilesCore);

	private static void GenerateAllTestFilesCore()
	{
		CreateSimpleCsv();
		CreateEmptyCsv();
		CreateWithQuotes();
		CreateWithDifferentDelimiters();
		CreateLargeCsv();
		CreateNoHeader();
		CreateMixedData();
	}

	/// <summary>
	/// Create a simple CSV with headers.
	/// </summary>
	private static void CreateSimpleCsv()
	{
		WriteTestFile("simple.csv", @"Name,Age,City
Alice,30,New York
Bob,25,London
Charlie,35,Tokyo");
	}

	/// <summary>
	/// Create an empty CSV file.
	/// </summary>
	private static void CreateEmptyCsv()
	{
		WriteTestFile("empty.csv", string.Empty);
	}

	/// <summary>
	/// Create a CSV with quoted fields.
	/// </summary>
	private static void CreateWithQuotes()
	{
		WriteTestFile("with-quotes.csv", @"Product,Description,Price
""Premium Widget"",""High-quality, durable widget"",29.99
""Basic Widget"",""Standard widget, good value"",14.99
""Deluxe Widget"",""Top-of-the-line, includes warranty"",49.99");
	}

	/// <summary>
	/// Create CSV files with different delimiters.
	/// </summary>
	private static void CreateWithDifferentDelimiters()
	{
		// Tab-delimited
		WriteTestFile("tab-delimited.csv", "Name\tAge\tCity\nAlice\t30\tNew York\nBob\t25\tLondon");

		// Semicolon-delimited
		WriteTestFile("semicolon-delimited.csv", "Name;Age;City\nAlice;30;New York\nBob;25;London");

		// Pipe-delimited
		WriteTestFile("pipe-delimited.csv", "Name|Age|City\nAlice|30|New York\nBob|25|London");
	}

	/// <summary>
	/// Create a large CSV for performance testing.
	/// </summary>
	private static void CreateLargeCsv()
	{
		var sb = new StringBuilder();

		_ = sb.AppendLine("ID,Name,Value,Date,Status");

		for (var i = 1; i <= 1000; i++)
		{
			_ = sb.AppendLine($"{i},Item {i},{i * 10.5:F2},2025-01-{(i % 28) + 1:D2},{(i % 3 == 0 ? "Active" : "Inactive")}");
		}

		WriteTestFile("large.csv", sb.ToString());
	}

	/// <summary>
	/// Create a CSV without header row.
	/// </summary>
	private static void CreateNoHeader()
	{
		WriteTestFile("no-header.csv", @"1,John,100
2,Jane,200
3,Bob,300");
	}

	/// <summary>
	/// Create a CSV with mixed data types.
	/// </summary>
	private static void CreateMixedData()
	{
		WriteTestFile("mixed-data.csv", @"Text,Number,Date,Boolean,Currency
Sample,123.45,2025-01-23,true,$1234.56
Another,456.78,2025-01-24,false,$2345.67
Test,789.01,2025-01-25,true,$3456.78");
	}

	private static void WriteTestFile(string name, string content)
		=> File.WriteAllText(Path.Combine(_testDataPath, name), content, Encoding.UTF8);
}
