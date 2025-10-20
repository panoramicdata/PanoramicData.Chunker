namespace PanoramicData.Chunker.Models;

/// <summary>
/// Represents large, non-embeddable structural groupings (Sections, Pages, Slides).
/// </summary>
public abstract class StructuralChunk : ChunkerBase
{
	/// <summary>
	/// Direct child chunks nested under this structural unit.
	/// Note: This is populated only when using hierarchical output mode.
	/// </summary>
	public List<ChunkerBase> Children { get; set; } = [];

	/// <summary>
	/// Optional summary of the content within this structural chunk.
	/// Generated when ChunkingOptions.GenerateSummaries is enabled.
	/// </summary>
	public string? Summary { get; set; }
}
