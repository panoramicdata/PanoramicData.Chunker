using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using UglyToad.PdfPig;
using DocType = PanoramicData.Chunker.Configuration.DocumentType;

namespace PanoramicData.Chunker.Chunkers.Pdf;

/// <summary>
/// Chunks PDF documents by extracting text from pages and identifying paragraphs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PdfDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public partial class PdfDocumentChunker(ITokenCounter tokenCounter, ILogger<PdfDocumentChunker>? logger = null) : IDocumentChunker
{
	private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46, 0x2D];

	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;

	/// <inheritdoc/>
	public DocType SupportedType => DocType.Pdf;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		try
		{
			var originalPosition = documentStream.Position;

			var buffer = new byte[PdfSignature.Length];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			documentStream.Position = originalPosition;

			// PDF files start with %PDF-
			return bytesRead >= PdfSignature.Length && buffer.AsSpan().SequenceEqual(PdfSignature);
		}
		catch
		{
			return false;
		}
	}

	/// <inheritdoc/>
	public async Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;
		_chunks.Clear();

		try
		{
			var workingStream = await GetSeekableStreamAsync(documentStream, cancellationToken);
			using var pdf = PdfDocument.Open(workingStream);

			if (pdf.NumberOfPages == 0)
			{
				logger?.LogInformation("Empty PDF document");
				return CreateEmptyResult(startTime);
			}

			logger?.LogInformation("Processing PDF with {PageCount} pages", pdf.NumberOfPages);
			ExtractDocument(pdf);

			HierarchyBuilder.BuildHierarchy(_chunks);
			logger?.LogInformation("Extracted {ChunkCount} chunks from PDF ({PageCount} pages)", _chunks.Count, pdf.NumberOfPages);

			return ChunkResultHelper.CreateSuccess(_chunks, startTime, options);
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking PDF document");
			return ChunkResultHelper.CreateFailure("PDF", ex);
		}
	}

	// PdfPig requires a seekable stream
	private static async Task<Stream> GetSeekableStreamAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		if (documentStream.CanSeek)
		{
			return documentStream;
		}

		var memoryStream = new MemoryStream();
		await documentStream.CopyToAsync(memoryStream, cancellationToken);
		memoryStream.Position = 0;
		return memoryStream;
	}

	private void ExtractDocument(PdfDocument pdf)
	{
		var documentChunk = CreateDocumentChunk(pdf);
		_chunks.Add(documentChunk);

		for (var pageNumber = 1; pageNumber <= pdf.NumberOfPages; pageNumber++)
		{
			var page = pdf.GetPage(pageNumber);

			var pageChunk = CreatePageChunk(page, documentChunk.Id);
			_chunks.Add(pageChunk);
			_chunks.AddRange(ExtractParagraphsFromPage(page, pageChunk.Id));
		}
	}

	private static ChunkingResult CreateEmptyResult(DateTime startTime) => new()
	{
		Chunks = [],
		Statistics = new ChunkingStatistics
		{
			ProcessingTime = DateTime.UtcNow - startTime
		},
		Warnings = [],
		Success = true
	};
}
