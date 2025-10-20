using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Represents a Markdown paragraph chunk.
/// </summary>
public class MarkdownParagraphChunk : ContentChunk
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MarkdownParagraphChunk"/> class.
	/// </summary>
	public MarkdownParagraphChunk()
	{
		SpecificType = "Paragraph";
	}
}
