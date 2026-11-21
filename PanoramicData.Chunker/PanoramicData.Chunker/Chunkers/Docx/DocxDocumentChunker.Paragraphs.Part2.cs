using DocumentFormat.OpenXml.Wordprocessing;
using PanoramicData.Chunker.Models;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.Docx;

public partial class DocxDocumentChunker
{
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
