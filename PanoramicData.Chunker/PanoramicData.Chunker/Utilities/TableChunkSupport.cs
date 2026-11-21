using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Utilities;

/// <summary>
/// Table text and metadata construction shared by the chunkers that build table chunks.
/// </summary>
internal static class TableChunkSupport
{
	public static string GetAllText(List<string> headers, List<List<string>> tableData)
		=> string.Join(" ", headers) + " " + string.Join(" ", tableData.SelectMany(r => r));

	public static int GetColumnCount(List<string> headers, List<List<string>> tableData)
	{
		if (headers.Count > 0)
		{
			return headers.Count;
		}

		return tableData.Count > 0 ? tableData[0].Count : 0;
	}

	public static TableMetadata CreateInfo(
		List<string> headers,
		List<List<string>> tableData,
		int columnCount,
		bool hasHeaderRow,
		bool hasMergedCells) => new()
		{
			RowCount = tableData.Count,
			ColumnCount = columnCount,
			Headers = [.. headers],
			HasHeaderRow = hasHeaderRow,
			HasMergedCells = hasMergedCells,
			PreferredFormat = TableSerializationFormat.Markdown
		};
}
