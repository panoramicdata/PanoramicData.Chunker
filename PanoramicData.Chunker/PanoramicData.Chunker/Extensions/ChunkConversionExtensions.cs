using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Extensions;

/// <summary>
/// Extension methods for converting chunks to various text formats.
/// </summary>
public static class ChunkConversionExtensions
{
	/// <summary>
	/// Converts a chunk to plain text.
	/// </summary>
	public static string ToPlainText(this ChunkerBase chunk) => chunk switch
	{
		ContentChunk contentChunk => contentChunk.Content ?? string.Empty,
		StructuralChunk structuralChunk when structuralChunk is MarkdownSectionChunk section =>
			section.HeadingText,
		VisualChunk visualChunk =>
			visualChunk.Caption ?? visualChunk.GeneratedDescription ?? "[Image]",
		_ => string.Empty
	};

	/// <summary>
	/// Converts a collection of chunks to plain text.
	/// </summary>
	/// <param name="chunks">The chunks to convert.</param>
	/// <param name="separator">Separator between chunks (default: double newline).</param>
	public static string ToPlainText(this IEnumerable<ChunkerBase> chunks, string separator = "\n\n")
		=> string.Join(separator, chunks.Select(c => c.ToPlainText()).Where(t => !string.IsNullOrWhiteSpace(t)));

	/// <summary>
	/// Converts a chunk to Markdown format.
	/// </summary>
	public static string ToMarkdown(this ChunkerBase chunk) => chunk switch
	{
		MarkdownSectionChunk section => section.MarkdownSyntax,
		MarkdownParagraphChunk paragraph => paragraph.MarkdownContent ?? paragraph.Content ?? string.Empty,
		MarkdownListItemChunk listItem => listItem.MarkdownContent ?? listItem.Content ?? string.Empty,
		MarkdownCodeBlockChunk codeBlock => codeBlock.MarkdownContent ?? $"```\n{codeBlock.Content}\n```",
		MarkdownQuoteChunk quote => quote.MarkdownContent ?? $"> {quote.Content}",
		MarkdownTableChunk table => table.MarkdownContent ?? table.Content ?? string.Empty,
		MarkdownImageChunk image => $"![{image.AltText}]({image.ImageUrl})",
		ContentChunk contentChunk => contentChunk.MarkdownContent ?? contentChunk.Content ?? string.Empty,
		StructuralChunk => string.Empty,
		VisualChunk visual => $"![{visual.Caption ?? "Image"}]({visual.BinaryReference})",
		_ => string.Empty
	};

	/// <summary>
	/// Converts a collection of chunks to Markdown format.
	/// </summary>
	/// <param name="chunks">The chunks to convert.</param>
	/// <param name="includeHierarchy">Whether to indent based on depth.</param>
	public static string ToMarkdown(this IEnumerable<ChunkerBase> chunks, bool includeHierarchy = false)
	{
		var sb = new StringBuilder();
		var chunkList = chunks.OrderBy(c => c.SequenceNumber).ToList();

		foreach (var chunk in chunkList)
		{
			var markdown = chunk.ToMarkdown();
			if (string.IsNullOrWhiteSpace(markdown))
			{
				continue;
			}

			if (includeHierarchy && chunk.Depth > 0)
			{
				var indent = new string(' ', chunk.Depth * 2);
				_ = sb.AppendLine($"{indent}{markdown}");
			}
			else
			{
				_ = sb.AppendLine(markdown);
			}

			_ = sb.AppendLine();
		}

		return sb.ToString().TrimEnd();
	}

	/// <summary>
	/// Converts a chunk to HTML format.
	/// </summary>
	public static string ToHtml(this ChunkerBase chunk) => chunk switch
	{
		MarkdownSectionChunk section =>
			$"<h{section.HeadingLevel}>{HtmlEncode(section.HeadingText)}</h{section.HeadingLevel}>",

		MarkdownParagraphChunk paragraph =>
			paragraph.HtmlContent ?? $"<p>{HtmlEncode(paragraph.Content ?? string.Empty)}</p>",

		MarkdownListItemChunk listItem =>
			$"<li>{HtmlEncode(listItem.Content ?? string.Empty)}</li>",

		MarkdownCodeBlockChunk codeBlock =>
			$"<pre><code class=\"language-{codeBlock.Language ?? "plaintext"}\">{HtmlEncode(codeBlock.Content ?? string.Empty)}</code></pre>",

		MarkdownQuoteChunk quote =>
			$"<blockquote>{HtmlEncode(quote.Content ?? string.Empty)}</blockquote>",

		MarkdownTableChunk table =>
			ConvertTableToHtml(table),

		MarkdownImageChunk image =>
			$"<img src=\"{HtmlEncode(image.ImageUrl)}\" alt=\"{HtmlEncode(image.AltText)}\" />",

		ContentChunk contentChunk =>
			contentChunk.HtmlContent ?? $"<p>{HtmlEncode(contentChunk.Content ?? string.Empty)}</p>",

		VisualChunk visual =>
			$"<img src=\"{HtmlEncode(visual.BinaryReference)}\" alt=\"{HtmlEncode(visual.Caption ?? "Image")}\" />",

		_ => string.Empty
	};

	/// <summary>
	/// Converts a collection of chunks to HTML format.
	/// </summary>
	/// <param name="chunks">The chunks to convert.</param>
	/// <param name="wrapInDocument">Whether to wrap in full HTML document.</param>
	public static string ToHtml(this IEnumerable<ChunkerBase> chunks, bool wrapInDocument = false)
	{
		var sb = new StringBuilder();

		if (wrapInDocument)
		{
			_ = sb.AppendLine("<!DOCTYPE html>");
			_ = sb.AppendLine("<html>");
			_ = sb.AppendLine("<head><meta charset=\"utf-8\"><title>Document</title></head>");
			_ = sb.AppendLine("<body>");
		}

		var chunkList = chunks.OrderBy(c => c.SequenceNumber).ToList();

		foreach (var chunk in chunkList)
		{
			var html = chunk.ToHtml();
			if (!string.IsNullOrWhiteSpace(html))
			{
				_ = sb.AppendLine(html);
			}
		}

		if (wrapInDocument)
		{
			_ = sb.AppendLine("</body>");
			_ = sb.AppendLine("</html>");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Extracts all text content from a chunk recursively.
	/// </summary>
	public static string ExtractAllText(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine(chunk.ToPlainText());

		var descendants = ChunkQueryExtensions.GetDescendants(chunk, allChunks);
		foreach (var descendant in descendants.OrderBy(c => c.SequenceNumber))
		{
			var text = descendant.ToPlainText();
			if (!string.IsNullOrWhiteSpace(text))
			{
				_ = sb.AppendLine(text);
			}
		}

		return sb.ToString().Trim();
	}

	/// <summary>
	/// Creates a summary string for a chunk.
	/// </summary>
	/// <param name="chunk">The chunk to summarize.</param>
	/// <param name="maxLength">Maximum length of summary (default: 100 characters).</param>
	public static string ToSummary(this ChunkerBase chunk, int maxLength = 100)
	{
		var text = chunk.ToPlainText();
		if (string.IsNullOrWhiteSpace(text))
		{
			return $"[{chunk.SpecificType}]";
		}

		if (text.Length <= maxLength)
		{
			return text;
		}

		return text[..(maxLength - 3)] + "...";
	}

	/// <summary>
	/// Converts chunks to a tree structure string representation.
	/// </summary>
	public static string ToTreeString(this IEnumerable<ChunkerBase> chunks)
	{
		var sb = new StringBuilder();
		var chunkList = chunks.ToList();
		var rootChunks = chunkList.Where(c => !c.ParentId.HasValue).OrderBy(c => c.SequenceNumber);

		foreach (var root in rootChunks)
		{
			AppendChunkTree(root, chunkList, sb, 0);
		}

		return sb.ToString();
	}

	private static void AppendChunkTree(ChunkerBase chunk, List<ChunkerBase> allChunks, StringBuilder sb, int indent)
	{
		var prefix = new string(' ', indent * 2);
		var summary = chunk.ToSummary(50);
		var typeInfo = $"[{chunk.SpecificType}]";
		var tokenInfo = chunk.QualityMetrics != null ? $" ({chunk.QualityMetrics.TokenCount} tokens)" : "";

		_ = sb.AppendLine($"{prefix}{typeInfo} {summary}{tokenInfo}");

		var children = allChunks.Where(c => c.ParentId == chunk.Id).OrderBy(c => c.SequenceNumber);
		foreach (var child in children)
		{
			AppendChunkTree(child, allChunks, sb, indent + 1);
		}
	}

	/// <summary>
	/// Converts a table chunk to HTML table format.
	/// </summary>
	private static string ConvertTableToHtml(MarkdownTableChunk table)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine("<table>");

		if (table.TableInfo?.Headers != null && table.TableInfo.Headers.Length > 0)
		{
			_ = sb.AppendLine("<thead><tr>");
			foreach (var header in table.TableInfo.Headers)
			{
				_ = sb.Append($"<th>{HtmlEncode(header)}</th>");
			}
			_ = sb.AppendLine("</tr></thead>");
		}

		_ = sb.AppendLine("<tbody>");
		// Note: Row data would need to be extracted from SerializedTable or stored separately
		_ = sb.AppendLine("</tbody>");
		_ = sb.AppendLine("</table>");

		return sb.ToString();
	}

	/// <summary>
	/// Simple HTML encoding for safety.
	/// </summary>
	private static string HtmlEncode(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		return text
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&#39;");
	}
}
