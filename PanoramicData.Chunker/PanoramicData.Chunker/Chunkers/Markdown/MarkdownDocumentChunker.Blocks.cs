using Markdig.Syntax;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

public partial class MarkdownDocumentChunker
{
	private void ProcessHeading(
		HeadingBlock heading,
		List<ChunkerBase> chunks,
		Stack<(int Level, Guid Id)> headerStack)
	{
		var headingText = MarkdownInlineText.GetInlineText(heading.Inline);
		var markdownSyntax = $"{new string('#', heading.Level)} {headingText}";

		var chunk = new MarkdownSectionChunk
		{
			HeadingLevel = heading.Level,
			HeadingText = headingText,
			MarkdownSyntax = markdownSyntax,
			SequenceNumber = _sequenceNumber++,
			SpecificType = $"Heading{heading.Level}",
			Metadata = CreateMetadata(markdownSyntax, [$"h{heading.Level}"]),
			QualityMetrics = CreateMetrics(headingText, 1.0)
		};

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

	private void ProcessParagraph(
		ParagraphBlock paragraph,
		string markdownContent,
		List<ChunkerBase> chunks,
		Guid? parentId)
	{
		var text = MarkdownInlineText.GetInlineText(paragraph.Inline);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var annotations = MarkdownInlineText.ExtractAnnotations(paragraph.Inline);

		chunks.Add(new MarkdownParagraphChunk
		{
			Content = text,
			MarkdownContent = MarkdownInlineText.ExtractMarkdownContent(paragraph, markdownContent),
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			Annotations = annotations.Count > 0 ? annotations : null,
			Metadata = CreateMetadata("Paragraph", ["paragraph"]),
			QualityMetrics = CreateMetrics(text, 1.0)
		});
	}

	private void ProcessList(
		ListBlock list,
		List<ChunkerBase> chunks,
		Guid? parentId,
		int nestingLevel = 0)
	{
		foreach (var listItem in list.OfType<ListItemBlock>())
		{
			var text = MarkdownInlineText.GetBlockText(listItem);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}

			var chunk = CreateListItemChunk(list, listItem, text, parentId, nestingLevel);
			chunks.Add(chunk);

			foreach (var nestedList in listItem.OfType<ListBlock>())
			{
				ProcessList(nestedList, chunks, chunk.Id, nestingLevel + 1);
			}
		}
	}

	private MarkdownListItemChunk CreateListItemChunk(
		ListBlock list,
		ListItemBlock listItem,
		string text,
		Guid? parentId,
		int nestingLevel) => new()
		{
			Content = text,
			MarkdownContent = list.IsOrdered ? $"{listItem.Order}. {text}" : $"- {text}",
			IsOrdered = list.IsOrdered,
			ItemNumber = list.IsOrdered ? listItem.Order : null,
			NestingLevel = nestingLevel,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			Metadata = CreateMetadata(
				list.IsOrdered ? "Ordered List Item" : "Unordered List Item",
				["list-item", list.IsOrdered ? "ordered" : "unordered"]),
			QualityMetrics = CreateMetrics(text, 0.8)
		};

	private void ProcessCodeBlock(
		CodeBlock codeBlock,
		List<ChunkerBase> chunks,
		Guid? parentId)
	{
		var code = MarkdownInlineText.GetCodeBlockContent(codeBlock);
		if (string.IsNullOrWhiteSpace(code))
		{
			return;
		}

		var fenced = codeBlock as FencedCodeBlock;
		var language = fenced?.Info;
		var isFenced = fenced != null;

		chunks.Add(new MarkdownCodeBlockChunk
		{
			Content = code,
			MarkdownContent = isFenced ? $"```{language}\n{code}\n```" : code,
			Language = language,
			IsFenced = isFenced,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			Metadata = CreateMetadata($"Code Block ({language ?? "plain"})", ["code", language ?? "plain"]),
			QualityMetrics = CreateMetrics(code, code.Split('\n').Length, 1.0)
		});
	}

	private void ProcessQuoteBlock(
		QuoteBlock quoteBlock,
		List<ChunkerBase> chunks,
		Guid? parentId)
	{
		var text = MarkdownInlineText.GetBlockText(quoteBlock);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		chunks.Add(new MarkdownQuoteChunk
		{
			Content = text,
			MarkdownContent = $"> {text.Replace("\n", "\n> ")}",
			NestingLevel = 0,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			Metadata = CreateMetadata("Blockquote", ["blockquote"]),
			QualityMetrics = CreateMetrics(text, 0.9)
		});
	}

	private void ProcessTable(
		Markdig.Extensions.Tables.Table table,
		List<ChunkerBase> chunks,
		Guid? parentId,
		ChunkingOptions options)
	{
		var content = MarkdownTableContent.Read(table);
		var markdown = content.ToMarkdown();

		chunks.Add(new MarkdownTableChunk
		{
			Content = content.ToText(),
			MarkdownContent = markdown,
			ColumnAlignments = content.Alignments,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			TableInfo = new TableMetadata
			{
				RowCount = content.Rows.Count,
				ColumnCount = content.Headers.Count,
				Headers = [.. content.Headers],
				HasHeaderRow = content.Headers.Count > 0,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			SerializedTable = markdown,
			SerializationFormat = options.TableFormat,
			Metadata = CreateMetadata("Table", ["table"]),
			QualityMetrics = CreateMetrics(content.AllText(), 1.0)
		});
	}

	private ChunkQualityMetrics CreateMetrics(string text, double completeness)
		=> CreateMetrics(text, CountWords(text), completeness);

	private ChunkQualityMetrics CreateMetrics(string text, int wordCount, double completeness) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = wordCount,
		SemanticCompleteness = completeness
	};

	private static int CountWords(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

	private static ChunkMetadata CreateMetadata(string hierarchy, IEnumerable<string> tags) => new()
	{
		DocumentType = "Markdown",
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		CreatedAt = DateTimeOffset.UtcNow
	};
}
