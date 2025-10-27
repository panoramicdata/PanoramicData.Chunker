using AwesomeAssertions;
using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests.Setup;

/// <summary>
/// Generate test PDF files. Run this test to create all test files.
/// </summary>
public class PdfTestFileGeneratorTests
{
	[Fact]
	public void GenerateAllTestFiles_ShouldCreateFiles()
	{
		// Act
		PdfTestFileGenerator.GenerateAllTestFiles();

		// Assert - Verify files were created
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		_ = Directory.Exists(testDataPath).Should().BeTrue();

		var expectedFiles = new[]
		{
			"simple.pdf",
			"empty.pdf",
			"multi-page.pdf",
			"with-headings.pdf",
			"with-lists.pdf",
			"with-tables.pdf",
			"large.pdf"
		};

		foreach (var file in expectedFiles)
		{
			var filePath = Path.Combine(testDataPath, file);
			_ = File.Exists(filePath).Should().BeTrue($"File {file} should exist");
		}
	}
}
