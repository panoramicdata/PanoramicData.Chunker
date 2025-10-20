namespace PanoramicData.Chunker.Models;

/// <summary>
/// Types of text annotations.
/// </summary>
public enum AnnotationType
{
	/// <summary>
	/// Bold text.
	/// </summary>
	Bold,

	/// <summary>
	/// Italic text.
	/// </summary>
	Italic,

	/// <summary>
	/// Underlined text.
	/// </summary>
	Underline,

	/// <summary>
	/// Strikethrough text.
	/// </summary>
	Strikethrough,

	/// <summary>
	/// Hyperlink.
	/// </summary>
	Link,

	/// <summary>
	/// Image reference.
	/// </summary>
	Image,

	/// <summary>
	/// Code or monospace text.
	/// </summary>
	Code,

	/// <summary>
	/// Highlighted text.
	/// </summary>
	Highlight,

	/// <summary>
	/// Subscript text.
	/// </summary>
	Subscript,

	/// <summary>
	/// Superscript text.
	/// </summary>
	Superscript
}
