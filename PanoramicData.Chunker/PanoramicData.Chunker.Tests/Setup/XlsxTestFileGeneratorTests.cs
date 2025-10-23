using PanoramicData.Chunker.Tests.Utilities;
using Xunit;

namespace PanoramicData.Chunker.Tests.Setup;

/// <summary>
/// Generate test XLSX files. Run this test to create all test files.
/// </summary>
public class XlsxTestFileGeneratorTests
{
	[Fact]
	public void GenerateAllTestFiles_ShouldCreateFiles()
	{
		// Act
		XlsxTestFileGenerator.GenerateAllTestFiles();

		// Assert - Verify files were created
		var testDataPath = Path.Combine("TestData", "Xlsx");
		Assert.True(Directory.Exists(testDataPath));

		var expectedFiles = new[]
		{
			"simple.xlsx",
			"empty.xlsx",
			"multiple-sheets.xlsx",
			"with-formulas.xlsx",
			"with-table.xlsx",
			"large.xlsx",
			"with-merged-cells.xlsx",
			"with-data-types.xlsx"
		};

		foreach (var file in expectedFiles)
		{
			var filePath = Path.Combine(testDataPath, file);
			Assert.True(File.Exists(filePath), $"File {file} should exist");
		}
	}
}
