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
}
