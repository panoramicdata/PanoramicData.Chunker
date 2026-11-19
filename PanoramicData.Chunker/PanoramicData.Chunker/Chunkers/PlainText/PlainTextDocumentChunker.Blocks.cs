using PanoramicData.Chunker.Models;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.PlainText;

public partial class PlainTextDocumentChunker
{
	private static readonly string[] CodeKeywords = ["public", "private", "class", "function", "def", "var", "const", "return", "if", "for", "while"];

	private static PlainTextListItemChunk? DetectListItem(string line, string? previousLine, ChunkerBase? previousChunk)
	{
		var trimmed = line.TrimStart();
		var indentLevel = line.Length - trimmed.Length;

		var bulletMatch = BulletListRegex().Match(trimmed);
		if (bulletMatch.Success)
		{
			return CreateListItem(bulletMatch, indentLevel, false);
		}

		var numberedMatch = NumberedListRegex().Match(trimmed);
		if (!numberedMatch.Success || DefersToHeadingDetector(numberedMatch, previousLine, previousChunk))
		{
			return null;
		}

		return CreateListItem(numberedMatch, indentLevel, true);
	}

	private static PlainTextListItemChunk CreateListItem(Match match, int indentLevel, bool ordered)
	{
		var listType = ordered ? "numbered" : "bullet";

		return new PlainTextListItemChunk
		{
			Id = Guid.NewGuid(),
			Content = match.Groups["text"].Value.Trim(),
			ListType = listType,
			Marker = match.Groups["marker"].Value,
			NestingLevel = indentLevel / 2,
			IsOrdered = ordered,
			SpecificType = "ListItem",
			Metadata = CreateMetadata("list-item", listType)
		};
	}

	private static bool DefersToHeadingDetector(Match match, string? previousLine, ChunkerBase? previousChunk)
	{
		if (EndsWithColon(previousLine) || previousChunk is PlainTextListItemChunk)
		{
			return false;
		}

		return !match.Groups["marker"].Value.Contains(')')
			&& LooksLikeHeadingText(match.Groups["text"].Value.Trim(), 99);
	}

	private static PlainTextCodeBlockChunk? DetectCodeBlock(string[] lines, ref int index)
		=> lines[index].Trim().StartsWith("```")
			? DetectFencedCodeBlock(lines, ref index)
			: DetectIndentedCodeBlock(lines, ref index);

	private static PlainTextCodeBlockChunk DetectFencedCodeBlock(string[] lines, ref int index)
	{
		var startLine = lines[index].Trim();
		var language = startLine.Length > 3 ? startLine[3..].Trim() : null;

		var codeLines = new List<string>();
		index++;

		while (index < lines.Length)
		{
			if (lines[index].Trim().StartsWith("```"))
			{
				index++;
				break;
			}

			codeLines.Add(lines[index]);
			index++;
		}

		return new PlainTextCodeBlockChunk
		{
			Id = Guid.NewGuid(),
			Content = string.Join("\n", codeLines),
			Language = language,
			IsFenced = true,
			IndentationLevel = 0,
			SpecificType = "CodeBlock",
			Metadata = CreateMetadata("code", language ?? "unknown")
		};
	}

	private static PlainTextCodeBlockChunk? DetectIndentedCodeBlock(string[] lines, ref int index)
	{
		var indent = GetIndentation(lines[index]);

		if (indent < 4)
		{
			return null;
		}

		var startIndex = index;
		var codeLines = CollectIndentedLines(lines, ref index, indent);

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

	private static PlainTextParagraphChunk? DetectParagraph(string[] lines, ref int index)
	{
		var paragraphLines = new List<string>();
		var startIndex = index;

		while (index < lines.Length)
		{
			var line = lines[index];

			if (string.IsNullOrWhiteSpace(line) || (index > startIndex && StartsNewBlock(line)))
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

		return new PlainTextParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = string.Join(" ", paragraphLines),
			DetectionMethod = ParagraphDetection.DoubleNewline,
			SpecificType = "Paragraph",
			Metadata = CreateMetadata("paragraph")
		};
	}

	private static List<string> CollectIndentedLines(string[] lines, ref int index, int indent)
	{
		var codeLines = new List<string>();

		while (index < lines.Length)
		{
			var currentLine = lines[index];

			if (string.IsNullOrWhiteSpace(currentLine))
			{
				codeLines.Add(string.Empty);
			}
			else if (GetIndentation(currentLine) < indent)
			{
				break;
			}
			else
			{
				codeLines.Add(currentLine[indent..]);
			}

			index++;
		}

		return codeLines;
	}

	private void PopulateQualityMetrics(ChunkerBase chunk, string text) => chunk.QualityMetrics = new ChunkQualityMetrics
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
		SemanticCompleteness = 1.0
	};
	private static bool StartsNewBlock(string line)
		=> LooksLikeHeading(line)
			|| BulletListRegex().IsMatch(line.TrimStart())
			|| NumberedListRegex().IsMatch(line.TrimStart())
			|| GetIndentation(line) >= 4;

	private static bool LooksLikeHeading(string line)
	{
		var trimmed = line.Trim();

		var letters = trimmed.Where(char.IsLetter).ToArray();
		if (letters.Length >= 4 && letters.All(char.IsUpper))
		{
			return true;
		}

		return NumberedSectionRegex().IsMatch(trimmed) || PrefixedHeadingRegex().IsMatch(trimmed);
	}

	private static int GetIndentation(string line)
	{
		var count = 0;
		foreach (var c in line)
		{
			if (c is not (' ' or '\t'))
			{
				break;
			}

			count += c == '\t' ? 4 : 1;
		}

		return count;
	}

	private static List<string> DetectCodeIndicators(string code)
		=> [.. CodeKeywords.Where(keyword => code.Contains(keyword, StringComparison.OrdinalIgnoreCase))];

	[GeneratedRegex(@"^(?<marker>[-*•])\s+(?<text>.+)$")]
	private static partial Regex BulletListRegex();

	[GeneratedRegex(@"^(?<marker>\d+[.)])\s+(?<text>.+)$")]
	private static partial Regex NumberedListRegex();
}
