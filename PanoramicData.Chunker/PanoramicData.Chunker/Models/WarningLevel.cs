namespace PanoramicData.Chunker.Models;

/// <summary>
/// Warning severity levels.
/// </summary>
public enum WarningLevel
{
	/// <summary>
	/// Informational message.
	/// </summary>
	Info,

	/// <summary>
	/// Warning that doesn't prevent processing.
	/// </summary>
	Warning,

	/// <summary>
	/// Error that may affect results.
	/// </summary>
	Error
}
