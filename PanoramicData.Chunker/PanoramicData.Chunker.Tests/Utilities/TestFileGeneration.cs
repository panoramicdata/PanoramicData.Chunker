namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Serialises test file generation and skips it when the marker file already exists.
/// </summary>
internal static class TestFileGeneration
{
	private static readonly object _generationLock = new();

	internal static void Run(string directory, string markerFileName, Action generate)
	{
		lock (_generationLock)
		{
			_ = Directory.CreateDirectory(directory);

			if (!File.Exists(Path.Combine(directory, markerFileName)))
			{
				generate();
			}
		}
	}
}
