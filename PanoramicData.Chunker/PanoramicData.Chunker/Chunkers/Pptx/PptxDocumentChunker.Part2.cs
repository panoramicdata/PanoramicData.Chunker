using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PanoramicData.Chunker.Chunkers.Pptx;

public partial class PptxDocumentChunker
{
	private void ExtractSlideChunks(SlidePart slidePart, int slideNumber)
	{
		var slide = slidePart.Slide;
		if (slide?.CommonSlideData == null)
		{
			return;
		}

		var shapeTree = slide.CommonSlideData.ShapeTree;
		var shapes = (shapeTree?.Elements<P.Shape>() ?? []).ToList();
		var titleShape = shapes.FirstOrDefault(s => IsTitlePlaceholder(GetPlaceholderType(s)));
		var slideTitle = titleShape == null ? null : GetShapeText(titleShape);

		var slideChunk = CreateSlideChunk(slidePart, slide, shapes.Count, slideTitle, slideNumber);
		_chunks.Add(slideChunk);

		if (titleShape != null && !string.IsNullOrWhiteSpace(slideTitle))
		{
			AddChunk(CreateTitleChunk(titleShape, slideNumber, slideChunk.Id, false));
		}

		foreach (var shape in shapes)
		{
			ExtractShapeChunk(shape, slideNumber, slideChunk.Id);
		}

		ExtractSlideAttachments(slidePart, slide, slideNumber, slideChunk.Id);
	}

	private void ExtractSlideAttachments(SlidePart slidePart, Slide slide, int slideNumber, Guid slideId)
	{
		foreach (var table in slide.CommonSlideData?.ShapeTree?.Descendants<A.Table>() ?? [])
		{
			AddChunk(CreateTableChunk(table, slideNumber, slideId));
		}

		if (slidePart.NotesSlidePart != null)
		{
			AddChunk(ExtractNotesChunk(slidePart.NotesSlidePart, slideNumber, slideId));
		}

		ExtractImagesFromSlide(slide, slideNumber, slideId);
	}

	private PptxSlideChunk CreateSlideChunk(SlidePart slidePart, Slide slide, int shapeCount, string? slideTitle, int slideNumber) => new()
	{
		SlideNumber = slideNumber,
		Title = slideTitle,
		LayoutName = slidePart.SlideLayoutPart?.SlideLayout?.CommonSlideData?.Name?.Value,
		HasNotes = slidePart.NotesSlidePart != null,
		ShapeCount = shapeCount,
		HasAnimations = slide.Timing != null,
		HasTransitions = slide.Transition != null,
		ParentId = null,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "slide",
		Metadata = CreateMetadata(slideNumber, $"slide{slideNumber}", "slide", $"slide{slideNumber}")
	};

	private void ExtractShapeChunk(P.Shape shape, int slideNumber, Guid slideId)
	{
		var placeholderType = GetPlaceholderType(shape);

		// The title was already processed
		if (IsTitlePlaceholder(placeholderType))
		{
			return;
		}

		if (placeholderType == "subTitle")
		{
			AddChunk(CreateTitleChunk(shape, slideNumber, slideId, true));
			return;
		}

		var text = GetShapeText(shape);
		if (!string.IsNullOrWhiteSpace(text))
		{
			AddChunk(CreateContentChunk(shape, text, slideNumber, slideId, placeholderType));
		}
	}

	private static bool IsTitlePlaceholder(string? placeholderType) => placeholderType is "title" or "ctrTitle";

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => ChunkFactory.CreateQualityMetrics(_tokenCounter, text, ChunkFactory.CountSpaceSeparatedWords(text));
}
