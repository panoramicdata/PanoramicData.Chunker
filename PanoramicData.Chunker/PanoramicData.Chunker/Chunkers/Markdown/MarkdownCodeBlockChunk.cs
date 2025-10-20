using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Represents a Markdown code block chunk.
/// </summary>
public class MarkdownCodeBlockChunk : ContentChunk
{
	/// <summary>
	/// Programming language identifier (e.g., "csharp", "javascript").
	/// </summary>
	public string? Language { get; set; }

	/// <summary>
	/// Indicates if this is a fenced code block (```).
	/// </summary>
	public bool IsFenced { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MarkdownCodeBlockChunk"/> class.
	/// </summary>
	public MarkdownCodeBlockChunk()
	{
		SpecificType = "CodeBlock";
	}
}
