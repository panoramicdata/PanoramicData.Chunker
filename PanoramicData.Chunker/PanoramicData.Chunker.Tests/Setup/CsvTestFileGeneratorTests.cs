using PanoramicData.Chunker.Tests.Utilities;
using Xunit;

namespace PanoramicData.Chunker.Tests.Setup;

/// <summary>
/// Generate test CSV files. Run this test to create all test files.
/// </summary>
public class CsvTestFileGeneratorTests
{
	[Fact]
	public void GenerateAllTestFiles_ShouldCreateFiles()
	{
		// Act
		CsvTestFileGenerator.GenerateAllTestFiles();

		// Assert - Verify files were created
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Csv");
		Assert.True(Directory.Exists(testDataPath));

		var expectedFiles = new[]
		{
			"simple.csv",
			"empty.csv",
			"with-quotes.csv",
			"tab-delimited.csv",
			"semicolon-delimited.csv",
			"pipe-delimited.csv",
			"large.csv",
			"no-header.csv",
			"mixed-data.csv"
		};

		foreach (var file in expectedFiles)
		{
			var filePath = Path.Combine(testDataPath, file);
			Assert.True(File.Exists(filePath), $"File {file} should exist");
		}
	}
}
