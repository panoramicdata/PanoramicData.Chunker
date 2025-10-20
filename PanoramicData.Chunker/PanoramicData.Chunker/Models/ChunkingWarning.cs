namespace PanoramicData.Chunker.Models;

/// <summary>
/// Warning or issue encountered during chunking.
/// </summary>
public class ChunkingWarning
{
	/// <summary>
	/// Severity level of the warning.
	/// </summary>
	public WarningLevel Level { get; set; }

	/// <summary>
	/// Warning message.
	/// </summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// ID of the chunk associated with this warning (if applicable).
	/// </summary>
	public Guid? ChunkId { get; set; }

	/// <summary>
	/// Warning code for programmatic handling.
	/// </summary>
	public string Code { get; set; } = string.Empty;

	/// <summary>
	/// Additional context information.
	/// </summary>
	public Dictionary<string, object>? Context { get; set; }
}
