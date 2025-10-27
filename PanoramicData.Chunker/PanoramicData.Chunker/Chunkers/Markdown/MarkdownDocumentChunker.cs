using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
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
public class MarkdownDocumentChunker(ITokenCounter tokenCounter) : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private int _sequenceNumber;

	/// <summary>
	/// Gets the supported document type.
	/// </summary>
	public DocumentType SupportedType => DocumentType.Markdown;

	/// <summary>
	/// Validates if the stream contains a valid Markdown document.
	/// </summary>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
	{
		if (documentStream == null || !documentStream.CanRead)
		{
			return false;
		}

		try
		{
			// Read first 1KB to check for Markdown patterns
			var buffer = new byte[1024];
			var position = documentStream.Position;
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);
			documentStream.Position = position; // Reset position

			if (bytesRead == 0)
			{
				return false;
			}

			var sample = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

			// Check for common Markdown patterns
			return sample.Contains('#') ||      // Headers
				   sample.Contains("```") ||    // Code blocks
				   sample.Contains("- ") ||     // Unordered lists
				   sample.Contains("* ") ||     // Alternative unordered lists
				   sample.Contains("1. ") ||    // Ordered lists
				   sample.Contains("](") ||     // Links/images
				   sample.Contains("**") ||     // Bold
				   sample.Contains("__");       // Alternative bold
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
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;

		// Read the Markdown content
		using var reader = new StreamReader(documentStream);
		var markdownContent = await reader.ReadToEndAsync(cancellationToken);

		// Parse the document using Markdig
		var pipeline = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.Build();

		var document = Markdig.Markdown.Parse(markdownContent, pipeline);

		// Process the document into chunks
		var chunks = new List<ChunkerBase>();
		ProcessDocument(document, markdownContent, chunks, options, cancellationToken);

		// Build hierarchy
		HierarchyBuilder.BuildHierarchy(chunks);

		// Split oversized chunks if needed
		var finalChunks = options.MaxTokens > 0
			? await SplitOversizedChunksAsync(chunks, options, cancellationToken)
			: chunks;

		// Calculate statistics
		var statistics = CalculateStatistics(finalChunks, startTime);

		// Perform validation if requested
		var validationResult = options.ValidateChunks
			? ValidateChunks(finalChunks)
			: null;

		return new ChunkingResult
		{
			Chunks = finalChunks,
			Statistics = statistics,
			ValidationResult = validationResult,
			Warnings = [],
			Success = true
		};
	}

	/// <summary>
	/// Processes the Markdown document and creates chunks.
	/// </summary>
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

			switch (block)
			{
				case HeadingBlock heading:
					ProcessHeading(heading, chunks, headerStack);
					break;

				case ParagraphBlock paragraph:
					var parentId = headerStack.Count > 0 ? headerStack.Peek().Id : (Guid?)null;
					ProcessParagraph(paragraph, markdownContent, chunks, parentId);
					break;

				case ListBlock list:
					parentId = headerStack.Count > 0 ? headerStack.Peek().Id : (Guid?)null;
					ProcessList(list, chunks, parentId);
					break;

				case CodeBlock codeBlock:
					parentId = headerStack.Count > 0 ? headerStack.Peek().Id : (Guid?)null;
					ProcessCodeBlock(codeBlock, chunks, parentId);
					break;

				case QuoteBlock quoteBlock:
					parentId = headerStack.Count > 0 ? headerStack.Peek().Id : (Guid?)null;
					ProcessQuoteBlock(quoteBlock, chunks, parentId);
					break;

				case Markdig.Extensions.Tables.Table table:
					parentId = headerStack.Count > 0 ? headerStack.Peek().Id : (Guid?)null;
					ProcessTable(table, chunks, parentId, options);
					break;
			}
		}
	}

	/// <summary>
	/// Processes a heading block and creates a structural chunk.
	/// </summary>
	private void ProcessHeading(
		HeadingBlock heading,
		List<ChunkerBase> chunks,
		Stack<(int Level, Guid Id)> headerStack)
	{
		var headingText = GetInlineText(heading.Inline);
		var markdownSyntax = $"{new string('#', heading.Level)} {headingText}";

		var chunk = new MarkdownSectionChunk
		{
			HeadingLevel = heading.Level,
			HeadingText = headingText,
			MarkdownSyntax = markdownSyntax,
			SequenceNumber = _sequenceNumber++,
			SpecificType = $"Heading{heading.Level}",
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				SourceId = string.Empty,
				Hierarchy = markdownSyntax,
				Tags = [$"h{heading.Level}"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = _tokenCounter.CountTokens(headingText),
				CharacterCount = headingText.Length,
				WordCount = headingText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
				SemanticCompleteness = 1.0
			}
		};

		// Manage header hierarchy stack
		while (headerStack.Count > 0 && headerStack.Peek().Level >= heading.Level)
		{
			_ = headerStack.Pop();
		}

		if (headerStack.Count > 0)
		{
			chunk.ParentId = headerStack.Peek().Id;
		}

		headerStack.Push((heading.Level, chunk.Id));
		chunks.Add(chunk);
	}

	/// <summary>
	/// Processes a paragraph block and creates a content chunk.
	/// </summary>
	private void ProcessParagraph(
		ParagraphBlock paragraph,
		string markdownContent,
		List<ChunkerBase> chunks,
		Guid? parentId)
	{
		var text = GetInlineText(paragraph.Inline);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		// Extract annotations (links, formatting)
		var annotations = ExtractAnnotations(paragraph.Inline);

		var chunk = new MarkdownParagraphChunk
		{
			Content = text,
			MarkdownContent = ExtractMarkdownContent(paragraph, markdownContent),
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			Annotations = annotations.Count > 0 ? annotations : null,
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				SourceId = string.Empty,
				Hierarchy = "Paragraph",
				Tags = ["paragraph"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = _tokenCounter.CountTokens(text),
				CharacterCount = text.Length,
				WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
				SemanticCompleteness = 1.0
			}
		};

		chunks.Add(chunk);
	}

	/// <summary>
	/// Processes a list block and creates content chunks for each item.
	/// </summary>
	private void ProcessList(
		ListBlock list,
		List<ChunkerBase> chunks,
		Guid? parentId,
		int nestingLevel = 0)
	{
		foreach (var item in list)
		{
			if (item is ListItemBlock listItem)
			{
				var text = GetBlockText(listItem);
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}

				var chunk = new MarkdownListItemChunk
				{
					Content = text,
					MarkdownContent = list.IsOrdered
						? $"{listItem.Order}. {text}"
						: $"- {text}",
					IsOrdered = list.IsOrdered,
					ItemNumber = list.IsOrdered ? listItem.Order : null,
					NestingLevel = nestingLevel,
					ParentId = parentId,
					SequenceNumber = _sequenceNumber++,
					Metadata = new ChunkMetadata
					{
						DocumentType = "Markdown",
						SourceId = string.Empty,
						Hierarchy = list.IsOrdered ? "Ordered List Item" : "Unordered List Item",
						Tags = ["list-item", list.IsOrdered ? "ordered" : "unordered"],
						CreatedAt = DateTimeOffset.UtcNow
					},
					QualityMetrics = new ChunkQualityMetrics
					{
						TokenCount = _tokenCounter.CountTokens(text),
						CharacterCount = text.Length,
						WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
						SemanticCompleteness = 0.8
					}
				};

				chunks.Add(chunk);

				// Process nested lists
				foreach (var subBlock in listItem)
				{
					if (subBlock is ListBlock nestedList)
					{
						ProcessList(nestedList, chunks, chunk.Id, nestingLevel + 1);
					}
				}
			}
		}
	}

	/// <summary>
	/// Processes a code block and creates a content chunk.
	/// </summary>
	private void ProcessCodeBlock(
		CodeBlock codeBlock,
		List<ChunkerBase> chunks,
		Guid? parentId)
	{
		var code = GetCodeBlockContent(codeBlock);
		if (string.IsNullOrWhiteSpace(code))
		{
			return;
		}

		var language = codeBlock is FencedCodeBlock fenced ? fenced.Info : null;
		var isFenced = codeBlock is FencedCodeBlock;

		var chunk = new MarkdownCodeBlockChunk
		{
			Content = code,
			MarkdownContent = isFenced
				? $"```{language}\n{code}\n```"
				: code,
			Language = language,
			IsFenced = isFenced,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				SourceId = string.Empty,
				Hierarchy = $"Code Block ({language ?? "plain"})",
				Tags = ["code", language ?? "plain"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = _tokenCounter.CountTokens(code),
				CharacterCount = code.Length,
				WordCount = code.Split('\n').Length, // Use line count for code
				SemanticCompleteness = 1.0
			}
		};

		chunks.Add(chunk);
	}

	/// <summary>
	/// Processes a quote block and creates a content chunk.
	/// </summary>
	private void ProcessQuoteBlock(
		QuoteBlock quoteBlock,
		List<ChunkerBase> chunks,
		Guid? parentId,
		int nestingLevel = 0)
	{
		var text = GetBlockText(quoteBlock);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var chunk = new MarkdownQuoteChunk
		{
			Content = text,
			MarkdownContent = $"> {text.Replace("\n", "\n> ")}",
			NestingLevel = nestingLevel,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				SourceId = string.Empty,
				Hierarchy = "Blockquote",
				Tags = ["blockquote"],
				CreatedAt = DateTimeOffset.UtcNow
			},
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = _tokenCounter.CountTokens(text),
				CharacterCount = text.Length,
				WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
				SemanticCompleteness = 0.9
			}
		};

		chunks.Add(chunk);
	}

	/// <summary>
	/// Processes a table and creates a table chunk.
	/// </summary>
	private void ProcessTable(
		Markdig.Extensions.Tables.Table table,
		List<ChunkerBase> chunks,
		Guid? parentId,
		ChunkingOptions options)
	{
		var headers = new List<string>();
		var rows = new List<List<string>>();
		var alignments = new List<TableColumnAlignment>();

		// Extract column alignments
		if (table.ColumnDefinitions != null)
		{
			foreach (var colDef in table.ColumnDefinitions)
			{
				alignments.Add(colDef.Alignment switch
				{
					Markdig.Extensions.Tables.TableColumnAlign.Left => TableColumnAlignment.Left,
					Markdig.Extensions.Tables.TableColumnAlign.Center => TableColumnAlignment.Center,
					Markdig.Extensions.Tables.TableColumnAlign.Right => TableColumnAlignment.Right,
					_ => TableColumnAlignment.None
				});
			}
		}

		// Extract headers and rows
		foreach (var row in table)
		{
			if (row is Markdig.Extensions.Tables.TableRow tableRow)
			{
				var rowData = new List<string>();
				foreach (var cell in tableRow)
				{
					if (cell is Markdig.Extensions.Tables.TableCell tableCell)
					{
						rowData.Add(GetBlockText(tableCell));
					}
				}

				if (tableRow.IsHeader)
				{
					headers.AddRange(rowData);
				}
				else
				{
					rows.Add(rowData);
				}
			}
		}

		var chunk = new MarkdownTableChunk
		{
			Content = SerializeTableToText(headers, rows),
			MarkdownContent = SerializeTableToMarkdown(headers, rows, alignments),
			ColumnAlignments = alignments,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			TableInfo = new TableMetadata
			{
				RowCount = rows.Count,
				ColumnCount = headers.Count,
				Headers = [.. headers],
				HasHeaderRow = headers.Count > 0,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			SerializedTable = SerializeTableToMarkdown(headers, rows, alignments),
			SerializationFormat = options.TableFormat,
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				SourceId = string.Empty,
				Hierarchy = "Table",
				Tags = ["table"],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		// Calculate quality metrics
		var allText = string.Join(" ", headers) + " " + string.Join(" ", rows.SelectMany(r => r));
		chunk.QualityMetrics = new ChunkQualityMetrics
		{
			TokenCount = _tokenCounter.CountTokens(allText),
			CharacterCount = allText.Length,
			WordCount = allText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
			SemanticCompleteness = 1.0
		};

		chunks.Add(chunk);
	}

	/// <summary>
	/// Extracts text from inline elements.
	/// </summary>
	private static string GetInlineText(ContainerInline? inline)
	{
		if (inline == null)
		{
			return string.Empty;
		}

		var text = new System.Text.StringBuilder();
		foreach (var child in inline)
		{
			if (child is LiteralInline literal)
			{
				_ = text.Append(literal.Content);
			}
			else if (child is LinkInline link && !link.IsImage)
			{
				_ = text.Append(GetInlineText(link));
			}
			else if (child is ContainerInline container)
			{
				_ = text.Append(GetInlineText(container));
			}
			else if (child is CodeInline code)
			{
				_ = text.Append(code.Content);
			}
		}

		return text.ToString();
	}

	/// <summary>
	/// Extracts annotations (links, formatting) from inline elements.
	/// </summary>
	private static List<ContentAnnotation> ExtractAnnotations(ContainerInline? inline)
	{
		var annotations = new List<ContentAnnotation>();

		if (inline == null)
		{
			return annotations;
		}

		var currentIndex = 0;

		foreach (var child in inline)
		{
			if (child is LinkInline link)
			{
				var linkText = GetInlineText(link);
				annotations.Add(new ContentAnnotation
				{
					StartIndex = currentIndex,
					Length = linkText.Length,
					Type = link.IsImage ? AnnotationType.Image : AnnotationType.Link,
					Attributes = new Dictionary<string, string>
					{
						["href"] = link.Url ?? string.Empty,
						["title"] = link.Title ?? string.Empty
					}
				});
				currentIndex += linkText.Length;
			}
			else if (child is EmphasisInline emphasis)
			{
				var emphasisText = GetInlineText(emphasis);
				annotations.Add(new ContentAnnotation
				{
					StartIndex = currentIndex,
					Length = emphasisText.Length,
					Type = emphasis.DelimiterCount == 1 ? AnnotationType.Italic : AnnotationType.Bold
				});
				currentIndex += emphasisText.Length;
			}
			else if (child is CodeInline code)
			{
				annotations.Add(new ContentAnnotation
				{
					StartIndex = currentIndex,
					Length = code.Content.Length,
					Type = AnnotationType.Code
				});
				currentIndex += code.Content.Length;
			}
			else if (child is LiteralInline literal)
			{
				currentIndex += literal.Content.ToString().Length;
			}
		}

		return annotations;
	}

	/// <summary>
	/// Gets text content from a block element.
	/// </summary>
	private static string GetBlockText(Block block)
	{
		var text = new System.Text.StringBuilder();

		if (block is LeafBlock leafBlock && leafBlock.Inline != null)
		{
			_ = text.Append(GetInlineText(leafBlock.Inline));
		}
		else if (block is ContainerBlock container)
		{
			foreach (var child in container)
			{
				_ = text.AppendLine(GetBlockText(child));
			}
		}

		return text.ToString().Trim();
	}

	/// <summary>
	/// Gets content from a code block.
	/// </summary>
	private static string GetCodeBlockContent(CodeBlock codeBlock)
	{
		var lines = codeBlock.Lines.Lines;
		if (lines == null || lines.Length == 0)
		{
			return string.Empty;
		}

		var text = new System.Text.StringBuilder();
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			_ = text.Append(line.Slice.ToString());
			if (i < lines.Length - 1)
			{
				_ = text.AppendLine();
			}
		}

		return text.ToString();
	}

	/// <summary>
	/// Extracts Markdown content for a block from the source.
	/// </summary>
	private static string ExtractMarkdownContent(Block block, string markdownContent)
	{
		if (block.Span.Start >= 0 && block.Span.End <= markdownContent.Length)
		{
			return markdownContent.Substring(block.Span.Start, block.Span.Length);
		}
		return string.Empty;
	}

	/// <summary>
	/// Serializes table to plain text.
	/// </summary>
	private static string SerializeTableToText(List<string> headers, List<List<string>> rows)
	{
		var text = new System.Text.StringBuilder();
		_ = text.AppendLine(string.Join(" | ", headers));
		foreach (var row in rows)
		{
			_ = text.AppendLine(string.Join(" | ", row));
		}
		return text.ToString();
	}

	/// <summary>
	/// Serializes table to Markdown format.
	/// </summary>
	private static string SerializeTableToMarkdown(
		List<string> headers,
		List<List<string>> rows,
		List<TableColumnAlignment> alignments)
	{
		var md = new System.Text.StringBuilder();

		// Headers
		_ = md.AppendLine($"| {string.Join(" | ", headers)} |");

		// Separator with alignment
		var separators = alignments.Count == headers.Count
			? alignments.Select(a => a switch
			{
				TableColumnAlignment.Left => ":---",
				TableColumnAlignment.Center => ":---:",
				TableColumnAlignment.Right => "---:",
				_ => "---"
			})
			: headers.Select(_ => "---");

		_ = md.AppendLine($"| {string.Join(" | ", separators)} |");

		// Rows
		foreach (var row in rows)
		{
			_ = md.AppendLine($"| {string.Join(" | ", row)} |");
		}

		return md.ToString();
	}

	/// <summary>
	/// Splits oversized chunks based on token limits.
	/// </summary>
	private async Task<List<ChunkerBase>> SplitOversizedChunksAsync(
		List<ChunkerBase> chunks,
		ChunkingOptions options,
		CancellationToken cancellationToken)
	{
		var result = new List<ChunkerBase>();

		foreach (var chunk in chunks)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (chunk.QualityMetrics?.TokenCount > options.MaxTokens)
			{
				// Only split content chunks, not structural ones
				if (chunk is ContentChunk contentChunk)
				{
					var splitChunks = SplitChunk(contentChunk, options);
					result.AddRange(splitChunks);
				}
				else
				{
					result.Add(chunk);
				}
			}
			else
			{
				result.Add(chunk);
			}
		}

		return result;
	}

	/// <summary>
	/// Splits a single content chunk into smaller chunks.
	/// </summary>
	private List<ChunkerBase> SplitChunk(
		ContentChunk chunk,
		ChunkingOptions options)
	{
		var text = chunk.Content ?? string.Empty;
		var sentences = text.Split([". ", ".\n", "! ", "!\n", "? ", "?\n"], StringSplitOptions.RemoveEmptyEntries);

		var result = new List<ChunkerBase>();
		var currentText = new System.Text.StringBuilder();
		var currentTokenCount = 0;

		foreach (var sentence in sentences)
		{
			var sentenceTokens = _tokenCounter.CountTokens(sentence);

			if (currentTokenCount + sentenceTokens > options.MaxTokens && currentText.Length > 0)
			{
				// Create a new chunk
				result.Add(CreateSplitChunk(chunk, currentText.ToString(), currentTokenCount));
				_ = currentText.Clear();
				currentTokenCount = 0;
			}

			_ = currentText.Append(sentence).Append(". ");
			currentTokenCount += sentenceTokens;
		}

		// Add remaining text
		if (currentText.Length > 0)
		{
			result.Add(CreateSplitChunk(chunk, currentText.ToString(), currentTokenCount));
		}

		return result.Count > 0 ? result : [chunk];
	}

	/// <summary>
	/// Creates a split chunk from the original chunk.
	/// </summary>
	private MarkdownParagraphChunk CreateSplitChunk(ContentChunk original, string text, int tokenCount)
	{
		var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

		return original switch
		{
			MarkdownParagraphChunk => new MarkdownParagraphChunk
			{
				Content = text,
				MarkdownContent = text,
				ParentId = original.ParentId,
				SequenceNumber = _sequenceNumber++,
				Metadata = new ChunkMetadata
				{
					DocumentType = "Markdown",
					SourceId = string.Empty,
					Hierarchy = $"{original.Metadata?.Hierarchy} (Split)",
					Tags = original.Metadata?.Tags ?? [],
					CreatedAt = DateTimeOffset.UtcNow
				},
				QualityMetrics = new ChunkQualityMetrics
				{
					TokenCount = tokenCount,
					CharacterCount = text.Length,
					WordCount = wordCount,
					SemanticCompleteness = 0.5,
					WasSplit = true
				}
			},
			_ => new MarkdownParagraphChunk
			{
				Content = text,
				MarkdownContent = text,
				ParentId = original.ParentId,
				SequenceNumber = _sequenceNumber++,
				Metadata = new ChunkMetadata
				{
					DocumentType = "Markdown",
					SourceId = string.Empty,
					Hierarchy = $"{original.SpecificType} (Split)",
					Tags = ["split"],
					CreatedAt = DateTimeOffset.UtcNow
				},
				QualityMetrics = new ChunkQualityMetrics
				{
					TokenCount = tokenCount,
					CharacterCount = text.Length,
					WordCount = wordCount,
					SemanticCompleteness = 0.5,
					WasSplit = true
				}
			}
		};
	}

	/// <summary>
	/// Calculates statistics for the chunking result.
	/// </summary>
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

	/// <summary>
	/// Validates the chunks for consistency.
	/// </summary>
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
}
