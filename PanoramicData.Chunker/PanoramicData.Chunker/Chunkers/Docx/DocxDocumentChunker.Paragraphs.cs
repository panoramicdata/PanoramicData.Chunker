using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.Docx;

public partial class DocxDocumentChunker
{
	private static readonly string[] MonospaceFonts = ["courier", "consolas", "monaco", "menlo", "source code pro", "monospace"];

	private static string GetParagraphText(Paragraph paragraph)
		=> WhitespaceRegex().Replace(paragraph.InnerText, " ").Trim();

	private static string? GetParagraphStyleName(Paragraph paragraph)
		=> paragraph.GetFirstChild<ParagraphProperties>()?.ParagraphStyleId?.Val?.Value;

	private static int? GetHeadingLevel(string? styleName)
	{
		if (string.IsNullOrEmpty(styleName))
		{
			return null;
		}

		return styleName.ToLowerInvariant() switch
		{
			"heading1" or "heading 1" or "h1" => 1,
			"heading2" or "heading 2" or "h2" => 2,
			"heading3" or "heading 3" or "h3" => 3,
			"heading4" or "heading 4" or "h4" => 4,
			"heading5" or "heading 5" or "h5" => 5,
			"heading6" or "heading 6" or "h6" => 6,
			"title" => 1,
			"subtitle" => 2,
			_ => null
		};
	}

	private NumberingInfo? GetNumberingInfo(Paragraph paragraph)
	{
		var numbering = GetNumberingProperties(paragraph);
		if (numbering == null)
		{
			return null;
		}

		// Simplified: a full implementation would parse the numbering definitions
		return new NumberingInfo
		{
			IsNumbered = true,
			Level = GetNumberingLevel(numbering),
			NumberingText = null,
			Format = _mainPart?.NumberingDefinitionsPart == null ? "unknown" : "decimal"
		};
	}

	private static NumberingProperties? GetNumberingProperties(Paragraph paragraph)
	{
		var numbering = paragraph.GetFirstChild<ParagraphProperties>()?.NumberingProperties;
		return numbering?.NumberingId?.Val?.Value == null ? null : numbering;
	}

	private static int GetNumberingLevel(NumberingProperties numbering)
		=> numbering.NumberingLevelReference?.Val?.Value ?? 0;

	private static bool IsCodeParagraph(Paragraph paragraph, string? styleName)
		=> IsCodeStyle(styleName) || IsMonospaceParagraph(paragraph);

	private static bool IsCodeStyle(string? styleName)
	{
		if (string.IsNullOrEmpty(styleName))
		{
			return false;
		}

		var lowerStyle = styleName.ToLowerInvariant();
		return lowerStyle.Contains("code") || lowerStyle.Contains("html") || lowerStyle.Contains("preformatted");
	}

	private static bool IsMonospaceParagraph(Paragraph paragraph)
		=> paragraph.Elements<Run>()
			.Select(run => run.RunProperties?.RunFonts?.Ascii?.Value?.ToLowerInvariant())
			.Any(fontName => fontName != null && MonospaceFonts.Any(fontName.Contains));

	private static string? GetParagraphAlignment(Paragraph paragraph)
		=> paragraph.GetFirstChild<ParagraphProperties>()?.Justification?.Val?.Value.ToString().ToLowerInvariant();
}
