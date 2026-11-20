using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Chunkers.Pptx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests;

public class BaseTest(ITestOutputHelper output)
{
	protected readonly ITestOutputHelper _output = output;

	protected static readonly CancellationToken CancellationToken = TestContext.Current.CancellationToken;

	static BaseTest()
	{
		CsvTestFileGenerator.GenerateAllTestFiles();
		XlsxTestFileGenerator.GenerateAllTestFiles();
		PdfTestFileGenerator.GenerateAllTestFiles();
		OfficeTestFileGenerator.GenerateAllTestFiles();
	}

	protected static Task<ChunkingResult> ChunkFileAsync(string filePath, DocumentType documentType)
		=> ChunkFileAsync(filePath, documentType, ChunkingPresets.ForOpenAIEmbeddings());

	protected static async Task<ChunkingResult> ChunkFileAsync(string filePath, DocumentType documentType, ChunkingOptions options)
	{
		await using var stream = File.OpenRead(filePath);
		return await DocumentChunker.ChunkAsync(stream, documentType, options, CancellationToken);
	}

	protected static Task<ChunkingResult> ChunkDocxAsync(string fileName, ChunkingOptions options)
		=> ChunkTestDataAsync(new DocxDocumentChunker(new CharacterBasedTokenCounter()), Path.Combine("TestData", "Docx", fileName), options);

	protected static Task<ChunkingResult> ChunkPptxAsync(string fileName, ChunkingOptions options)
		=> ChunkTestDataAsync(new PptxDocumentChunker(new CharacterBasedTokenCounter()), Path.Combine("TestData", "Pptx", fileName), options);

	private static async Task<ChunkingResult> ChunkTestDataAsync(IDocumentChunker chunker, string path, ChunkingOptions options)
	{
		_ = File.Exists(path).Should().BeTrue($"test data file '{path}' must exist");
		await using var stream = File.OpenRead(path);
		return await chunker.ChunkAsync(stream, options, CancellationToken);
	}
}