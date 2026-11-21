using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.PlainText;

public partial class PlainTextDocumentChunker
{
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
}
