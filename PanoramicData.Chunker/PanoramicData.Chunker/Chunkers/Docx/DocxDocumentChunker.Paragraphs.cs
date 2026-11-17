using DocumentFormat.OpenXml.Wordprocessing;
using PanoramicData.Chunker.Models;
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

	private static int GetIndentationLevel(Paragraph paragraph)
	{
		var left = paragraph.GetFirstChild<ParagraphProperties>()?.Indentation?.Left?.Value;

		// Twentieths of a point: every 720 is one level
		return left == null ? 0 : int.Parse(left) / 720;
	}

	private static bool HasFormatting(Paragraph paragraph)
		=> paragraph.Elements<Run>().Any(run => run.RunProperties != null && HasRunFormatting(run.RunProperties));

	private static bool HasRunFormatting(RunProperties properties)
		=> new object?[] { properties.Bold, properties.Italic, properties.Underline, properties.Strike, properties.Color, properties.Highlight }
			.Any(property => property != null);

	private static List<ContentAnnotation> ExtractAnnotations(Paragraph paragraph)
	{
		var annotations = paragraph.Descendants<Hyperlink>()
			.Where(hyperlink => hyperlink.Id?.Value != null)
			.Select(CreateLinkAnnotation)
			.ToList();

		foreach (var run in paragraph.Elements<Run>().Where(run => run.RunProperties != null))
		{
			annotations.AddRange(CreateFormatAnnotations(run));
		}

		return annotations;
	}

	private static ContentAnnotation CreateLinkAnnotation(Hyperlink hyperlink) => new()
	{
		Type = AnnotationType.Link,
		Attributes = new Dictionary<string, string>
		{
			["text"] = hyperlink.InnerText,
			["relationshipId"] = hyperlink.Id!.Value!
		}
	};

	private static IEnumerable<ContentAnnotation> CreateFormatAnnotations(Run run)
	{
		var properties = run.RunProperties!;
		var text = run.InnerText;

		if (properties.Bold != null)
		{
			yield return CreateTextAnnotation(AnnotationType.Bold, text);
		}

		if (properties.Italic != null)
		{
			yield return CreateTextAnnotation(AnnotationType.Italic, text);
		}

		if (properties.Underline != null)
		{
			yield return CreateTextAnnotation(AnnotationType.Underline, text);
		}
	}

	private static ContentAnnotation CreateTextAnnotation(AnnotationType type, string text) => new()
	{
		Type = type,
		Attributes = new Dictionary<string, string> { ["text"] = text }
	};

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	private sealed class NumberingInfo
	{
		public bool IsNumbered { get; set; }
		public int Level { get; set; }
		public string? NumberingText { get; set; }
		public string? Format { get; set; }
	}
}
