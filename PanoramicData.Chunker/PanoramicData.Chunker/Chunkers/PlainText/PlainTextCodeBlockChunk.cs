using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.PlainText;

/// <summary>
/// Represents a code block chunk from a plain text document.
/// </summary>
public class PlainTextCodeBlockChunk : ContentChunk
{
	/// <summary>
	/// The detected or inferred programming language (if any).
	/// </summary>
	public string? Language { get; set; }

	/// <summary>
	/// The indentation level of this code block.
	/// </summary>
	public int IndentationLevel { get; set; }

	/// <summary>
	/// Code indicators found in the block (keywords, patterns).
	/// </summary>
	public List<string> CodeIndicators { get; set; } = [];

	/// <summary>
	/// Indicates if this was a fenced code block (```).
	/// </summary>
	public bool IsFenced { get; set; }
}
