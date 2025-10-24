using System.Text;

namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Utility class to generate test CSV files for integration testing.
/// </summary>
public static class CsvTestFileGenerator
{
	private static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Csv");

	/// <summary>
	/// Generate all test CSV files.
	/// </summary>
	public static void GenerateAllTestFiles()
	{
		Directory.CreateDirectory(TestDataPath);

		// If files already exist, skip generation
		if (File.Exists(Path.Combine(TestDataPath, "simple.csv")))
		{
			return;
		}

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
		var filePath = Path.Combine(TestDataPath, "simple.csv");
		var content = @"Name,Age,City
Alice,30,New York
Bob,25,London
Charlie,35,Tokyo";

		File.WriteAllText(filePath, content, Encoding.UTF8);
	}

	/// <summary>
	/// Create an empty CSV file.
	/// </summary>
	private static void CreateEmptyCsv()
	{
		var filePath = Path.Combine(TestDataPath, "empty.csv");
		File.WriteAllText(filePath, string.Empty, Encoding.UTF8);
	}

	/// <summary>
	/// Create a CSV with quoted fields.
	/// </summary>
	private static void CreateWithQuotes()
	{
		var filePath = Path.Combine(TestDataPath, "with-quotes.csv");
		var content = @"Product,Description,Price
""Premium Widget"",""High-quality, durable widget"",29.99
""Basic Widget"",""Standard widget, good value"",14.99
""Deluxe Widget"",""Top-of-the-line, includes warranty"",49.99";

		File.WriteAllText(filePath, content, Encoding.UTF8);
	}

	/// <summary>
	/// Create CSV files with different delimiters.
	/// </summary>
	private static void CreateWithDifferentDelimiters()
	{
		// Tab-delimited
		var tabPath = Path.Combine(TestDataPath, "tab-delimited.csv");
		var tabContent = "Name\tAge\tCity\nAlice\t30\tNew York\nBob\t25\tLondon";
		File.WriteAllText(tabPath, tabContent, Encoding.UTF8);

		// Semicolon-delimited
		var semiPath = Path.Combine(TestDataPath, "semicolon-delimited.csv");
		var semiContent = "Name;Age;City\nAlice;30;New York\nBob;25;London";
		File.WriteAllText(semiPath, semiContent, Encoding.UTF8);

		// Pipe-delimited
		var pipePath = Path.Combine(TestDataPath, "pipe-delimited.csv");
		var pipeContent = "Name|Age|City\nAlice|30|New York\nBob|25|London";
		File.WriteAllText(pipePath, pipeContent, Encoding.UTF8);
	}

	/// <summary>
	/// Create a large CSV for performance testing.
	/// </summary>
	private static void CreateLargeCsv()
	{
		var filePath = Path.Combine(TestDataPath, "large.csv");
		var sb = new StringBuilder();
		
		sb.AppendLine("ID,Name,Value,Date,Status");
		
		for (int i = 1; i <= 1000; i++)
		{
			sb.AppendLine($"{i},Item {i},{i * 10.5:F2},2025-01-{(i % 28) + 1:D2},{(i % 3 == 0 ? "Active" : "Inactive")}");
		}

		File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
	}

	/// <summary>
	/// Create a CSV without header row.
	/// </summary>
	private static void CreateNoHeader()
	{
		var filePath = Path.Combine(TestDataPath, "no-header.csv");
		var content = @"1,John,100
2,Jane,200
3,Bob,300";

		File.WriteAllText(filePath, content, Encoding.UTF8);
	}

	/// <summary>
	/// Create a CSV with mixed data types.
	/// </summary>
	private static void CreateMixedData()
	{
		var filePath = Path.Combine(TestDataPath, "mixed-data.csv");
		var content = @"Text,Number,Date,Boolean,Currency
Sample,123.45,2025-01-23,true,$1234.56
Another,456.78,2025-01-24,false,$2345.67
Test,789.01,2025-01-25,true,$3456.78";

		File.WriteAllText(filePath, content, Encoding.UTF8);
	}
}
