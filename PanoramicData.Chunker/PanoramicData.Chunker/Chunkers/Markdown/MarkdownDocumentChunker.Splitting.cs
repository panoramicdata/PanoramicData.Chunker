using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Chunkers.Markdown;

public partial class MarkdownDocumentChunker
{
	private static readonly string[] SentenceSeparators = [". ", ".\n", "! ", "!\n", "? ", "?\n"];

	private List<ChunkerBase> SplitOversizedChunks(
		List<ChunkerBase> chunks,
		ChunkingOptions options,
		CancellationToken cancellationToken)
	{
		var result = new List<ChunkerBase>();

		foreach (var chunk in chunks)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (chunk.QualityMetrics?.TokenCount > options.MaxTokens && chunk is ContentChunk contentChunk)
			{
				result.AddRange(SplitChunk(contentChunk, options));
			}
			else
			{
				result.Add(chunk);
			}
		}

		return result;
	}

	private List<ChunkerBase> SplitChunk(ContentChunk chunk, ChunkingOptions options)
	{
		var sentences = (chunk.Content ?? string.Empty).Split(SentenceSeparators, StringSplitOptions.RemoveEmptyEntries);

		var result = new List<ChunkerBase>();
		var currentText = new StringBuilder();
		var currentTokenCount = 0;

		foreach (var sentence in sentences)
		{
			var sentenceTokens = _tokenCounter.CountTokens(sentence);

			if (currentTokenCount + sentenceTokens > options.MaxTokens && currentText.Length > 0)
			{
				result.Add(CreateSplitChunk(chunk, currentText.ToString(), currentTokenCount));
				_ = currentText.Clear();
				currentTokenCount = 0;
			}

			_ = currentText.Append(sentence).Append(". ");
			currentTokenCount += sentenceTokens;
		}

		if (currentText.Length > 0)
		{
			result.Add(CreateSplitChunk(chunk, currentText.ToString(), currentTokenCount));
		}

		return result.Count > 0 ? result : [chunk];
	}

	private MarkdownParagraphChunk CreateSplitChunk(ContentChunk original, string text, int tokenCount)
	{
		var isParagraph = original is MarkdownParagraphChunk;
		var hierarchy = isParagraph ? original.Metadata?.Hierarchy : original.SpecificType;
		List<string> tags = isParagraph ? original.Metadata?.Tags ?? [] : ["split"];

		return new MarkdownParagraphChunk
		{
			Content = text,
			MarkdownContent = text,
			ParentId = original.ParentId,
			SequenceNumber = _sequenceNumber++,
			Metadata = CreateMetadata($"{hierarchy} (Split)", tags),
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = tokenCount,
				CharacterCount = text.Length,
				WordCount = CountWords(text),
				SemanticCompleteness = 0.5,
				WasSplit = true
			}
		};
	}
}
