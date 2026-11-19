using Markdig;
using Markdig.Syntax;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Markdown document chunker using Markdig parser.
/// Implements hierarchical chunking based on header structure.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MarkdownDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
public partial class MarkdownDocumentChunker(ITokenCounter tokenCounter) : IDocumentChunker
{
	private static readonly string[] MarkdownMarkers = ["```", "- ", "* ", "1. ", "](", "**", "__"];

	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private int _sequenceNumber;

	/// <summary>
	/// Gets the supported document type.
	/// </summary>
	public DocumentType SupportedType => DocumentType.Markdown;

	/// <summary>
	/// Validates if the stream contains a valid Markdown document.
	/// </summary>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		if (documentStream == null || !documentStream.CanRead)
		{
			return false;
		}

		try
		{
			var buffer = new byte[1024];
			var position = documentStream.Position;
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);
			documentStream.Position = position;

			return bytesRead != 0 && HasMarkdownMarkers(System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead));
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Chunks a Markdown document from a stream.
	/// </summary>
	public async Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;

		using var reader = new StreamReader(documentStream);
		var markdownContent = await reader.ReadToEndAsync(cancellationToken);

		var pipeline = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.Build();

		var document = Markdig.Markdown.Parse(markdownContent, pipeline);

		var chunks = new List<ChunkerBase>();
		ProcessDocument(document, markdownContent, chunks, options, cancellationToken);

		HierarchyBuilder.BuildHierarchy(chunks);

		var finalChunks = options.MaxTokens > 0
			? SplitOversizedChunks(chunks, options, cancellationToken)
			: chunks;

		return ChunkResultHelper.CreateSuccess(finalChunks, startTime, options);
	}

	private static bool HasMarkdownMarkers(string sample)
		=> sample.Contains('#') || MarkdownMarkers.Any(marker => sample.Contains(marker, StringComparison.Ordinal));

	private void ProcessDocument(
		MarkdownDocument document,
		string markdownContent,
		List<ChunkerBase> chunks,
		ChunkingOptions options,
		CancellationToken cancellationToken)
	{
		var headerStack = new Stack<(int Level, Guid Id)>();

		foreach (var block in document)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ProcessBlock(block, markdownContent, chunks, headerStack, options);
		}
	}

	private void ProcessBlock(
		Block block,
		string markdownContent,
		List<ChunkerBase> chunks,
		Stack<(int Level, Guid Id)> headerStack,
		ChunkingOptions options)
	{
		if (block is HeadingBlock heading)
		{
			ProcessHeading(heading, chunks, headerStack);
			return;
		}

		ProcessContentBlock(block, markdownContent, chunks, CurrentParentId(headerStack), options);
	}

	private static Guid? CurrentParentId(Stack<(int Level, Guid Id)> headerStack)
		=> headerStack.Count > 0 ? headerStack.Peek().Id : null;

	private void ProcessContentBlock(
		Block block,
		string markdownContent,
		List<ChunkerBase> chunks,
		Guid? parentId,
		ChunkingOptions options)
	{
		if (block is ParagraphBlock paragraph)
		{
			ProcessParagraph(paragraph, markdownContent, chunks, parentId);
		}
		else if (block is ListBlock list)
		{
			ProcessList(list, chunks, parentId);
		}
		else if (block is CodeBlock codeBlock)
		{
			ProcessCodeBlock(codeBlock, chunks, parentId);
		}
		else if (block is QuoteBlock quoteBlock)
		{
			ProcessQuoteBlock(quoteBlock, chunks, parentId);
		}
		else if (block is Markdig.Extensions.Tables.Table table)
		{
			ProcessTable(table, chunks, parentId, options);
		}
	}
}