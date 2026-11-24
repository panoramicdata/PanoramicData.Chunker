using System.Text;
using UglyToad.PdfPig.Content;

namespace PanoramicData.Chunker.Chunkers.Pdf;

/// <summary>
/// Builds page text from words so separately positioned blocks are never fused together.
/// </summary>
internal static class PdfPageText
{
	// Words whose baselines differ by more than this fraction of their height are on different lines
	private const double LineTolerance = 0.5;

	public static string Build(Page page)
	{
		var builder = new StringBuilder();
		Word? previous = null;

		foreach (var word in page.GetWords())
		{
			if (previous is not null)
			{
				_ = builder.Append(IsSameLine(previous, word) ? ' ' : '\n');
			}

			_ = builder.Append(word.Text);
			previous = word;
		}

		return builder.ToString();
	}

	private static bool IsSameLine(Word previous, Word current)
	{
		var height = Math.Max(previous.BoundingBox.Height, current.BoundingBox.Height);
		return Math.Abs(previous.BoundingBox.Bottom - current.BoundingBox.Bottom) <= height * LineTolerance;
	}
}
