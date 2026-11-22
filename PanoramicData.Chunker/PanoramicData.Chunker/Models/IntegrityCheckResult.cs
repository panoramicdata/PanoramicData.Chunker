namespace PanoramicData.Chunker.Models;

/// <summary>
/// Result of an integrity check, with the errors found.
/// </summary>
/// <param name="Errors">The errors found; empty when the check passed.</param>
public sealed record IntegrityCheckResult(IReadOnlyList<string> Errors)
{
	/// <summary>
	/// Gets a value indicating whether no errors were found.
	/// </summary>
	public bool IsValid => Errors.Count == 0;
}
