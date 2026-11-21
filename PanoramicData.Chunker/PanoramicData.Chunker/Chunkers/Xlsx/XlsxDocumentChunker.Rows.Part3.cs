using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

public partial class XlsxDocumentChunker
{
	[GeneratedRegex(@"[A-Z]+", RegexOptions.None)]
	private static partial Regex ColumnNameRegex();

	[GeneratedRegex(@"^=?([A-Z]+)\(", RegexOptions.None)]
	private static partial Regex FormulaTypeRegex();

	[GeneratedRegex(@"[A-Z]+\d+(?::[A-Z]+\d+)?", RegexOptions.None)]
	private static partial Regex CellReferenceRegex();

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => ChunkFactory.CreateQualityMetrics(_tokenCounter, text, ChunkFactory.CountSpaceSeparatedWords(text));
}
