namespace PanoramicData.Chunker.Models;

/// <summary>
/// A validation issue detected during chunk validation.
/// </summary>
public class ValidationIssue
{
	/// <summary>
	/// Severity of the validation issue.
	/// </summary>
	public ValidationSeverity Severity { get; set; }

	/// <summary>
	/// Description of the issue.
	/// </summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// ID of the chunk with the issue (if applicable).
	/// </summary>
	public Guid? ChunkId { get; set; }

	/// <summary>
	/// Issue code for programmatic handling.
	/// </summary>
	public string Code { get; set; } = string.Empty;
}
