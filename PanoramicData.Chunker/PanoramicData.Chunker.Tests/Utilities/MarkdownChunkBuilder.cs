using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Utilities;

/// <summary>
/// Builds linked Markdown chunks (depth, ancestors and hierarchy derived from the parent) for tree tests.
/// </summary>
internal static class MarkdownChunkBuilder
{
	public static MarkdownSectionChunk Section(string heading, int level, int sequence, string specificType = "Section", ChunkerBase? parent = null)
	{
		var chunk = new MarkdownSectionChunk
		{
			SpecificType = specificType,
			HeadingLevel = level,
			HeadingText = heading
		};

		return Link(chunk, heading, sequence, parent);
	}

	public static MarkdownParagraphChunk Paragraph(string content, int sequence, ChunkerBase parent)
	{
		var chunk = new MarkdownParagraphChunk
		{
			SpecificType = "Paragraph",
			Content = content
		};

		return Link(chunk, "Paragraph", sequence, parent);
	}

	public static MarkdownCodeBlockChunk Code(string content, string language, int sequence, ChunkerBase parent)
	{
		var chunk = new MarkdownCodeBlockChunk
		{
			SpecificType = "Code",
			Content = content,
			Language = language
		};

		return Link(chunk, "Code", sequence, parent);
	}

	private static T Link<T>(T chunk, string label, int sequence, ChunkerBase? parent)
		where T : ChunkerBase
	{
		chunk.Id = Guid.NewGuid();
		chunk.ParentId = parent?.Id;
		chunk.Depth = parent == null ? 0 : parent.Depth + 1;
		chunk.SequenceNumber = sequence;
		chunk.AncestorIds = parent == null ? [] : [.. parent.AncestorIds, parent.Id];
		chunk.Metadata = new ChunkMetadata
		{
			DocumentType = "Markdown",
			Hierarchy = parent == null ? label : $"{parent.Metadata?.Hierarchy} > {label}"
		};

		return chunk;
	}
}
