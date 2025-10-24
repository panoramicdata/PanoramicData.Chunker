using PanoramicData.Chunker.Tests.Utilities;
using Xunit;

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
		Assert.True(Directory.Exists(testDataPath));

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
			Assert.True(File.Exists(filePath), $"File {file} should exist");
		}
	}
}
