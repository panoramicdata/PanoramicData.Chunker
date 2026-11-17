using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using PanoramicData.Chunker.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.Html;

public partial class HtmlDocumentChunker
{
	private HtmlSectionChunk CreateStructuralChunk(IElement element, Guid? parentId)
	{
		var tagName = element.TagName.ToLowerInvariant();
		var headingLevel = GetHeadingLevel(tagName);

		return new HtmlSectionChunk
		{
			ParentId = parentId,
			TagName = tagName,
			HeadingLevel = headingLevel,
			CssClasses = [.. element.ClassList],
			ElementId = element.Id,
			Role = element.GetAttribute("role"),
			SequenceNumber = _sequenceNumber++,
			SpecificType = headingLevel.HasValue ? $"Heading{headingLevel}" : tagName,
			Metadata = CreateMetadata(tagName, tagName),
			QualityMetrics = CalculateQualityMetrics(GetCleanText(element))
		};
	}

	private ChunkerBase? CreateContentChunk(IElement element, Guid? parentId)
	{
		var tagName = element.TagName.ToLowerInvariant();

		// List items exclude nested list content
		var text = tagName == "li" ? GetDirectTextContent(element) : GetCleanText(element);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var chunk = CreateTypedChunk(element, tagName, text);
		chunk.ParentId = parentId;
		chunk.SequenceNumber = _sequenceNumber++;
		chunk.SpecificType = tagName;
		chunk.Metadata = CreateMetadata(tagName, tagName);

		var annotations = ExtractAnnotations(element);
		if (chunk is ContentChunk contentChunk && annotations.Count > 0)
		{
			contentChunk.Annotations = annotations;
		}

		chunk.QualityMetrics = CalculateQualityMetrics(text);
		return chunk;
	}

	private static ChunkerBase CreateTypedChunk(IElement element, string tagName, string text) => tagName switch
	{
		"blockquote" => new HtmlBlockquoteChunk
		{
			Content = text,
			HtmlContent = element.InnerHtml,
			CiteUrl = element.GetAttribute("cite"),
			CssClasses = [.. element.ClassList],
			ElementId = element.Id
		},
		"pre" => CreateCodeBlockChunk(element, text),
		"li" => CreateListItemChunk(element, text),
		_ => new HtmlParagraphChunk
		{
			Content = text,
			HtmlContent = element.InnerHtml,
			CssClasses = [.. element.ClassList],
			ElementId = element.Id
		}
	};

	private static HtmlCodeBlockChunk CreateCodeBlockChunk(IElement element, string text)
	{
		var codeElement = element.QuerySelector("code");

		return new HtmlCodeBlockChunk
		{
			Content = text,
			HtmlContent = element.InnerHtml,
			Language = GetCodeLanguage(codeElement ?? element),
			HasCodeElement = codeElement != null,
			CssClasses = [.. element.ClassList],
			ElementId = element.Id
		};
	}

	// e.g. "language-csharp" or "lang-csharp"
	private static string? GetCodeLanguage(IElement element)
		=> element.ClassList
			.FirstOrDefault(c => c.StartsWith("language-") || c.StartsWith("lang-"))
			?.Replace("language-", "").Replace("lang-", "");

	private static HtmlListItemChunk CreateListItemChunk(IElement element, string text)
	{
		var listType = element.ParentElement?.TagName.ToLowerInvariant() ?? "ul";

		// 0-based nesting: count ul/ol ancestors above the immediate list
		var nestingLevel = AncestorsFrom(element.ParentElement?.ParentElement)
			.Count(ancestor => ancestor.TagName.ToLowerInvariant() is "ul" or "ol");

		return new HtmlListItemChunk
		{
			Content = text,
			HtmlContent = element.InnerHtml,
			ListType = listType,
			NestingLevel = nestingLevel,
			CssClasses = [.. element.ClassList],
			ElementId = element.Id
		};
	}

	private static IEnumerable<IElement> AncestorsFrom(IElement? start)
	{
		for (var current = start; current != null; current = current.ParentElement)
		{
			yield return current;
		}
	}

	private HtmlImageChunk? CreateImageChunk(IElement element, Guid? parentId)
	{
		if (element is not IHtmlImageElement img)
		{
			return null;
		}

		// The raw attribute, not the computed Source which resolves against a base URL
		var src = img.GetAttribute("src");
		if (string.IsNullOrEmpty(src))
		{
			return null;
		}

		var altText = img.AlternativeText ?? string.Empty;

		return new HtmlImageChunk
		{
			ParentId = parentId,
			Caption = altText,
			BinaryReference = src,
			MimeType = DetermineMimeType(src),
			Title = img.Title,
			Width = ParseDimension(img.GetAttribute("width")),
			Height = ParseDimension(img.GetAttribute("height")),
			CssClasses = [.. element.ClassList],
			ElementId = element.Id,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "image",
			Metadata = CreateMetadata("image", "image"),
			QualityMetrics = new ChunkQualityMetrics
			{
				CharacterCount = altText.Length,
				WordCount = altText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
				SemanticCompleteness = string.IsNullOrEmpty(altText) ? 0.5 : 1.0
			}
		};
	}

	private static int? ParseDimension(string? value) => int.TryParse(value, out var parsed) ? parsed : null;

	private static string DetermineMimeType(string src)
		=> Path.GetExtension(src).ToLowerInvariant() switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".gif" => "image/gif",
			".svg" => "image/svg+xml",
			".webp" => "image/webp",
			_ => "image/unknown"
		};

	private static string GetCleanText(IElement element)
		=> WhitespaceRegex().Replace(element.TextContent, " ").Trim();

	// Direct text only, excluding nested ul/ol elements
	private static string GetDirectTextContent(IElement element)
	{
		var sb = new StringBuilder();

		foreach (var node in element.ChildNodes)
		{
			if (node.NodeType == NodeType.Text || (node is IElement child && child.TagName.ToLowerInvariant() is not "ul" and not "ol"))
			{
				_ = sb.Append(node.TextContent);
			}
		}

		return WhitespaceRegex().Replace(sb.ToString(), " ").Trim();
	}

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
}
