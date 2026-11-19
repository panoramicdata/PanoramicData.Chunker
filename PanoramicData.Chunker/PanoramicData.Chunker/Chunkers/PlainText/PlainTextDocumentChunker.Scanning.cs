using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.PlainText;

public partial class PlainTextDocumentChunker
{
	private sealed class ScanState
	{
		public List<ChunkerBase> Chunks { get; } = [];

		public Stack<(int Level, Guid Id)> Headers { get; } = new();

		public int Index { get; set; }

		public string? PreviousLine { get; set; }

		public ChunkerBase? PreviousChunk { get; set; }

		public Guid? CurrentParentId => Headers.Count > 0 ? Headers.Peek().Id : null;
	}

	private List<ChunkerBase> ProcessLines(string[] lines)
	{
		var state = new ScanState();

		while (state.Index < lines.Length)
		{
			var line = lines[state.Index];

			if (string.IsNullOrWhiteSpace(line))
			{
				state.PreviousLine = line;
				state.Index++;
				continue;
			}

			if (TryAddHeading(lines, line, state) || TryAddListItem(line, state) || TryAddCodeBlock(lines, line, state))
			{
				continue;
			}

			TryAddParagraph(lines, state);
		}

		return state.Chunks;
	}

	private bool TryAddHeading(string[] lines, string line, ScanState state)
	{
		var index = state.Index;
		var heading = DetectHeading(lines, ref index, state.PreviousLine, state.PreviousChunk);
		state.Index = index;
		if (heading == null)
		{
			return false;
		}

		while (state.Headers.Count > 0 && state.Headers.Peek().Level >= heading.HeadingLevel)
		{
			_ = state.Headers.Pop();
		}

		heading.ParentId = state.CurrentParentId;
		heading.Depth = state.Headers.Count;
		heading.SequenceNumber = _sequenceNumber++;

		state.Headers.Push((heading.HeadingLevel, heading.Id));
		state.Chunks.Add(heading);
		state.PreviousLine = line;
		state.PreviousChunk = heading;
		return true;
	}

	private bool TryAddListItem(string line, ScanState state)
	{
		var listItem = DetectListItem(line, state.PreviousLine, state.PreviousChunk);
		if (listItem == null)
		{
			return false;
		}

		AddContent(listItem, listItem.Content, state);
		state.PreviousLine = line;
		state.Index++;
		return true;
	}

	private bool TryAddCodeBlock(string[] lines, string line, ScanState state)
	{
		var index = state.Index;
		var codeBlock = DetectCodeBlock(lines, ref index);
		state.Index = index;
		if (codeBlock == null)
		{
			return false;
		}

		AddContent(codeBlock, codeBlock.Content, state);
		state.PreviousLine = line;
		return true;
	}

	private void TryAddParagraph(string[] lines, ScanState state)
	{
		var index = state.Index;
		var paragraph = DetectParagraph(lines, ref index);
		state.Index = index;
		if (paragraph == null)
		{
			return;
		}

		AddContent(paragraph, paragraph.Content, state);
		state.PreviousLine = index > 0 ? lines[index - 1] : null;
	}

	private void AddContent(ContentChunk chunk, string text, ScanState state)
	{
		chunk.ParentId = state.CurrentParentId;
		chunk.Depth = state.Headers.Count + 1;
		chunk.SequenceNumber = _sequenceNumber++;
		PopulateQualityMetrics(chunk, text);
		state.Chunks.Add(chunk);
		state.PreviousChunk = chunk;
	}
}
