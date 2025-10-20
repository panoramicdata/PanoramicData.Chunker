namespace PanoramicData.Chunker.Models;

/// <summary>
/// Validation issue severity levels.
/// </summary>
public enum ValidationSeverity
{
	/// <summary>
	/// Informational message.
	/// </summary>
	Info,

	/// <summary>
	/// Warning that doesn't prevent usage.
	/// </summary>
	Warning,

	/// <summary>
	/// Error that may affect functionality.
	/// </summary>
	Error,

	/// <summary>
	/// Critical error that prevents usage.
	/// </summary>
	Critical
}
