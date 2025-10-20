namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Chunking strategies for determining chunk boundaries.
/// </summary>
public enum ChunkingStrategy
{
	/// <summary>
	/// Use document structure (headings, slides, etc.) to define boundaries.
	/// </summary>
	Structural,

	/// <summary>
	/// Use embeddings to find semantically coherent boundaries.
	/// </summary>
	Semantic,

	/// <summary>
	/// Combine structural and semantic approaches.
	/// </summary>
	Hybrid,

	/// <summary>
	/// Traditional fixed-size chunks with overlap.
	/// </summary>
	FixedSize,

	/// <summary>
	/// Sentence-boundary aware chunking.
	/// </summary>
	Sentence,

	/// <summary>
	/// Topic modeling based chunking.
	/// </summary>
	Topic
}
