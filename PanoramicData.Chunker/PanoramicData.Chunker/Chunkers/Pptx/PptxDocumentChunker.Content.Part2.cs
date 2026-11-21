using DocumentFormat.OpenXml.Presentation;
using PanoramicData.Chunker.Models;
using System.Text.RegularExpressions;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PanoramicData.Chunker.Chunkers.Pptx;

public partial class PptxDocumentChunker
{
	private static bool HasFormatting(P.Shape shape)
		=> shape.TextBody?.Descendants<A.Run>()
			.Any(run => run.RunProperties is { } properties
				&& (properties.Bold != null || properties.Italic != null || properties.Underline != null || properties.Strike != null))
			== true;

	private static List<ContentAnnotation> ExtractAnnotations(P.Shape shape)
	{
		var annotations = new List<ContentAnnotation>();
		var runs = shape.TextBody?.Descendants<A.Run>().Where(run => run.RunProperties != null) ?? [];

		foreach (var run in runs)
		{
			var properties = run.RunProperties!;
			var text = run.Text?.Text ?? string.Empty;

			AddAnnotation(annotations, properties.Bold, AnnotationType.Bold, text);
			AddAnnotation(annotations, properties.Italic, AnnotationType.Italic, text);
			AddAnnotation(annotations, properties.Underline, AnnotationType.Underline, text);
		}

		return annotations;
	}

	private static void AddAnnotation(List<ContentAnnotation> annotations, object? property, AnnotationType type, string text)
	{
		if (property != null)
		{
			annotations.Add(new ContentAnnotation
			{
				Type = type,
				Attributes = new Dictionary<string, string> { ["text"] = text }
			});
		}
	}

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
}
