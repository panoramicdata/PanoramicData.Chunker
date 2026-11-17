using PanoramicData.Chunker.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PanoramicData.Chunker.Chunkers.Pdf;

public partial class PdfDocumentChunker
{
	private static readonly string[] HeadingPrefixes = ["Chapter ", "Section ", "Part "];

	private static ChunkMetadata CreateMetadata(string hierarchy, params string[] tags) => new()
	{
		DocumentType = "PDF",
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		CreatedAt = DateTimeOffset.UtcNow
	};

	private PdfDocumentChunk CreateDocumentChunk(PdfDocument pdf)
	{
		var info = pdf.Information;

		return new PdfDocumentChunk
		{
			PdfVersion = pdf.Version.ToString(),
			PageCount = pdf.NumberOfPages,
			Title = info?.Title,
			Author = info?.Author,
			Subject = info?.Subject,
			CreationDate = TryParseDate(info?.CreationDate),
			ModificationDate = TryParseDate(info?.ModifiedDate),
			IsEncrypted = false,
			ParentId = null,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "pdf-document",
			Metadata = CreateMetadata("pdf", "pdf", "document"),
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 0,
				CharacterCount = 0,
				WordCount = 0,
				SemanticCompleteness = 1.0
			}
		};
	}

	private PdfPageChunk CreatePageChunk(Page page, Guid documentId) => new()
	{
		PageNumber = page.Number,
		Width = (double)page.Width,
		Height = (double)page.Height,
		Rotation = (int)page.Rotation.Value,
		ExtractedText = page.Text,
		WordCount = CountWords(page.Text),
		ParentId = documentId,
		SequenceNumber = _sequenceNumber++,
		SpecificType = "pdf-page",
		Metadata = CreateMetadata($"pdf/page{page.Number}", "pdf-page"),
		QualityMetrics = CalculateQualityMetrics(page.Text)
	};

	private List<PdfParagraphChunk> ExtractParagraphsFromPage(Page page, Guid pageId)
	{
		var text = page.Text;
		if (string.IsNullOrWhiteSpace(text))
		{
			return [];
		}

		// Simple heuristic: paragraphs are separated by a blank line
		var paragraphTexts = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);

		return [.. paragraphTexts
			.Select((paragraphText, index) => (Text: paragraphText.Trim(), Index: index))
			.Where(paragraph => !string.IsNullOrWhiteSpace(paragraph.Text))
			.Select(paragraph => CreateParagraphChunk(page.Number, pageId, paragraph.Text, paragraph.Index))];
	}

	private PdfParagraphChunk CreateParagraphChunk(int pageNumber, Guid pageId, string paragraphText, int index)
	{
		var isLikelyHeading = IsLikelyHeading(paragraphText);

		return new PdfParagraphChunk
		{
			Content = paragraphText,
			PageNumber = pageNumber,
			ParagraphIndex = index,
			YPosition = 0,
			FontSize = null,
			IsLikelyHeading = isLikelyHeading,
			ParentId = pageId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "pdf-paragraph",
			Metadata = CreateMetadata(
				$"pdf/page{pageNumber}/para{index}",
				isLikelyHeading ? ["pdf-paragraph", "heading"] : ["pdf-paragraph"]),
			QualityMetrics = CalculateQualityMetrics(paragraphText)
		};
	}

	// Short text, no trailing punctuation, and either mostly uppercase or a heading prefix
	private static bool IsLikelyHeading(string text)
	{
		if (text.Length > 100 || text.EndsWith('.') || text.EndsWith(','))
		{
			return false;
		}

		var uppercaseRatio = text.Count(char.IsUpper) / (double)text.Length;
		return uppercaseRatio > 0.3
			|| HeadingPrefixes.Any(prefix => text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
	}

	private static int CountWords(string text)
		=> string.IsNullOrWhiteSpace(text) ? 0 : text.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = CountWords(text),
		SemanticCompleteness = 1.0
	};

	private static DateTime? TryParseDate(string? dateString)
	{
		if (string.IsNullOrWhiteSpace(dateString))
		{
			return null;
		}

		// PDF dates are in format: D:YYYYMMDDHHmmSS
		if (dateString.StartsWith("D:") && dateString.Length >= 16)
		{
			return new DateTime(
				int.Parse(dateString.AsSpan(2, 4)),
				int.Parse(dateString.AsSpan(6, 2)),
				int.Parse(dateString.AsSpan(8, 2)),
				int.Parse(dateString.AsSpan(10, 2)),
				int.Parse(dateString.AsSpan(12, 2)),
				int.Parse(dateString.AsSpan(14, 2)));
		}

		return DateTime.TryParse(dateString, out var result) ? result : null;
	}
}
