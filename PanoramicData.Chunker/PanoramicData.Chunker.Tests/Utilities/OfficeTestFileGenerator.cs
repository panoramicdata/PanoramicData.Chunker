using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Generates the DOCX and PPTX fixtures that are not committed: formatted.docx, large.docx and large.pptx.
/// </summary>
public static class OfficeTestFileGenerator
{
	private static readonly string _docxPath = Path.Combine("TestData", "Docx");
	private static readonly string _pptxPath = Path.Combine("TestData", "Pptx");
	private static readonly object _generationLock = new();

	public static void GenerateAllTestFiles()
	{
		lock (_generationLock)
		{
			_ = Directory.CreateDirectory(_docxPath);
			_ = Directory.CreateDirectory(_pptxPath);

			CreateIfMissing(Path.Combine(_docxPath, "formatted.docx"), CreateFormattedDocx);
			CreateIfMissing(Path.Combine(_docxPath, "large.docx"), CreateLargeDocx);
			CreateIfMissing(Path.Combine(_pptxPath, "large.pptx"), CreateLargePptx);
		}
	}

	private static void CreateIfMissing(string path, Action<string> create)
	{
		if (!File.Exists(path))
		{
			create(path);
		}
	}

	private static void CreateFormattedDocx(string path)
	{
		WriteDocx(path,
		[
			HeadingParagraph("Formatted Document", 1),
			RunParagraph(PlainRun("This paragraph has "), StyledRun("bold", new Bold()), PlainRun(" and "), StyledRun("italic", new Italic()), PlainRun(" text.")),
			RunParagraph(PlainRun("Another with "), StyledRun("underlined", new Underline { Val = UnderlineValues.Single }), PlainRun(" words."))
		]);
	}

	private static void CreateLargeDocx(string path)
	{
		var paragraphs = new List<Paragraph>();

		for (var section = 1; section <= 200; section++)
		{
			paragraphs.Add(HeadingParagraph($"Section {section}", 1));

			for (var paragraph = 1; paragraph <= 5; paragraph++)
			{
				paragraphs.Add(RunParagraph(PlainRun($"Paragraph {paragraph} of section {section} contains explanatory text used to exercise chunking of a large document.")));
			}
		}

		WriteDocx(path, paragraphs);
	}

	private static void WriteDocx(string path, IEnumerable<Paragraph> paragraphs)
	{
		using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = document.AddMainDocumentPart();
		mainPart.Document = new Document(new Body(paragraphs));
		mainPart.Document.Save();
	}

	private static Paragraph HeadingParagraph(string text, int level)
	{
		var paragraph = RunParagraph(PlainRun(text));
		paragraph.PrependChild(new ParagraphProperties(new ParagraphStyleId { Val = $"Heading{level}" }));
		return paragraph;
	}

	private static Paragraph RunParagraph(params Run[] runs) => new(runs);

	private static Run PlainRun(string text) => new(new W.Text(text) { Space = SpaceProcessingModeValues.Preserve });

	private static Run StyledRun(string text, OpenXmlElement property)
	{
		var run = PlainRun(text);
		run.PrependChild(new RunProperties(property));
		return run;
	}

	private static void CreateLargePptx(string path)
	{
		using var document = PresentationDocument.Create(path, PresentationDocumentType.Presentation);
		var presentationPart = document.AddPresentationPart();
		presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
		var slideIds = presentationPart.Presentation.AppendChild(new SlideIdList());

		for (uint slide = 1; slide <= 60; slide++)
		{
			var slidePart = presentationPart.AddNewPart<SlidePart>();
			slidePart.Slide = CreateSlide(slide);
			slidePart.Slide.Save();
			slideIds.Append(new SlideId { Id = 255 + slide, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
		}

		presentationPart.Presentation.Save();
	}

	private static Slide CreateSlide(uint number) => new(
		new CommonSlideData(
			new ShapeTree(
				new NonVisualGroupShapeProperties(
					new NonVisualDrawingProperties { Id = 1, Name = string.Empty },
					new NonVisualGroupShapeDrawingProperties(),
					new ApplicationNonVisualDrawingProperties()),
				new GroupShapeProperties(),
				CreateShape(2, "Title", $"Slide {number} title", new PlaceholderShape { Type = PlaceholderValues.Title }),
				CreateShape(3, "Content", $"Slide {number} body text with some content to chunk.", new PlaceholderShape { Type = PlaceholderValues.Body }))));

	private static Shape CreateShape(uint id, string name, string text, PlaceholderShape placeholder) => new(
		new NonVisualShapeProperties(
			new NonVisualDrawingProperties { Id = id, Name = name },
			new NonVisualShapeDrawingProperties(),
			new ApplicationNonVisualDrawingProperties(placeholder)),
		new ShapeProperties(),
		new TextBody(new A.BodyProperties(), new A.ListStyle(), new A.Paragraph(new A.Run(new A.Text(text)))));
}
