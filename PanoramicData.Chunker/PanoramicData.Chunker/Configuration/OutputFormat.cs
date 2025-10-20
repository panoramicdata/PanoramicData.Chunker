namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Output format for chunking results.
/// </summary>
public enum OutputFormat
{
	/// <summary>
	/// Flat list of all chunks with ParentId references.
	/// </summary>
	Flat,

	/// <summary>
	/// Hierarchical tree structure with Children populated.
	/// </summary>
	Hierarchical,

	/// <summary>
	/// Only leaf chunks (ContentChunk and VisualChunk).
	/// </summary>
	LeavesOnly
}
