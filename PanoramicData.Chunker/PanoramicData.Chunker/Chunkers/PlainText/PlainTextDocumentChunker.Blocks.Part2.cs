using PanoramicData.Chunker.Models;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.PlainText;

public partial class PlainTextDocumentChunker
{
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
}
