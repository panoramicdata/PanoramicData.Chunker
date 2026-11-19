using PanoramicData.Chunker.Models;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.PlainText;

public partial class PlainTextDocumentChunker
{
	private PlainTextSectionChunk? DetectHeading(string[] lines, ref int index, string? previousLine, ChunkerBase? previousChunk)
	{
		var line = lines[index];

		if (index + 1 < lines.Length)
		{
			var underlineHeading = DetectUnderlinedHeading(line, lines[index + 1]);
			if (underlineHeading != null)
			{
				index += 2;
				return underlineHeading;
			}
		}

		var heading = DetectNumberedHeading(line, previousLine, previousChunk)
			?? DetectAllCapsHeading(line)
			?? DetectPrefixedHeading(line);
		if (heading != null)
		{
			index++;
		}

		return heading;
	}

	private PlainTextSectionChunk? DetectUnderlinedHeading(string line, string nextLine)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(nextLine))
		{
			return null;
		}

		var trimmedLine = line.Trim();
		var trimmedNext = nextLine.Trim();

		if (IsUnderline(trimmedNext, '='))
		{
			return CreateSectionChunk(trimmedLine, 1, HeadingHeuristic.Underlined, 0.95);
		}

		return IsUnderline(trimmedNext, '-')
			? CreateSectionChunk(trimmedLine, 2, HeadingHeuristic.Underlined, 0.90)
			: null;
	}

	private PlainTextSectionChunk? DetectNumberedHeading(string line, string? previousLine, ChunkerBase? previousChunk)
	{
		var match = NumberedSectionRegex().Match(line.Trim());
		if (!match.Success)
		{
			return null;
		}

		var numbering = match.Groups["numbering"].Value;
		var text = match.Groups["text"].Value.Trim();
		var level = numbering.Count(c => c == '.') + 1;

		if (level == 1 && !IsStandaloneHeading(text, previousLine, previousChunk))
		{
			return null;
		}

		return CreateSectionChunk(text, Math.Min(level, 6), HeadingHeuristic.Numbered, 0.85);
	}

	private PlainTextSectionChunk? DetectAllCapsHeading(string line)
	{
		var trimmed = line.Trim();

		if (trimmed.Length is < 4 or > 100)
		{
			return null;
		}

		var letters = trimmed.Where(char.IsLetter).ToArray();
		if (letters.Length == 0 || !letters.All(char.IsUpper) || letters.Length < trimmed.Length * 0.5)
		{
			return null;
		}

		return CreateSectionChunk(trimmed, 1, HeadingHeuristic.AllCaps, 0.70);
	}

	private PlainTextSectionChunk? DetectPrefixedHeading(string line)
	{
		var match = PrefixedHeadingRegex().Match(line.Trim());
		if (!match.Success)
		{
			return null;
		}

		var text = match.Groups["text"].Value.Trim();
		var level = Math.Min(match.Groups["prefix"].Value.Length, 6);

		return CreateSectionChunk(text, level, HeadingHeuristic.Prefixed, 0.75);
	}

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

	private static bool IsUnderline(string text, char marker)
		=> text.Length >= 3 && text.All(c => c == marker);

	private static bool IsStandaloneHeading(string text, string? previousLine, ChunkerBase? previousChunk)
		=> !EndsWithColon(previousLine)
			&& previousChunk is not PlainTextListItemChunk
			&& LooksLikeHeadingText(text, 100);

	private static bool EndsWithColon(string? line) => line?.Trim().EndsWith(':') == true;

	private static bool LooksLikeHeadingText(string text, int maxLength)
		=> char.IsUpper(text.FirstOrDefault())
			&& text.Length <= maxLength
			&& text.TrimEnd().LastOrDefault() is not ('.' or '!' or '?');

	[GeneratedRegex(@"^(?<numbering>\d+(\.\d+)*)\.?\s+(?<text>.+)$")]
	private static partial Regex NumberedSectionRegex();

	[GeneratedRegex(@"^(?<prefix>#{1,6})\s+(?<text>.+)$")]
	private static partial Regex PrefixedHeadingRegex();
}
