using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using Microsoft.Extensions.Logging;

namespace PanoramicData.Chunker;

/// <summary>
/// Fluent builder for configuring and executing chunking operations.
/// </summary>
public class ChunkerBuilder
{
	private readonly ChunkerFactory _factory;
	private Stream? _documentStream;
	private DocumentType? _documentType;
	private string? _filePath;
	private readonly ChunkingOptions _options = new();

	internal ChunkerBuilder(ChunkerFactory factory)
	{
		_factory = factory;
	}

	/// <summary>
	/// Specify the document stream and type.
	/// </summary>
	public ChunkerBuilder WithDocument(Stream stream, DocumentType type)
	{
		_documentStream = stream;
		_documentType = type;
		_filePath = null;
		return this;
	}

	/// <summary>
	/// Specify the document file path (type will be auto-detected).
	/// </summary>
	public ChunkerBuilder WithDocument(string filePath)
	{
		_filePath = filePath;
		_documentStream = null;
		_documentType = null;
		return this;
	}

	/// <summary>
	/// Set maximum tokens per chunk.
	/// </summary>
	public ChunkerBuilder WithMaxTokens(int maxTokens)
	{
		_options.MaxTokens = maxTokens;
		return this;
	}

	/// <summary>
	/// Set overlap tokens between chunks.
	/// </summary>
	public ChunkerBuilder WithOverlap(int overlapTokens)
	{
		_options.OverlapTokens = overlapTokens;
		return this;
	}

	/// <summary>
	/// Set custom token counter.
	/// </summary>
	public ChunkerBuilder WithTokenCounter(ITokenCounter tokenCounter)
	{
		_options.TokenCounter = tokenCounter;
		_options.TokenCountingMethod = TokenCountingMethod.Custom;
		return this;
	}

	/// <summary>
	/// Set external hierarchy path.
	/// </summary>
	public ChunkerBuilder WithExternalHierarchy(string hierarchy)
	{
		_options.ExternalHierarchy = hierarchy;
		return this;
	}

	/// <summary>
	/// Add tags to all chunks.
	/// </summary>
	public ChunkerBuilder WithTags(params string[] tags)
	{
		_options.Tags.AddRange(tags);
		return this;
	}

	/// <summary>
	/// Set chunking strategy.
	/// </summary>
	public ChunkerBuilder WithStrategy(ChunkingStrategy strategy)
	{
		_options.Strategy = strategy;
		return this;
	}

	/// <summary>
	/// Enable or disable image extraction.
	/// </summary>
	public ChunkerBuilder ExtractImages(bool extract = true)
	{
		_options.ExtractImages = extract;
		return this;
	}

	/// <summary>
	/// Enable image description generation with a provider.
	/// </summary>
	public ChunkerBuilder GenerateImageDescriptions(IImageDescriptionProvider provider)
	{
		_options.GenerateImageDescriptions = true;
		_options.ImageDescriptionProvider = provider;
		return this;
	}

	/// <summary>
	/// Enable summary generation with an LLM provider.
	/// </summary>
	public ChunkerBuilder GenerateSummaries(ILlmProvider provider)
	{
		_options.GenerateSummaries = true;
		_options.LlmProvider = provider;
		return this;
	}

	/// <summary>
	/// Enable or disable keyword extraction.
	/// </summary>
	public ChunkerBuilder ExtractKeywords(bool extract = true)
	{
		_options.ExtractKeywords = extract;
		return this;
	}

	/// <summary>
	/// Enable or disable formatting preservation.
	/// </summary>
	public ChunkerBuilder PreserveFormatting(bool preserve = true)
	{
		_options.PreserveFormatting = preserve;
		return this;
	}

	/// <summary>
	/// Set logger instance.
	/// </summary>
	public ChunkerBuilder WithLogger(ILogger logger)
	{
		_options.Logger = logger;
		return this;
	}

	/// <summary>
	/// Set output format.
	/// </summary>
	public ChunkerBuilder WithOutputFormat(OutputFormat format)
	{
		_options.OutputFormat = format;
		return this;
	}

	/// <summary>
	/// Enable or disable streaming.
	/// </summary>
	public ChunkerBuilder EnableStreaming(bool enable = true)
	{
		_options.EnableStreaming = enable;
		return this;
	}

	/// <summary>
	/// Set cache provider.
	/// </summary>
	public ChunkerBuilder WithCaching(ICacheProvider cache)
	{
		_options.EnableCaching = true;
		_options.CacheProvider = cache;
		return this;
	}

	/// <summary>
	/// Set source identifier.
	/// </summary>
	public ChunkerBuilder WithSourceId(string sourceId)
	{
		_options.SourceId = sourceId;
		return this;
	}

	/// <summary>
	/// Set table serialization format.
	/// </summary>
	public ChunkerBuilder WithTableFormat(Models.TableSerializationFormat format)
	{
		_options.TableFormat = format;
		return this;
	}

	/// <summary>
	/// Enable or disable quality metrics calculation.
	/// </summary>
	public ChunkerBuilder IncludeQualityMetrics(bool include = true)
	{
		_options.IncludeQualityMetrics = include;
		return this;
	}

	/// <summary>
	/// Execute the chunking operation.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Chunking result.</returns>
	public async Task<ChunkingResult> ChunkAsync(CancellationToken cancellationToken = default)
	{
		if (_filePath != null)
		{
			return await DocumentChunker.ChunkFileAsync(_filePath, _options, cancellationToken);
		}

		if (_documentStream != null && _documentType.HasValue)
		{
			return await DocumentChunker.ChunkAsync(_documentStream, _documentType.Value, _options, cancellationToken);
		}

		if (_documentStream != null)
		{
			return await DocumentChunker.ChunkAutoDetectAsync(_documentStream, null, _options, cancellationToken);
		}

		throw new InvalidOperationException("No document specified. Use WithDocument() to specify a document stream or file path.");
	}
}
