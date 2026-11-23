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

	protected async Task<ChunkingResult> ChunkAutoDetectFileAsync(string filePath, string name)
	{
		await using var stream = File.OpenRead(filePath);
		return await DocumentChunker.ChunkAutoDetectAsync(stream, name, ChunkingPresets.ForOpenAIEmbeddings(), CancellationToken);
	}

	protected static async Task<(ChunkingResult Result, TimeSpan Elapsed)> ChunkTimedAsync(string filePath, DocumentType documentType)
	{
		var startTime = DateTime.UtcNow;
		var result = await ChunkFileAsync(filePath, documentType);
		return (result, DateTime.UtcNow - startTime);
	}

	protected static Task<ChunkingResult> ChunkValidatedAsync(string filePath, DocumentType documentType)
		=> ChunkFileAsync(filePath, documentType, new ChunkingOptions { ValidateChunks = true });

	protected static async Task<List<T>> SuccessfulChunksAsync<T>(Task<ChunkingResult> resultTask) where T : ChunkerBase
	{
		var result = await resultTask;

		_ = result.Success.Should().BeTrue();
		return [.. result.Chunks.OfType<T>()];
	}
	protected static async Task<List<DocxTableChunk>> SuccessfulDocxTablesAsync(string fileName, ChunkingOptions options)
	{
		var tables = await SuccessfulChunksAsync<DocxTableChunk>(ChunkDocxAsync(fileName, options));

		_ = tables.Should().NotBeEmpty();
		return tables;
	}

	protected static Task<ChunkingResult> ChunkDocxAsync(string fileName, ChunkingOptions options)
		=> ChunkDocxAsync(fileName, options, new CharacterBasedTokenCounter());

	protected static Task<ChunkingResult> ChunkDocxAsync(string fileName, ChunkingOptions options, ITokenCounter tokenCounter)
		=> ChunkTestDataAsync(new DocxDocumentChunker(tokenCounter), Path.Combine("TestData", "Docx", fileName), options);

	protected static Task<ChunkingResult> ChunkPptxAsync(string fileName, ChunkingOptions options)
		=> ChunkTestDataAsync(new PptxDocumentChunker(new CharacterBasedTokenCounter()), Path.Combine("TestData", "Pptx", fileName), options);

	private static async Task<ChunkingResult> ChunkTestDataAsync(IDocumentChunker chunker, string path, ChunkingOptions options)
	{
		_ = File.Exists(path).Should().BeTrue($"test data file '{path}' must exist");
		await using var stream = File.OpenRead(path);
		return await chunker.ChunkAsync(stream, options, CancellationToken);
	}
}