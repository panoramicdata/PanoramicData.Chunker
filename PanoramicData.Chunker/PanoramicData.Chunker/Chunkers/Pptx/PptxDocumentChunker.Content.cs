using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PanoramicData.Chunker.Models;
using System.Text;
using System.Text.RegularExpressions;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PanoramicData.Chunker.Chunkers.Pptx;

public partial class PptxDocumentChunker
{
	private PptxTitleChunk? CreateTitleChunk(P.Shape shape, int slideNumber, Guid slideId, bool isSubtitle)
	{
		var text = GetShapeText(shape);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var kind = isSubtitle ? "subtitle" : "title";

		return new PptxTitleChunk
		{
			Content = text,
			SlideNumber = slideNumber,
			IsSubtitle = isSubtitle,
			PlaceholderType = GetPlaceholderType(shape),
			HasFormatting = HasFormatting(shape),
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = kind,
			Metadata = CreateMetadata(slideNumber, $"slide{slideNumber}/{kind}", kind, $"slide{slideNumber}"),
			Annotations = ExtractAnnotations(shape),
			QualityMetrics = CalculateQualityMetrics(text)
		};
	}

	private PptxContentChunk CreateContentChunk(P.Shape shape, string text, int slideNumber, Guid slideId, string? placeholderType) => new()
	{
		Content = text,
		SlideNumber = slideNumber,
		SourceType = placeholderType ?? "textBox",
		PlaceholderType = placeholderType,
		HasFormatting = HasFormatting(shape),
		ShapeName = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value,
		ParentId = slideId,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "content",
		Metadata = CreateMetadata(slideNumber, $"slide{slideNumber}/content", "content", $"slide{slideNumber}"),
		Annotations = ExtractAnnotations(shape),
		QualityMetrics = CalculateQualityMetrics(text)
	};

	private PptxNotesChunk? ExtractNotesChunk(NotesSlidePart notesSlidePart, int slideNumber, Guid slideId)
	{
		var shapes = notesSlidePart.NotesSlide?.CommonSlideData?.ShapeTree?.Elements<P.Shape>();
		if (notesSlidePart.NotesSlide?.CommonSlideData == null)
		{
			return null;
		}

		var notesText = new StringBuilder();
		foreach (var shape in (shapes ?? []).Where(s => GetPlaceholderType(s) != "sldImg"))
		{
			var text = GetShapeText(shape);
			if (!string.IsNullOrWhiteSpace(text))
			{
				_ = notesText.AppendLine(text);
			}
		}

		var notesContent = notesText.ToString().Trim();
		return string.IsNullOrWhiteSpace(notesContent) ? null : CreateNotesChunk(notesContent, slideNumber, slideId);
	}

	private PptxNotesChunk CreateNotesChunk(string notesContent, int slideNumber, Guid slideId) => new()
	{
		Content = notesContent,
		SlideNumber = slideNumber,
		NotesLength = notesContent.Length,
		ParentId = slideId,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "notes",
		Metadata = CreateMetadata(slideNumber, $"slide{slideNumber}/notes", "notes", "speakerNotes", $"slide{slideNumber}"),
		QualityMetrics = CalculateQualityMetrics(notesContent)
	};

	private void ExtractImagesFromSlide(Slide slide, int slideNumber, Guid slideId)
	{
		var pictures = slide.Descendants<P.Picture>()
			.Where(picture => picture.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value != null);

		foreach (var _ in pictures)
		{
			_chunks.Add(CreateImageChunk("image", slideNumber, slideId, null));
		}

		foreach (var _ in slide.Descendants<P.GraphicFrame>().Where(IsChartFrame))
		{
			var chunk = CreateImageChunk("chart", slideNumber, slideId, "chart");
			chunk.IsChart = true;
			_chunks.Add(chunk);
		}
	}

	private static bool IsChartFrame(P.GraphicFrame frame)
		=> frame.Descendants<A.GraphicData>().FirstOrDefault()?.Uri?.Value?.Contains("chart") == true;

	private PptxImageChunk CreateImageChunk(string visualType, int slideNumber, Guid slideId, string? chartType)
	{
		var metadata = CreateMetadata(slideNumber, $"slide{slideNumber}/{visualType}", visualType, $"slide{slideNumber}");
		metadata.HasImage = true;

		return new PptxImageChunk
		{
			SlideNumber = slideNumber,
			VisualType = visualType,
			IsChart = visualType == "chart",
			ChartType = chartType,
			Position = null,
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = visualType,
			MimeType = "image/unknown",
			BinaryReference = Guid.NewGuid().ToString(),
			Metadata = metadata
		};
	}

	private static string GetShapeText(P.Shape shape)
	{
		var textBody = shape.TextBody;
		if (textBody == null)
		{
			return string.Empty;
		}

		var sb = new StringBuilder();
		foreach (var paragraph in textBody.Elements<A.Paragraph>())
		{
			_ = sb.Append(GetRunsText(paragraph)).AppendLine();
		}

		return WhitespaceRegex().Replace(sb.ToString(), " ").Trim();
	}

	private static string GetRunsText(A.Paragraph paragraph)
		=> string.Concat(paragraph.Elements<A.Run>().Select(run => run.Text?.Text).Where(text => !string.IsNullOrEmpty(text)));

	private static string? GetPlaceholderType(P.Shape shape)
		=> shape.NonVisualShapeProperties?
			.ApplicationNonVisualDrawingProperties?
			.PlaceholderShape?
			.Type?.Value.ToString();

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
