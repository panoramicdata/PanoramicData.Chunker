using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker;

/// <summary>
/// Main entry point for document chunking operations.
/// </summary>
public static class DocumentChunker
{
	private static readonly ChunkerFactory _factory = new();

	/// <summary>
	/// Chunk a document from a stream with default options.
	/// </summary>
	/// <param name="documentStream">The document stream to chunk.</param>
	/// <param name="type">The document type.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Chunking result with chunks and statistics.</returns>
	public static async Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		DocumentType type,
		CancellationToken cancellationToken = default) => await ChunkAsync(documentStream, type, new ChunkingOptions(), cancellationToken);

	/// <summary>
	/// Chunk a document from a stream with custom options.
	/// </summary>
	/// <param name="documentStream">The document stream to chunk.</param>
	/// <param name="type">The document type.</param>
	/// <param name="options">Chunking options.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Chunking result with chunks and statistics.</returns>
	public static async Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		DocumentType type,
		ChunkingOptions options,
		CancellationToken cancellationToken = default)
	{
		var chunker = _factory.GetChunker(type);
		return await chunker.ChunkAsync(documentStream, options, cancellationToken);
	}

	/// <summary>
	/// Chunk a document from a file path (auto-detects type from extension).
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <param name="options">Optional chunking options.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Chunking result with chunks and statistics.</returns>
	public static async Task<ChunkingResult> ChunkFileAsync(
		string filePath,
		ChunkingOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		options ??= new ChunkingOptions();

		// Set file path in options if not already set
		if (string.IsNullOrEmpty(options.SourceId))
		{
			options.SourceId = Path.GetFileName(filePath);
		}

		await using var fileStream = File.OpenRead(filePath);
		var chunker = _factory.GetChunkerForStream(fileStream, filePath);
		
		// Reset stream position
		fileStream.Position = 0;
		
		return await chunker.ChunkAsync(fileStream, options, cancellationToken);
	}

	/// <summary>
	/// Chunk a document with automatic format detection.
	/// </summary>
	/// <param name="documentStream">The document stream.</param>
	/// <param name="fileNameHint">Optional filename hint for type detection.</param>
	/// <param name="options">Optional chunking options.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Chunking result with chunks and statistics.</returns>
	public static async Task<ChunkingResult> ChunkAutoDetectAsync(
		Stream documentStream,
		string? fileNameHint = null,
		ChunkingOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		options ??= new ChunkingOptions();
		var chunker = _factory.GetChunkerForStream(documentStream, fileNameHint);
		
		// Reset stream position if possible
		if (documentStream.CanSeek)
		{
			documentStream.Position = 0;
		}
		
		return await chunker.ChunkAsync(documentStream, options, cancellationToken);
	}

	/// <summary>
	/// Create a fluent builder for advanced chunking scenarios.
	/// </summary>
	/// <returns>A new ChunkerBuilder instance.</returns>
	public static ChunkerBuilder CreateBuilder() => new(_factory);

	/// <summary>
	/// Register a custom document chunker.
	/// </summary>
	/// <param name="chunker">The chunker to register.</param>
	public static void RegisterChunker(Interfaces.IDocumentChunker chunker) => _factory.RegisterChunker(chunker);
}
