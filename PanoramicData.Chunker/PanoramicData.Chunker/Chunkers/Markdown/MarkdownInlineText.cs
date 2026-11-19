using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Extracts plain text and annotations from Markdig syntax nodes.
/// </summary>
internal static class MarkdownInlineText
{
	public static string GetInlineText(ContainerInline? inline)
	{
		if (inline == null)
		{
			return string.Empty;
		}

		var text = new StringBuilder();
		foreach (var child in inline)
		{
			_ = text.Append(GetChildText(child));
		}

		return text.ToString();
	}

	public static List<ContentAnnotation> ExtractAnnotations(ContainerInline? inline)
	{
		var annotations = new List<ContentAnnotation>();

		if (inline == null)
		{
			return annotations;
		}

		var currentIndex = 0;

		foreach (var child in inline)
		{
			var (annotation, length) = Describe(child, currentIndex);
			if (annotation != null)
			{
				annotations.Add(annotation);
			}

			currentIndex += length;
		}

		return annotations;
	}

	public static string GetBlockText(Block block)
	{
		var text = new StringBuilder();

		if (block is LeafBlock leafBlock && leafBlock.Inline != null)
		{
			_ = text.Append(GetInlineText(leafBlock.Inline));
		}
		else if (block is ContainerBlock container)
		{
			foreach (var child in container)
			{
				_ = text.AppendLine(GetBlockText(child));
			}
		}

		return text.ToString().Trim();
	}

	public static string GetCodeBlockContent(CodeBlock codeBlock)
	{
		var lines = codeBlock.Lines.Lines;
		if (lines == null || lines.Length == 0)
		{
			return string.Empty;
		}

		var text = new StringBuilder();
		for (var i = 0; i < lines.Length; i++)
		{
			_ = text.Append(lines[i].Slice.ToString());
			if (i < lines.Length - 1)
			{
				_ = text.AppendLine();
			}
		}

		return text.ToString();
	}

	public static string ExtractMarkdownContent(Block block, string markdownContent)
		=> block.Span.Start >= 0 && block.Span.End <= markdownContent.Length
			? markdownContent.Substring(block.Span.Start, block.Span.Length)
			: string.Empty;

	private static string GetChildText(Inline child)
	{
		if (child is LiteralInline literal)
		{
			return literal.Content.ToString();
		}

		if (child is ContainerInline container)
		{
			return GetInlineText(container);
		}

		return child is CodeInline code ? code.Content : string.Empty;
	}

	private static (ContentAnnotation? Annotation, int Length) Describe(Inline child, int start)
	{
		if (child is LinkInline link)
		{
			var linkText = GetInlineText(link);
			return (CreateLinkAnnotation(link, start, linkText.Length), linkText.Length);
		}

		if (child is EmphasisInline emphasis)
		{
			var emphasisText = GetInlineText(emphasis);
			var type = emphasis.DelimiterCount == 1 ? AnnotationType.Italic : AnnotationType.Bold;
			return (CreateAnnotation(start, emphasisText.Length, type), emphasisText.Length);
		}

		if (child is CodeInline code)
		{
			return (CreateAnnotation(start, code.Content.Length, AnnotationType.Code), code.Content.Length);
		}

		return (null, child is LiteralInline literal ? literal.Content.ToString().Length : 0);
	}

	private static ContentAnnotation CreateAnnotation(int start, int length, AnnotationType type) => new()
	{
		StartIndex = start,
		Length = length,
		Type = type
	};

	private static ContentAnnotation CreateLinkAnnotation(LinkInline link, int start, int length)
	{
		var annotation = CreateAnnotation(start, length, link.IsImage ? AnnotationType.Image : AnnotationType.Link);
		annotation.Attributes = new Dictionary<string, string>
		{
			["href"] = link.Url ?? string.Empty,
			["title"] = link.Title ?? string.Empty
		};
		return annotation;
	}
}
