using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Simple concrete implementation of ContentChunk for testing purposes.
/// This class exists solely to allow instantiation of ContentChunk in unit tests,
/// since ContentChunk is abstract in the main library.
/// </summary>
public class TestContentChunk : ContentChunk
{
	// No additional implementation needed - just makes ContentChunk concrete for testing
}
