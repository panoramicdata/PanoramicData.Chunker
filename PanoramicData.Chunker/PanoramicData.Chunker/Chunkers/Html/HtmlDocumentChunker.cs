using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Chunks HTML documents by extracting semantic elements, headings, paragraphs, lists, tables, code blocks, and images.
/// Uses AngleSharp for robust HTML parsing and DOM manipulation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HtmlDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public partial class HtmlDocumentChunker(ITokenCounter tokenCounter, ILogger<HtmlDocumentChunker>? logger = null) : IDocumentChunker
{
	private static readonly string[] HtmlMarkers = ["<!doctype html", "<html", "<head>", "<body>", "<div", "<p>"];

	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly HtmlParser _parser = new();
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;
	private readonly Stack<(int level, Guid id)> _headingStack = new();

	/// <inheritdoc/>
	public DocumentType SupportedType => DocumentType.Html;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		try
		{
			var originalPosition = documentStream.Position;

			// Read first 512 bytes to detect HTML
			var buffer = new byte[512];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			documentStream.Position = originalPosition;

			if (bytesRead == 0)
			{
				return false;
			}

			var content = Encoding.UTF8.GetString(buffer, 0, bytesRead).ToLowerInvariant();
			return HtmlMarkers.Any(content.Contains);
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
			var document = await _parser.ParseDocumentAsync(documentStream, cancellationToken);
			logger?.LogInformation("Parsed HTML document with {ElementCount} elements", document.All.Length);

			ExtractChunks(document);
			HierarchyBuilder.BuildHierarchy(_chunks);
			logger?.LogInformation("Extracted {ChunkCount} chunks from HTML document", _chunks.Count);

			return ChunkResultHelper.CreateSuccess(_chunks, startTime, options);
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking HTML document");
			return ChunkResultHelper.CreateFailure("HTML", ex);
		}
	}

	private void ExtractChunks(IHtmlDocument document)
	{
		foreach (var element in document.QuerySelectorAll("script, style").ToList())
		{
			element.Remove();
		}

		// Prefer <main>; <article> is not used because there may be several
		var mainContent = document.QuerySelector("main") ?? document.Body;
		if (mainContent == null)
		{
			logger?.LogWarning("No main content area found in HTML document");
			return;
		}

		_headingStack.Clear();
		ProcessElement(mainContent, parentId: null);
	}

	private void ProcessElement(IElement element, Guid? parentId)
	{
		var tagName = element.TagName.ToLowerInvariant();

		if (IsStructuralElement(tagName))
		{
			ProcessStructuralElement(element, tagName, parentId);
		}
		else if (tagName == "li")
		{
			ProcessListItem(element, parentId);
		}
		else if (IsContentElement(tagName))
		{
			AddChunk(CreateContentChunk(element, parentId));
		}
		else if (tagName == "table")
		{
			AddChunk(CreateTableChunk(element, parentId));
		}
		else if (tagName == "img")
		{
			AddChunk(CreateImageChunk(element, parentId));
		}
		else
		{
			ProcessChildren(element, parentId);
		}
	}

	private void ProcessChildren(IElement element, Guid? parentId)
	{
		foreach (var child in element.Children)
		{
			ProcessElement(child, parentId);
		}
	}

	private void ProcessStructuralElement(IElement element, string tagName, Guid? parentId)
	{
		var headingLevel = GetHeadingLevel(tagName);
		var actualParentId = headingLevel.HasValue ? ResolveHeadingParent(headingLevel.Value) : parentId;

		var structuralChunk = CreateStructuralChunk(element, actualParentId);
		_chunks.Add(structuralChunk);

		if (headingLevel.HasValue)
		{
			_headingStack.Push((headingLevel.Value, structuralChunk.Id));
		}

		ProcessChildren(element, structuralChunk.Id);
	}

	// Pops headings at the same or a deeper level; the heading left on top is the parent
	private Guid? ResolveHeadingParent(int headingLevel)
	{
		while (_headingStack.Count > 0 && _headingStack.Peek().level >= headingLevel)
		{
			_ = _headingStack.Pop();
		}

		return _headingStack.Count > 0 ? _headingStack.Peek().id : null;
	}

	private void ProcessListItem(IElement element, Guid? parentId)
	{
		var contentChunk = CreateContentChunk(element, parentId);
		if (contentChunk == null)
		{
			return;
		}

		_chunks.Add(contentChunk);

		// Nested lists share the parent of the list item that contains them
		var nestedItems = element.Children
			.Where(c => c.TagName.ToLowerInvariant() is "ul" or "ol")
			.SelectMany(list => list.Children);

		foreach (var listItem in nestedItems)
		{
			ProcessElement(listItem, parentId);
		}
	}

	private void AddChunk(ChunkerBase? chunk)
	{
		if (chunk != null)
		{
			_chunks.Add(chunk);
		}
	}

	private static bool IsStructuralElement(string tagName)
		=> tagName is "article" or "section" or "main" or "aside" or "header" or "footer" or "nav"
			or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

	private static bool IsContentElement(string tagName)
		=> tagName is "p" or "blockquote" or "pre" or "li" or "div";

	private static int? GetHeadingLevel(string tagName)
		=> tagName switch
		{
			"h1" => 1,
			"h2" => 2,
			"h3" => 3,
			"h4" => 4,
			"h5" => 5,
			"h6" => 6,
			_ => null
		};

	private static ChunkMetadata CreateMetadata(string hierarchy, params string[] tags) => new()
	{
		DocumentType = "HTML",
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		CreatedAt = DateTimeOffset.UtcNow
	};

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
		SemanticCompleteness = 1.0
	};
}
