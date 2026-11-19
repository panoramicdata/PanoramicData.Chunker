using Markdig.Extensions.Tables;
using System.Text;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Headers, rows and column alignments read from a Markdig table.
/// </summary>
internal sealed class MarkdownTableContent
{
	public List<string> Headers { get; } = [];

	public List<List<string>> Rows { get; } = [];

	public List<TableColumnAlignment> Alignments { get; } = [];

	public static MarkdownTableContent Read(Table table)
	{
		var content = new MarkdownTableContent();

		if (table.ColumnDefinitions != null)
		{
			content.Alignments.AddRange(table.ColumnDefinitions.Select(c => ToAlignment(c.Alignment)));
		}

		foreach (var tableRow in table.OfType<TableRow>())
		{
			var rowData = tableRow.OfType<TableCell>().Select(MarkdownInlineText.GetBlockText).ToList();
			if (tableRow.IsHeader)
			{
				content.Headers.AddRange(rowData);
			}
			else
			{
				content.Rows.Add(rowData);
			}
		}

		return content;
	}

	public string ToText()
	{
		var text = new StringBuilder();
		_ = text.AppendLine(string.Join(" | ", Headers));
		foreach (var row in Rows)
		{
			_ = text.AppendLine(string.Join(" | ", row));
		}

		return text.ToString();
	}

	public string ToMarkdown()
	{
		var md = new StringBuilder();
		_ = md.AppendLine($"| {string.Join(" | ", Headers)} |");

		var separators = Alignments.Count == Headers.Count
			? Alignments.Select(ToSeparator)
			: Headers.Select(_ => "---");

		_ = md.AppendLine($"| {string.Join(" | ", separators)} |");

		foreach (var row in Rows)
		{
			_ = md.AppendLine($"| {string.Join(" | ", row)} |");
		}

		return md.ToString();
	}

	public string AllText() => string.Join(" ", Headers) + " " + string.Join(" ", Rows.SelectMany(r => r));

	private static TableColumnAlignment ToAlignment(TableColumnAlign? align) => align switch
	{
		TableColumnAlign.Left => TableColumnAlignment.Left,
		TableColumnAlign.Center => TableColumnAlignment.Center,
		TableColumnAlign.Right => TableColumnAlignment.Right,
		_ => TableColumnAlignment.None
	};

	private static string ToSeparator(TableColumnAlignment alignment) => alignment switch
	{
		TableColumnAlignment.Left => ":---",
		TableColumnAlignment.Center => ":---:",
		TableColumnAlignment.Right => "---:",
		_ => "---"
	};
}
