using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Represents a Markdown blockquote chunk.
/// </summary>
public class MarkdownQuoteChunk : ContentChunk
{
	/// <summary>
	/// Nesting level of the quote (0 = top level).
	/// </summary>
	public int NestingLevel { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MarkdownQuoteChunk"/> class.
	/// </summary>
	public MarkdownQuoteChunk()
	{
		SpecificType = "Blockquote";
	}
}
