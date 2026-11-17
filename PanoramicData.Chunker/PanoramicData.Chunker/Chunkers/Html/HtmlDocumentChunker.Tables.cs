using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;

namespace PanoramicData.Chunker.Chunkers.Html;

public partial class HtmlDocumentChunker
{
	private HtmlTableChunk? CreateTableChunk(IElement element, Guid? parentId)
	{
		if (element is not IHtmlTableElement table)
		{
			return null;
		}

		var headers = ExtractHeaders(table);
		var rows = ExtractRows(table);

		return headers.Count == 0 && rows.Count == 0
			? null
			: BuildTableChunk(table, parentId, headers, rows);
	}

	private HtmlTableChunk BuildTableChunk(IHtmlTableElement table, Guid? parentId, List<string> headers, List<List<string>> rows)
	{
		var tableText = MarkdownTableSerializer.Serialize(headers, rows);
		var allText = string.Join(" ", headers) + " " + string.Join(" ", rows.SelectMany(r => r));

		return new HtmlTableChunk
		{
			Content = tableText,
			HtmlContent = table.OuterHtml,
			Caption = table.Caption?.TextContent.Trim(),
			Summary = table.GetAttribute("summary") ?? table.GetAttribute("aria-label"),
			CssClasses = [.. table.ClassList],
			ElementId = table.Id,
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			TableInfo = new TableMetadata
			{
				RowCount = rows.Count,
				ColumnCount = GetColumnCount(headers, rows),
				Headers = [.. headers],
				HasHeaderRow = headers.Count > 0,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			SerializedTable = tableText,
			SerializationFormat = TableSerializationFormat.Markdown,
			Metadata = CreateMetadata("table", "table"),
			QualityMetrics = CalculateQualityMetrics(allText)
		};
	}

	private static int GetColumnCount(List<string> headers, List<List<string>> rows)
	{
		if (headers.Count > 0)
		{
			return headers.Count;
		}

		return rows.Count > 0 ? rows[0].Count : 0;
	}

	private static List<string> ExtractHeaders(IHtmlTableElement table)
	{
		var headerRow = table.QuerySelector("thead tr") ?? table.QuerySelector("tr");

		return headerRow == null
			? []
			: [.. headerRow.QuerySelectorAll("th, td").Select(cell => cell.TextContent.Trim())];
	}

	private static List<List<string>> ExtractRows(IHtmlTableElement table)
	{
		var bodyRows = table.QuerySelectorAll("tbody tr").ToList();
		if (bodyRows.Count == 0)
		{
			// Without a tbody, every row except the header row
			bodyRows = [.. table.QuerySelectorAll("tr").Skip(1)];
		}

		return [.. bodyRows
			.Select(row => row.QuerySelectorAll("td, th").Select(cell => cell.TextContent.Trim()).ToList())
			.Where(cells => cells.Count > 0)];
	}

	private static List<ContentAnnotation> ExtractAnnotations(IElement element)
	{
		var annotations = new List<ContentAnnotation>();

		foreach (var link in element.QuerySelectorAll("a[href]"))
		{
			var href = link.GetAttribute("href");
			if (!string.IsNullOrEmpty(href))
			{
				annotations.Add(new ContentAnnotation
				{
					Type = AnnotationType.Link,
					Attributes = new Dictionary<string, string>
					{
						["href"] = href,
						["text"] = link.TextContent.Trim()
					}
				});
			}
		}

		annotations.AddRange(TextAnnotations(element, "strong, b", AnnotationType.Bold));
		annotations.AddRange(TextAnnotations(element, "em, i", AnnotationType.Italic));
		annotations.AddRange(TextAnnotations(element, "code:not(pre code)", AnnotationType.Code));

		return annotations;
	}

	private static IEnumerable<ContentAnnotation> TextAnnotations(IElement element, string selector, AnnotationType type)
		=> element.QuerySelectorAll(selector)
			.Select(match => match.TextContent.Trim())
			.Where(text => !string.IsNullOrEmpty(text))
			.Select(text => new ContentAnnotation
			{
				Type = type,
				Attributes = new Dictionary<string, string> { ["text"] = text }
			});
}
