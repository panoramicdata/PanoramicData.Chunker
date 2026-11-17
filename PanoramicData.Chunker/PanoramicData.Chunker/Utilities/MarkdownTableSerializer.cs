using System.Text;

namespace PanoramicData.Chunker.Utilities;

/// <summary>
/// Serialises a header and rows as a Markdown table.
/// </summary>
internal static class MarkdownTableSerializer
{
	public static string Serialize(List<string> headers, List<List<string>> rows)
	{
		var sb = new StringBuilder();

		if (headers.Count > 0)
		{
			AppendRow(sb, headers);
			AppendRow(sb, headers.Select(_ => "---"));
		}

		foreach (var row in rows)
		{
			AppendRow(sb, row);
		}

		return sb.ToString();
	}

	private static void AppendRow(StringBuilder sb, IEnumerable<string> cells)
		=> sb.Append("| ").AppendJoin(" | ", cells).AppendLine(" |");
}