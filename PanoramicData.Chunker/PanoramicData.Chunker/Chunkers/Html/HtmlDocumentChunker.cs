using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text;
using System.Text.RegularExpressions;

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
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private readonly HtmlParser _parser = new HtmlParser();
	private readonly List<ChunkerBase> _chunks = [];
	private int _sequenceNumber;
	private readonly Stack<(int level, Guid id)> _headingStack = new();

	/// <inheritdoc/>
	public DocumentType SupportedType => DocumentType.Html;

	/// <inheritdoc/>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
	{
		try
		{
			// Save position to restore later
			var originalPosition = documentStream.Position;
			
			// Read first 512 bytes to detect HTML
			var buffer = new byte[512];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);
			
			// Restore position
			documentStream.Position = originalPosition;

			if (bytesRead == 0)
			{
				return false;
			}

			var content = Encoding.UTF8.GetString(buffer, 0, bytesRead).ToLowerInvariant();

			// Check for common HTML markers
			return content.Contains("<!doctype html") 
				|| content.Contains("<html") 
				|| content.Contains("<head>") 
				|| content.Contains("<body>")
				|| content.Contains("<div")
				|| content.Contains("<p>");
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
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;
		_chunks.Clear();

		try
		{
			// Parse HTML document
			var document = await _parser.ParseDocumentAsync(documentStream, cancellationToken);

			logger?.LogInformation("Parsed HTML document with {ElementCount} elements", document.All.Length);

			// Extract chunks from the document
			ExtractChunks(document);

			// Build hierarchy
			HierarchyBuilder.BuildHierarchy(_chunks);

			logger?.LogInformation("Extracted {ChunkCount} chunks from HTML document", _chunks.Count);

			// Calculate statistics
			var statistics = CalculateStatistics(_chunks, startTime);

			// Perform validation if requested
			var validationResult = options.ValidateChunks
				? ValidateChunks(_chunks)
				: null;

			return new ChunkingResult
			{
				Chunks = _chunks,
				Statistics = statistics,
				ValidationResult = validationResult,
				Warnings = [],
				Success = true
			};
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking HTML document");
			return new ChunkingResult
			{
				Chunks = [],
				Statistics = new ChunkingStatistics(),
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk HTML document: {ex.Message}"
					}
				],
				Success = false
			};
		}
	}

	private void ExtractChunks(IHtmlDocument document)
	{
		// Remove script and style elements
		var scriptsAndStyles = document.QuerySelectorAll("script, style").ToList();
		foreach (var element in scriptsAndStyles)
		{
			element.Remove();
		}

		// Find the main content area (prefer <main>, or <body>)
		// Don't use <article> as the main area since there might be multiple articles
		var mainContent = document.QuerySelector("main") 
			?? document.Body;

		if (mainContent == null)
		{
			logger?.LogWarning("No main content area found in HTML document");
			return;
		}

		// Clear heading stack for new document
		_headingStack.Clear();

		// Process the main content recursively
		ProcessElement(mainContent, parentId: null, depth: 0);
	}

	private void ProcessElement(IElement element, Guid? parentId, int depth)
	{
		var tagName = element.TagName.ToLowerInvariant();

		// Check if this is a structural element (semantic or heading)
		if (IsStructuralElement(tagName))
		{
			// Determine the actual parent ID based on heading hierarchy
			var actualParentId = parentId;
			var headingLevel = GetHeadingLevel(tagName);
			
			if (headingLevel.HasValue)
			{
				// For headings, find the appropriate parent based on hierarchy
				// Pop headings from stack that are at same or deeper level
				while (_headingStack.Count > 0 && _headingStack.Peek().level >= headingLevel.Value)
				{
					_ = _headingStack.Pop();
				}

				// The parent is the heading on top of the stack (if any)
				if (_headingStack.Count > 0)
				{
					actualParentId = _headingStack.Peek().id;
				}
				else
				{
					actualParentId = null; // This is a root-level heading
				}
			}

			var structuralChunk = CreateStructuralChunk(element, actualParentId, depth);
			_chunks.Add(structuralChunk);

			// Push this heading onto the stack if it's a heading
			if (headingLevel.HasValue)
			{
				_headingStack.Push((headingLevel.Value, structuralChunk.Id));
			}

			// Process children with this chunk as parent
			foreach (var child in element.Children)
			{
				ProcessElement(child, structuralChunk.Id, depth + 1);
			}
		}
		// Check if this is a list item - special handling for nested lists
		else if (tagName == "li")
		{
			var contentChunk = CreateContentChunk(element, parentId, depth);
			if (contentChunk != null)
			{
				_chunks.Add(contentChunk);
				
				// Process any nested ul/ol elements within this li
				foreach (var child in element.Children.Where(c => c.TagName.ToLowerInvariant() is "ul" or "ol"))
				{
					// Process the nested list's children with same parent
					foreach (var listItem in child.Children)
					{
						ProcessElement(listItem, parentId, depth);
					}
				}
			}
		}
		// Check if this is a content element
		else if (IsContentElement(tagName))
		{
			var contentChunk = CreateContentChunk(element, parentId, depth);
			if (contentChunk != null)
			{
				_chunks.Add(contentChunk);
			}
		}
		// Check if this is a table
		else if (tagName == "table")
		{
			var tableChunk = CreateTableChunk(element, parentId, depth);
			if (tableChunk != null)
			{
				_chunks.Add(tableChunk);
			}
		}
		// Check if this is an image
		else if (tagName == "img")
		{
			var imageChunk = CreateImageChunk(element, parentId, depth);
			if (imageChunk != null)
			{
				_chunks.Add(imageChunk);
			}
		}
		// Otherwise, process children in the same context
		else
		{
			foreach (var child in element.Children)
			{
				ProcessElement(child, parentId, depth);
			}
		}
	}

	private static bool IsStructuralElement(string tagName)
		=> tagName is "article" or "section" or "main" or "aside" or "header" or "footer" or "nav"
			or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

	private static bool IsContentElement(string tagName)
		=> tagName is "p" or "blockquote" or "pre" or "li" or "div";

	private HtmlSectionChunk CreateStructuralChunk(IElement element, Guid? parentId, int depth)
	{
		var tagName = element.TagName.ToLowerInvariant();
		var text = GetCleanText(element);

		var chunk = new HtmlSectionChunk
		{
			ParentId = parentId,
			TagName = tagName,
			HeadingLevel = GetHeadingLevel(tagName),
			CssClasses = element.ClassList.ToList(),
			ElementId = element.Id,
			Role = element.GetAttribute("role"),
			SequenceNumber = _sequenceNumber++,
			SpecificType = GetHeadingLevel(tagName).HasValue ? $"Heading{GetHeadingLevel(tagName)}" : tagName,
			Metadata = new ChunkMetadata
			{
				DocumentType = "HTML",
				SourceId = string.Empty,
				Hierarchy = tagName,
				Tags = [tagName],
				CreatedAt = DateTimeOffset.UtcNow
			},
			// Calculate quality metrics based on text content
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = _tokenCounter.CountTokens(text),
				CharacterCount = text.Length,
				WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
				SemanticCompleteness = 1.0
			}
		};

		return chunk;
	}

	private ChunkerBase? CreateContentChunk(IElement element, Guid? parentId, int depth)
	{
		var tagName = element.TagName.ToLowerInvariant();
		
		// For list items, exclude nested list content
		string text;
		if (tagName == "li")
		{
			// Get only direct text nodes, not nested ul/ol content
			text = GetDirectTextContent(element);
		}
		else
		{
			text = GetCleanText(element);
		}

		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		ChunkerBase chunk = tagName switch
		{
			"p" => new HtmlParagraphChunk
			{
				Content = text,
				HtmlContent = element.InnerHtml,
				CssClasses = element.ClassList.ToList(),
				ElementId = element.Id
			},
			"blockquote" => new HtmlBlockquoteChunk
			{
				Content = text,
				HtmlContent = element.InnerHtml,
				CiteUrl = element.GetAttribute("cite"),
				CssClasses = element.ClassList.ToList(),
				ElementId = element.Id
			},
			"pre" => CreateCodeBlockChunk(element, text),
			"li" => CreateListItemChunk(element, text),
			"div" => new HtmlParagraphChunk // Treat divs as paragraphs
			{
				Content = text,
				HtmlContent = element.InnerHtml,
				CssClasses = element.ClassList.ToList(),
				ElementId = element.Id
			},
			_ => new HtmlParagraphChunk
			{
				Content = text,
				HtmlContent = element.InnerHtml
			}
		};

		chunk.ParentId = parentId;
		chunk.SequenceNumber = _sequenceNumber++;
		chunk.SpecificType = tagName;
		chunk.Metadata = new ChunkMetadata
		{
			DocumentType = "HTML",
			SourceId = string.Empty,
			Hierarchy = tagName,
			Tags = [tagName],
			CreatedAt = DateTimeOffset.UtcNow
		};

		// Extract annotations (links, formatting)
		var annotations = ExtractAnnotations(element);
		if (chunk is ContentChunk contentChunk && annotations.Count > 0)
		{
			contentChunk.Annotations = annotations;
		}

		// Calculate quality metrics using token counter
		chunk.QualityMetrics = new ChunkQualityMetrics
		{
			TokenCount = _tokenCounter.CountTokens(text),
			CharacterCount = text.Length,
			WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
			SemanticCompleteness = 1.0
		};

		return chunk;
	}

	private static HtmlCodeBlockChunk CreateCodeBlockChunk(IElement element, string text)
	{
		var codeElement = element.QuerySelector("code");
		var hasCodeElement = codeElement != null;
		var targetElement = codeElement ?? element;

		// Try to extract language from class (e.g., "language-csharp" or "lang-csharp")
		string? language = null;
		var classes = targetElement.ClassList;
		var languageClass = classes.FirstOrDefault(c => c.StartsWith("language-") || c.StartsWith("lang-"));
		if (languageClass != null)
		{
			language = languageClass.Replace("language-", "").Replace("lang-", "");
		}

		return new HtmlCodeBlockChunk
		{
			Content = text,
			HtmlContent = element.InnerHtml,
			Language = language,
			HasCodeElement = hasCodeElement,
			CssClasses = element.ClassList.ToList(),
			ElementId = element.Id
		};
	}

	private static HtmlListItemChunk CreateListItemChunk(IElement element, string text)
	{
		// Determine list type by looking at immediate parent
		var parent = element.ParentElement;
		var listType = parent?.TagName.ToLowerInvariant() ?? "ul";

		// Calculate nesting level - count the number of list (ul/ol) ancestors
		// Start counting from the parent's parent (skip the immediate list parent)
		// to get 0-based nesting level where 0 = top-level list item
		var nestingLevel = 0;
		var current = element.ParentElement?.ParentElement; // Start from grandparent
		
		while (current != null)
		{
			var tagName = current.TagName.ToLowerInvariant();
			// Count ul/ol ancestors, but skip if they're inside a li (that's the content)
			if (tagName is "ul" or "ol")
			{
				nestingLevel++;
			}
			current = current.ParentElement;
		}

		return new HtmlListItemChunk
		{
			Content = text,
			HtmlContent = element.InnerHtml,
			ListType = listType,
			NestingLevel = nestingLevel,
			CssClasses = element.ClassList.ToList(),
			ElementId = element.Id
		};
	}

	private HtmlTableChunk? CreateTableChunk(IElement element, Guid? parentId, int depth)
	{
		if (element is not IHtmlTableElement table)
		{
			return null;
		}

		var caption = table.Caption?.TextContent.Trim();
		var summary = table.GetAttribute("summary") ?? table.GetAttribute("aria-label");

		// Extract headers
		var headers = new List<string>();
		var headerRow = table.QuerySelector("thead tr") ?? table.QuerySelector("tr");
		if (headerRow != null)
		{
			headers.AddRange(headerRow.QuerySelectorAll("th, td")
				.Select(cell => cell.TextContent.Trim()));
		}

		// Extract rows
		var rows = new List<List<string>>();
		var bodyRowsList = table.QuerySelectorAll("tbody tr").ToList();
		if (bodyRowsList.Count == 0)
		{
			// If no tbody, get all rows except the first one (which we used for headers)
			bodyRowsList = table.QuerySelectorAll("tr").Skip(1).ToList();
		}

		foreach (var row in bodyRowsList)
		{
			var cells = row.QuerySelectorAll("td, th")
				.Select(cell => cell.TextContent.Trim())
				.ToList();
			if (cells.Count > 0)
			{
				rows.Add(cells);
			}
		}

		if (headers.Count == 0 && rows.Count == 0)
		{
			return null;
		}

		// Serialize table
		var tableText = SerializeTableAsMarkdown(headers, rows);

		var chunk = new HtmlTableChunk
		{
			Content = tableText,
			HtmlContent = element.OuterHtml,
			Caption = caption,
			Summary = summary,
			CssClasses = element.ClassList.ToList(),
			ElementId = element.Id,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			TableInfo = new TableMetadata
			{
				RowCount = rows.Count,
				ColumnCount = headers.Count > 0 ? headers.Count : (rows.Count > 0 ? rows[0].Count : 0),
				Headers = [.. headers],
				HasHeaderRow = headers.Count > 0,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			SerializedTable = tableText,
			SerializationFormat = TableSerializationFormat.Markdown,
			Metadata = new ChunkMetadata
			{
				DocumentType = "HTML",
				SourceId = string.Empty,
				Hierarchy = "table",
				Tags = ["table"],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics using token counter
		var allText = string.Join(" ", headers) + " " + string.Join(" ", rows.SelectMany(r => r));
		chunk.QualityMetrics = new ChunkQualityMetrics
		{
			TokenCount = _tokenCounter.CountTokens(allText),
			CharacterCount = allText.Length,
			WordCount = allText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
			SemanticCompleteness = 1.0
		};

		return chunk;
	}

	private HtmlImageChunk? CreateImageChunk(IElement element, Guid? parentId, int depth)
	{
		if (element is not IHtmlImageElement img)
		{
			return null;
		}

		// Use the original src attribute, not the computed Source property
		// AngleSharp's Source property resolves relative URLs to absolute URLs with a base URL
		var src = img.GetAttribute("src");
		if (string.IsNullOrEmpty(src))
		{
			return null;
		}

		var altText = img.AlternativeText ?? string.Empty;
		var title = img.Title;

		// Parse width and height attributes
		int? width = null;
		int? height = null;
		if (int.TryParse(img.GetAttribute("width"), out var w))
		{
			width = w;
		}
		if (int.TryParse(img.GetAttribute("height"), out var h))
		{
			height = h;
		}

		return new HtmlImageChunk
		{
			ParentId = parentId,
			Caption = altText,
			BinaryReference = src,
			MimeType = DetermineMimeType(src),
			Title = title,
			Width = width,
			Height = height,
			CssClasses = element.ClassList.ToList(),
			ElementId = element.Id,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "image",
			Metadata = new ChunkMetadata
			{
				DocumentType = "HTML",
				SourceId = string.Empty,
				Hierarchy = "image",
				Tags = ["image"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			QualityMetrics = new ChunkQualityMetrics
			{
				CharacterCount = altText.Length,
				WordCount = altText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
				SemanticCompleteness = string.IsNullOrEmpty(altText) ? 0.5 : 1.0
			}
		};
	}

	private static string DetermineMimeType(string src)
	{
		var extension = Path.GetExtension(src).ToLowerInvariant();
		return extension switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".gif" => "image/gif",
			".svg" => "image/svg+xml",
			".webp" => "image/webp",
			_ => "image/unknown"
		};
	}

	private static string GetCleanText(IElement element)
	{
		// Get text content and normalize whitespace
		var text = element.TextContent;
		text = WhitespaceRegex().Replace(text, " ");
		return text.Trim();
	}

	private static string GetDirectTextContent(IElement element)
	{
		// Get only direct text nodes, excluding nested ul/ol elements
		var sb = new StringBuilder();
		
		foreach (var node in element.ChildNodes)
		{
			if (node.NodeType == NodeType.Text)
			{
				_ = sb.Append(node.TextContent);
			}
			else if (node is IElement childElement)
			{
				var childTag = childElement.TagName.ToLowerInvariant();
				// Skip nested lists
				if (childTag is not "ul" and not "ol")
				{
					_ = sb.Append(childElement.TextContent);
				}
			}
		}
		
		var text = sb.ToString();
		text = WhitespaceRegex().Replace(text, " ");
		return text.Trim();
	}

	private static List<ContentAnnotation> ExtractAnnotations(IElement element)
	{
		var annotations = new List<ContentAnnotation>();

		// Extract links
		var links = element.QuerySelectorAll("a[href]");
		foreach (var link in links)
		{
			var href = link.GetAttribute("href");
			var linkText = link.TextContent.Trim();
			if (!string.IsNullOrEmpty(href))
			{
				annotations.Add(new ContentAnnotation
				{
					Type = AnnotationType.Link,
					Attributes = new Dictionary<string, string>
					{
						["href"] = href,
						["text"] = linkText
					}
				});
			}
		}

		// Extract bold text
		var boldElements = element.QuerySelectorAll("strong, b");
		foreach (var bold in boldElements)
		{
			var text = bold.TextContent.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				annotations.Add(new ContentAnnotation
				{
					Type = AnnotationType.Bold,
					Attributes = new Dictionary<string, string>
					{
						["text"] = text
					}
				});
			}
		}

		// Extract italic text
		var italicElements = element.QuerySelectorAll("em, i");
		foreach (var italic in italicElements)
		{
			var text = italic.TextContent.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				annotations.Add(new ContentAnnotation
				{
					Type = AnnotationType.Italic,
					Attributes = new Dictionary<string, string>
					{
						["text"] = text
					}
				});
			}
		}

		// Extract code spans
		var codeElements = element.QuerySelectorAll("code:not(pre code)");
		foreach (var code in codeElements)
		{
			var text = code.TextContent.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				annotations.Add(new ContentAnnotation
				{
					Type = AnnotationType.Code,
					Attributes = new Dictionary<string, string>
					{
						["text"] = text
					}
				});
			}
		}

		return annotations;
	}

	private static string SerializeTableAsMarkdown(List<string> headers, List<List<string>> rows)
	{
		var sb = new StringBuilder();

		if (headers.Count > 0)
		{
			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", headers);
			_ = sb.AppendLine(" |");

			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", headers.Select(_ => "---"));
			_ = sb.AppendLine(" |");
		}

		foreach (var row in rows)
		{
			_ = sb.Append("| ");
			_ = sb.AppendJoin(" | ", row);
			_ = sb.AppendLine(" |");
		}

		return sb.ToString();
	}

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

	private static ChunkingStatistics CalculateStatistics(List<ChunkerBase> chunks, DateTime startTime)
	{
		var processingTime = DateTime.UtcNow - startTime;

		return new ChunkingStatistics
		{
			TotalChunks = chunks.Count,
			StructuralChunks = chunks.OfType<StructuralChunk>().Count(),
			ContentChunks = chunks.OfType<ContentChunk>().Count(),
			VisualChunks = chunks.OfType<VisualChunk>().Count(),
			TableChunks = chunks.OfType<TableChunk>().Count(),
			MaxDepth = chunks.Count != 0 ? chunks.Max(c => c.Depth) : 0,
			ProcessingTime = processingTime,
			AverageTokensPerChunk = chunks.Count != 0 ? (int)chunks.Average(c => c.QualityMetrics?.TokenCount ?? 0) : 0,
			TotalTokens = chunks.Sum(c => c.QualityMetrics?.TokenCount ?? 0),
			MaxTokensInChunk = chunks.Count != 0 ? chunks.Max(c => c.QualityMetrics?.TokenCount ?? 0) : 0,
			MinTokensInChunk = chunks.Count != 0 ? chunks.Min(c => c.QualityMetrics?.TokenCount ?? 0) : 0
		};
	}

	private static ValidationResult ValidateChunks(List<ChunkerBase> chunks)
	{
		var issues = new List<ValidationIssue>();
		var chunkIds = new HashSet<Guid>(chunks.Select(c => c.Id));

		// Check for orphaned chunks
		foreach (var chunk in chunks.Where(c => c.ParentId.HasValue))
		{
			if (!chunkIds.Contains(chunk.ParentId!.Value))
			{
				issues.Add(new ValidationIssue
				{
					Severity = ValidationSeverity.Warning,
					Message = $"Chunk {chunk.Id} references non-existent parent {chunk.ParentId.Value}",
					ChunkId = chunk.Id,
					Code = "ORPHANED_CHUNK"
				});
			}
		}

		return new ValidationResult
		{
			IsValid = issues.Count == 0,
			Issues = issues,
			HasOrphanedChunks = issues.Any(i => i.Code == "ORPHANED_CHUNK")
		};
	}

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
}
