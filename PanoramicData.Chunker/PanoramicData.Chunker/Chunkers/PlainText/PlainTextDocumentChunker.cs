using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.PlainText;

/// <summary>
/// Chunks plain text documents using heuristic-based structure detection.
/// </summary>
public partial class PlainTextDocumentChunker : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter;
	private readonly ILogger<PlainTextDocumentChunker>? _logger;
	private int _sequenceNumber;

	/// <summary>
	/// Initializes a new instance of the <see cref="PlainTextDocumentChunker"/> class.
	/// </summary>
	/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
	/// <param name="logger">Optional logger for diagnostic information.</param>
	public PlainTextDocumentChunker(ITokenCounter tokenCounter, ILogger<PlainTextDocumentChunker>? logger = null)
	{
		_tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
		_logger = logger;
	}

	/// <summary>
	/// Gets the supported document type.
	/// </summary>
	public DocumentType SupportedType => DocumentType.PlainText;

	/// <summary>
	/// Validates if the stream contains plain text.
	/// </summary>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
	{
		if (documentStream == null || !documentStream.CanRead)
		{
			return false;
		}

		try
		{
			var originalPosition = documentStream.Position;
			documentStream.Position = 0;

			using var reader = new StreamReader(documentStream, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
			
			// Read first 1KB to check
			var buffer = new char[1024];
			var bytesRead = await reader.ReadAsync(buffer, cancellationToken);
			
			if (bytesRead == 0)
			{
				return false;
			}

			var sample = new string(buffer, 0, bytesRead);

			// Check if it's NOT HTML (no tags)
			if (sample.Contains('<') && sample.Contains('>') && HtmlTagRegex().IsMatch(sample))
			{
				return false;
			}

			// Check if it's NOT Markdown (minimal markdown-specific syntax)
			if (HasMarkdownSyntax(sample))
			{
				return false;
			}

			// Must be primarily printable ASCII/UTF-8
			var printableRatio = sample.Count(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t') / (double)sample.Length;
			
			documentStream.Position = originalPosition;
			return printableRatio > 0.9; // At least 90% printable
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Chunks a plain text document.
	/// </summary>
	public async Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken = default)
	{
		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;

		try
		{
			// Read and normalize the text
			var text = await ReadAndNormalizeAsync(documentStream, cancellationToken);

			if (string.IsNullOrWhiteSpace(text))
			{
				return CreateEmptyResult(startTime);
			}

			// Split into lines for processing
			var lines = text.Split('\n');

			// Process lines to detect structure and create chunks
			var chunks = ProcessLines(lines, options);

			// Build hierarchy
			HierarchyBuilder.BuildHierarchy(chunks);

			// Apply output format
			var finalChunks = ApplyOutputFormat(chunks, options.OutputFormat);

			// Validate if requested
			ValidationResult? validationResult = null;
			if (options.ValidateChunks)
			{
				validationResult = ValidateChunks(finalChunks);
			}

			return new ChunkingResult
			{
				Chunks = finalChunks,
				Statistics = CalculateStatistics(finalChunks, startTime),
				Success = true,
				ValidationResult = validationResult
			};
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error chunking plain text document");
			
			return new ChunkingResult
			{
				Chunks = [],
				Statistics = CalculateStatistics([], startTime),
				Success = false,
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk document: {ex.Message}",
						Code = "PLAINTEXT_CHUNKING_ERROR"
					}
				]
			};
		}
	}

	/// <summary>
	/// Reads and normalizes the text from the stream.
	/// </summary>
	private static async Task<string> ReadAndNormalizeAsync(Stream stream, CancellationToken cancellationToken)
	{
		stream.Position = 0;
		using var reader = new StreamReader(stream, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		var text = await reader.ReadToEndAsync(cancellationToken);

		// Normalize line endings to \n
		text = text.Replace("\r\n", "\n").Replace("\r", "\n");

		return text;
	}

	/// <summary>
	/// Processes lines to detect structure and create chunks.
	/// </summary>
	private List<ChunkerBase> ProcessLines(string[] lines, ChunkingOptions options)
	{
		var chunks = new List<ChunkerBase>();
		var headerStack = new Stack<(int Level, Guid Id)>();
		var i = 0;
		string? previousLine = null;
		ChunkerBase? previousChunk = null;

		while (i < lines.Length)
		{
			var line = lines[i];

			// Skip empty lines
			if (string.IsNullOrWhiteSpace(line))
			{
				previousLine = line;
				// Don't reset previousChunk - allow list continuations across empty lines
				i++;
				continue;
			}

			// Try to detect heading
			var heading = DetectHeading(lines, ref i, previousLine, previousChunk);
			if (heading != null)
			{
				// Update header stack
				while (headerStack.Count > 0 && headerStack.Peek().Level >= heading.HeadingLevel)
				{
					headerStack.Pop();
				}

				heading.ParentId = headerStack.Count > 0 ? headerStack.Peek().Id : null;
				heading.Depth = headerStack.Count;
				heading.SequenceNumber = _sequenceNumber++;
				
				headerStack.Push((heading.HeadingLevel, heading.Id));
				chunks.Add(heading);
				previousLine = line;
				previousChunk = heading;
				continue;
			}

			// Try to detect list item
			var listItem = DetectListItem(line, previousLine, previousChunk);
			if (listItem != null)
			{
				listItem.ParentId = headerStack.Count > 0 ? headerStack.Peek().Id : null;
				listItem.Depth = headerStack.Count + 1;
				listItem.SequenceNumber = _sequenceNumber++;
				PopulateQualityMetrics(listItem, listItem.Content);
				chunks.Add(listItem);
				previousLine = line;
				previousChunk = listItem;
				i++;
				continue;
			}

			// Try to detect code block
			var codeBlock = DetectCodeBlock(lines, ref i);
			if (codeBlock != null)
			{
				codeBlock.ParentId = headerStack.Count > 0 ? headerStack.Peek().Id : null;
				codeBlock.Depth = headerStack.Count + 1;
				codeBlock.SequenceNumber = _sequenceNumber++;
				PopulateQualityMetrics(codeBlock, codeBlock.Content);
				chunks.Add(codeBlock);
				previousLine = line;
				previousChunk = codeBlock;
				continue;
			}

			// Default: treat as paragraph
			var paragraph = DetectParagraph(lines, ref i);
			if (paragraph != null)
			{
				paragraph.ParentId = headerStack.Count > 0 ? headerStack.Peek().Id : null;
				paragraph.Depth = headerStack.Count + 1;
				paragraph.SequenceNumber = _sequenceNumber++;
				PopulateQualityMetrics(paragraph, paragraph.Content);
				chunks.Add(paragraph);
				// Keep the last line before this paragraph for context (for list detection)
				previousLine = i > 0 ? lines[i - 1] : null;
				previousChunk = paragraph;
			}
		}

		return chunks;
	}

	/// <summary>
	/// Detects a heading using various heuristics.
	/// </summary>
	private PlainTextSectionChunk? DetectHeading(string[] lines, ref int index, string? previousLine, ChunkerBase? previousChunk)
	{
		var line = lines[index];

		// Try underlined heading first (highest confidence)
		if (index + 1 < lines.Length)
		{
			var nextLine = lines[index + 1];
			var underlineHeading = DetectUnderlinedHeading(line, nextLine);
			if (underlineHeading != null)
			{
				index += 2; // Skip heading and underline
				return underlineHeading;
			}
		}

		// Try numbered section (but check context)
		var numberedHeading = DetectNumberedHeading(line, previousLine, previousChunk);
		if (numberedHeading != null)
		{
			index++;
			return numberedHeading;
		}

		// Try ALL CAPS
		var allCapsHeading = DetectAllCapsHeading(line);
		if (allCapsHeading != null)
		{
			index++;
			return allCapsHeading;
		}

		// Try prefixed (#, ##, ###)
		var prefixedHeading = DetectPrefixedHeading(line);
		if (prefixedHeading != null)
		{
			index++;
			return prefixedHeading;
		}

		return null;
	}

	/// <summary>
	/// Detects underlined headings (=== or ---).
	/// </summary>
	private PlainTextSectionChunk? DetectUnderlinedHeading(string line, string nextLine)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(nextLine))
		{
			return null;
		}

		var trimmedLine = line.Trim();
		var trimmedNext = nextLine.Trim();

		// Check if next line is all = or -
		if (trimmedNext.All(c => c == '=') && trimmedNext.Length >= 3)
		{
			// Double underline = level 1
			return CreateSectionChunk(trimmedLine, 1, HeadingHeuristic.Underlined, 0.95);
		}

		if (trimmedNext.All(c => c == '-') && trimmedNext.Length >= 3)
		{
			// Single underline = level 2
			return CreateSectionChunk(trimmedLine, 2, HeadingHeuristic.Underlined, 0.90);
		}

		return null;
	}

	/// <summary>
	/// Detects numbered section headings (1., 1.1, 1.1.1).
	/// </summary>
	private PlainTextSectionChunk? DetectNumberedHeading(string line, string? previousLine, ChunkerBase? previousChunk)
	{
		var trimmed = line.Trim();
		var match = NumberedSectionRegex().Match(trimmed);
		if (!match.Success)
		{
			return null;
		}

		var numbering = match.Groups["numbering"].Value;
		var text = match.Groups["text"].Value.Trim();

		// Count depth by number of dots
		var level = numbering.Count(c => c == '.') + 1;
		
		// For single number (e.g., "1."), only treat as heading if context suggests heading
		if (level == 1)
		{
			// If previous line ends with colon, this is likely a list item, not a heading
			if (previousLine != null && previousLine.Trim().EndsWith(':'))
			{
				return null;
			}
			
			// If previous chunk was a list item, this is continuing a list, not a heading
			if (previousChunk is PlainTextListItemChunk)
			{
				return null;
			}
			
			// Must start with capital letter
			if (!char.IsUpper(text.FirstOrDefault()))
			{
				return null;
			}
			
			// Should be reasonably short (headings are typically < 100 chars)
			if (text.Length > 100)
			{
				return null;
			}
			
			// Should not end with common sentence endings
			if (text.TrimEnd().EndsWith('.') || text.TrimEnd().EndsWith('!') || text.TrimEnd().EndsWith('?'))
			{
				return null;
			}
		}
		
		level = Math.Min(level, 6); // Cap at 6

		return CreateSectionChunk(text, level, HeadingHeuristic.Numbered, 0.85);
	}

	/// <summary>
	/// Detects ALL CAPS headings.
	/// </summary>
	private PlainTextSectionChunk? DetectAllCapsHeading(string line)
	{
		var trimmed = line.Trim();

		// Must be at least 4 characters
		if (trimmed.Length < 4)
		{
			return null;
		}

		// Must be shorter than 100 characters (probably not a heading otherwise)
		if (trimmed.Length > 100)
		{
			return null;
		}

		// Must be all uppercase letters (allowing spaces and punctuation)
		var letters = trimmed.Where(char.IsLetter).ToArray();
		if (letters.Length == 0 || !letters.All(char.IsUpper))
		{
			return null;
		}

		// At least 50% of characters should be letters
		if (letters.Length < trimmed.Length * 0.5)
		{
			return null;
		}

		return CreateSectionChunk(trimmed, 1, HeadingHeuristic.AllCaps, 0.70);
	}

	/// <summary>
	/// Detects prefixed headings (#, ##, ###).
	/// </summary>
	private PlainTextSectionChunk? DetectPrefixedHeading(string line)
	{
		var match = PrefixedHeadingRegex().Match(line.Trim());
		if (!match.Success)
		{
			return null;
		}

		var prefix = match.Groups["prefix"].Value;
		var text = match.Groups["text"].Value.Trim();

		var level = Math.Min(prefix.Length, 6);

		return CreateSectionChunk(text, level, HeadingHeuristic.Prefixed, 0.75);
	}

	/// <summary>
	/// Creates a section chunk.
	/// </summary>
	private PlainTextSectionChunk CreateSectionChunk(string headingText, int level, HeadingHeuristic heuristic, double confidence)
	{
		var chunk = new PlainTextSectionChunk
		{
			Id = Guid.NewGuid(),
			HeadingText = headingText,
			HeadingLevel = level,
			HeadingType = heuristic,
			Confidence = confidence,
			SpecificType = $"Heading{level}",
			Metadata = new ChunkMetadata
			{
				DocumentType = "PlainText",
				SourceId = string.Empty,
				Hierarchy = headingText,
				Tags = ["heading", $"level{level}", heuristic.ToString().ToLowerInvariant()],
				CreatedAt = DateTimeOffset.UtcNow
			}
		};

		PopulateQualityMetrics(chunk, headingText);
		return chunk;
	}

	/// <summary>
	/// Detects list items.
	/// </summary>
	private PlainTextListItemChunk? DetectListItem(string line, string? previousLine, ChunkerBase? previousChunk)
	{
		var trimmed = line.TrimStart();
		var indentLevel = line.Length - trimmed.Length;

		// Bullet list
		var bulletMatch = BulletListRegex().Match(trimmed);
		if (bulletMatch.Success)
		{
			var marker = bulletMatch.Groups["marker"].Value;
			var text = bulletMatch.Groups["text"].Value.Trim();

			return new PlainTextListItemChunk
			{
				Id = Guid.NewGuid(),
				Content = text,
				ListType = "bullet",
				Marker = marker,
				NestingLevel = indentLevel / 2, // 2 spaces per level
				IsOrdered = false,
				SpecificType = "ListItem",
				Metadata = CreateMetadata("list-item", "bullet")
			};
		}

		// Numbered list 
		var numberedMatch = NumberedListRegex().Match(trimmed);
		if (numberedMatch.Success)
		{
			var marker = numberedMatch.Groups["marker"].Value;
			var text = numberedMatch.Groups["text"].Value.Trim();
			
			// If previous line ends with colon, definitely a list item
			var previousEndsWithColon = previousLine != null && previousLine.Trim().EndsWith(':');
			
			// If previous chunk was a list item, continue the list
			var previousWasListItem = previousChunk is PlainTextListItemChunk;
			
			// If previous line ends with colon or we're continuing a list, definitely treat as list
			if (previousEndsWithColon || previousWasListItem)
			{
				return new PlainTextListItemChunk
				{
					Id = Guid.NewGuid(),
					Content = text,
					ListType = "numbered",
					Marker = marker,
					NestingLevel = indentLevel / 2,
					IsOrdered = true,
					SpecificType = "ListItem",
					Metadata = CreateMetadata("list-item", "numbered")
				};
			}
			
			// Otherwise, check if it looks more like a heading than a list item
			// (only for patterns like "1." without parenthesis)
			if (!marker.Contains(')'))
			{
				var looksLikeHeading = char.IsUpper(text.FirstOrDefault()) &&
									   text.Length < 100 &&
									   !text.TrimEnd().EndsWith('.') &&
									   !text.TrimEnd().EndsWith('!') &&
									   !text.TrimEnd().EndsWith('?');
				
				if (looksLikeHeading)
				{
					// Let the heading detector handle this
					return null;
				}
			}

			// Treat as list item
			return new PlainTextListItemChunk
			{
				Id = Guid.NewGuid(),
				Content = text,
				ListType = "numbered",
				Marker = marker,
				NestingLevel = indentLevel / 2,
				IsOrdered = true,
				SpecificType = "ListItem",
				Metadata = CreateMetadata("list-item", "numbered")
			};
		}

		return null;
	}

	/// <summary>
	/// Detects code blocks.
	/// </summary>
	private PlainTextCodeBlockChunk? DetectCodeBlock(string[] lines, ref int index)
	{
		var line = lines[index];

		// Check for fenced code block
		if (line.Trim().StartsWith("```"))
		{
			return DetectFencedCodeBlock(lines, ref index);
		}

		// Check for indented code block
		return DetectIndentedCodeBlock(lines, ref index);
	}

	/// <summary>
	/// Detects fenced code blocks (```).
	/// </summary>
	private PlainTextCodeBlockChunk? DetectFencedCodeBlock(string[] lines, ref int index)
	{
		var startLine = lines[index].Trim();
		if (!startLine.StartsWith("```"))
		{
			return null;
		}

		// Extract language if specified
		var language = startLine.Length > 3 ? startLine[3..].Trim() : null;

		var codeLines = new List<string>();
		index++; // Skip opening fence

		// Read until closing fence
		while (index < lines.Length)
		{
			if (lines[index].Trim().StartsWith("```"))
			{
				index++; // Skip closing fence
				break;
			}

			codeLines.Add(lines[index]);
			index++;
		}

		var code = string.Join("\n", codeLines);

		return new PlainTextCodeBlockChunk
		{
			Id = Guid.NewGuid(),
			Content = code,
			Language = language,
			IsFenced = true,
			IndentationLevel = 0,
			SpecificType = "CodeBlock",
			Metadata = CreateMetadata("code", language ?? "unknown")
		};
	}

	/// <summary>
	/// Detects indented code blocks.
	/// </summary>
	private PlainTextCodeBlockChunk? DetectIndentedCodeBlock(string[] lines, ref int index)
	{
		var line = lines[index];
		var indent = GetIndentation(line);

		// Must be indented at least 4 spaces or 1 tab
		if (indent < 4)
		{
			return null;
		}

		var codeLines = new List<string>();
		var startIndex = index;

		// Collect consecutive indented lines
		while (index < lines.Length)
		{
			var currentLine = lines[index];
			var currentIndent = GetIndentation(currentLine);

			// Empty line is okay
			if (string.IsNullOrWhiteSpace(currentLine))
			{
				codeLines.Add(string.Empty);
				index++;
				continue;
			}

			// Must maintain indentation
			if (currentIndent < indent)
			{
				break;
			}

			codeLines.Add(currentLine[indent..]);
			index++;
		}

		// Need at least 2 lines for code block
		if (codeLines.Count < 2)
		{
			index = startIndex;
			return null;
		}

		var code = string.Join("\n", codeLines);

		return new PlainTextCodeBlockChunk
		{
			Id = Guid.NewGuid(),
			Content = code,
			Language = null,
			IsFenced = false,
			IndentationLevel = indent,
			CodeIndicators = DetectCodeIndicators(code),
			SpecificType = "CodeBlock",
			Metadata = CreateMetadata("code", "indented")
		};
	}

	/// <summary>
	/// Detects paragraphs.
	/// </summary>
	private PlainTextParagraphChunk? DetectParagraph(string[] lines, ref int index)
	{
		var paragraphLines = new List<string>();
		var startIndex = index;

		while (index < lines.Length)
		{
			var line = lines[index];

			// Stop at empty line
			if (string.IsNullOrWhiteSpace(line))
			{
				break;
			}

			// Stop at potential heading or list
			if (index > startIndex && (
				LooksLikeHeading(line) ||
				BulletListRegex().IsMatch(line.TrimStart()) ||
				NumberedListRegex().IsMatch(line.TrimStart()) ||
				GetIndentation(line) >= 4))
			{
				break;
			}

			paragraphLines.Add(line.Trim());
			index++;
		}

		if (paragraphLines.Count == 0)
		{
			return null;
		}

		var text = string.Join(" ", paragraphLines);

		return new PlainTextParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = text,
			DetectionMethod = ParagraphDetection.DoubleNewline,
			SpecificType = "Paragraph",
			Metadata = CreateMetadata("paragraph")
		};
	}

	// Helper methods

	private static bool LooksLikeHeading(string line)
	{
		var trimmed = line.Trim();
		
		// ALL CAPS
		var letters = trimmed.Where(char.IsLetter).ToArray();
		if (letters.Length >= 4 && letters.All(char.IsUpper))
		{
			return true;
		}

		// Numbered section
		if (NumberedSectionRegex().IsMatch(trimmed))
		{
			return true;
		}

		// Prefixed
		if (PrefixedHeadingRegex().IsMatch(trimmed))
		{
			return true;
		}

		return false;
	}

	private static int GetIndentation(string line)
	{
		var count = 0;
		foreach (var c in line)
		{
			if (c == ' ')
			{
				count++;
			}
			else if (c == '\t')
			{
				count += 4; // Tab = 4 spaces
			}
			else
			{
				break;
			}
		}
		return count;
	}

	private static List<string> DetectCodeIndicators(string code)
	{
		var indicators = new List<string>();
		var keywords = new[] { "public", "private", "class", "function", "def", "var", "const", "return", "if", "for", "while" };

		foreach (var keyword in keywords)
		{
			if (code.Contains(keyword, StringComparison.OrdinalIgnoreCase))
			{
				indicators.Add(keyword);
			}
		}

		return indicators;
	}

	private static bool HasMarkdownSyntax(string text)
	{
		// Check for strong Markdown indicators
		return text.Contains("**") || text.Contains("__") ||
			   text.Contains("```") || text.Contains("![](") ||
			   text.Contains("[](") || text.Contains("| ") ||
			   (text.Contains('#') && text.Contains('\n'));
	}

	private void PopulateQualityMetrics(ChunkerBase chunk, string text)
	{
		chunk.QualityMetrics = new ChunkQualityMetrics
		{
			TokenCount = _tokenCounter.CountTokens(text),
			CharacterCount = text.Length,
			WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
			SemanticCompleteness = 1.0
		};
	}

	private ChunkMetadata CreateMetadata(params string[] tags)
	{
		return new ChunkMetadata
		{
			DocumentType = "PlainText",
			SourceId = string.Empty,
			Hierarchy = string.Empty,
			Tags = [.. tags],
			CreatedAt = DateTimeOffset.UtcNow
		};
	}

	private static List<ChunkerBase> ApplyOutputFormat(List<ChunkerBase> chunks, OutputFormat format)
	{
		return format switch
		{
			OutputFormat.Flat => chunks,
			OutputFormat.Hierarchical => BuildHierarchicalStructure(chunks),
			OutputFormat.LeavesOnly => chunks.Where(c => c is ContentChunk).ToList(),
			_ => chunks
		};
	}

	private static List<ChunkerBase> BuildHierarchicalStructure(List<ChunkerBase> flatChunks)
	{
		// Build parent-child relationships
		var chunkDict = flatChunks.ToDictionary(c => c.Id);
		
		foreach (var chunk in flatChunks.OfType<StructuralChunk>())
		{
		chunk.Children.Clear();
			chunk.Children.AddRange(flatChunks.Where(c => c.ParentId == chunk.Id));
		}

		// Return only root chunks
		return flatChunks.Where(c => c.ParentId == null).ToList();
	}

	private static ChunkingResult CreateEmptyResult(DateTime startTime)
	{
		return new ChunkingResult
		{
			Chunks = [],
			Statistics = CalculateStatistics([], startTime),
			Success = true
		};
	}

	private static ChunkingStatistics CalculateStatistics(List<ChunkerBase> chunks, DateTime startTime)
	{
		var processingTime = DateTime.UtcNow - startTime;

		return new ChunkingStatistics
		{
			TotalChunks = chunks.Count,
			StructuralChunks = chunks.OfType<StructuralChunk>().Count(),
			ContentChunks = chunks.OfType<ContentChunk>().Count(),
			VisualChunks = 0,
			TableChunks = 0,
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

		// Check for orphaned chunks
		var chunkIds = chunks.Select(c => c.Id).ToHashSet();
		foreach (var chunk in chunks.Where(c => c.ParentId.HasValue))
		{
			if (!chunkIds.Contains(chunk.ParentId!.Value))
			{
				issues.Add(new ValidationIssue
				{
					Severity = ValidationSeverity.Warning,
					Message = $"Chunk {chunk.Id} has orphaned parent reference",
					ChunkId = chunk.Id
				});
			}
		}

		return new ValidationResult
		{
			IsValid = issues.Count == 0,
			Issues = issues
		};
	}

	// Regex patterns
	[GeneratedRegex(@"^(?<numbering>\d+(\.\d+)*)\.?\s+(?<text>.+)$")]
	private static partial Regex NumberedSectionRegex();

	[GeneratedRegex(@"^(?<prefix>#{1,6})\s+(?<text>.+)$")]
	private static partial Regex PrefixedHeadingRegex();

	[GeneratedRegex(@"^(?<marker>[-*•])\s+(?<text>.+)$")]
	private static partial Regex BulletListRegex();

	[GeneratedRegex(@"^(?<marker>\d+[.)])\s+(?<text>.+)$")]
	private static partial Regex NumberedListRegex();

	[GeneratedRegex(@"<[a-zA-Z][^>]*>")]
	private static partial Regex HtmlTagRegex();
}
