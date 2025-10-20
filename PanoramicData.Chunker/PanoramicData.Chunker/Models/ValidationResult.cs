namespace PanoramicData.Chunker.Models;

/// <summary>
/// Result of chunk validation.
/// </summary>
public class ValidationResult
{
	/// <summary>
	/// Indicates if the chunks passed validation.
	/// </summary>
	public bool IsValid { get; set; } = true;

	/// <summary>
	/// List of validation issues.
	/// </summary>
	public List<ValidationIssue> Issues { get; set; } = [];

	/// <summary>
	/// Indicates if orphaned chunks were detected.
	/// </summary>
	public bool HasOrphanedChunks { get; set; }

	/// <summary>
	/// Indicates if circular references were detected.
	/// </summary>
	public bool HasCircularReferences { get; set; }

	/// <summary>
	/// List of chunks that exceed size limits.
	/// </summary>
	public List<ChunkerBase> OversizedChunks { get; set; } = [];

	/// <summary>
	/// List of chunks that are below minimum size.
	/// </summary>
	public List<ChunkerBase> UndersizedChunks { get; set; } = [];

	/// <summary>
	/// Indicates if hierarchy structure is invalid.
	/// </summary>
	public bool HasInvalidHierarchy { get; set; }
}
