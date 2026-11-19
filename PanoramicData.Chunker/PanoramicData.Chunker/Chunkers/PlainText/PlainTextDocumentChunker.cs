using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using System.Text;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.PlainText;

/// <summary>
/// Chunks plain text documents using heuristic-based structure detection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlainTextDocumentChunker"/> class.
/// </remarks>
/// <param name="tokenCounter">Token counter for calculating chunk sizes.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public partial class PlainTextDocumentChunker(ITokenCounter tokenCounter, ILogger<PlainTextDocumentChunker>? logger = null) : IDocumentChunker
{
	private readonly ITokenCounter _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
	private int _sequenceNumber;

	/// <summary>
	/// Gets the supported document type.
	/// </summary>
	public DocumentType SupportedType => DocumentType.PlainText;

	/// <summary>
	/// Validates if the stream contains plain text.
	/// </summary>
	public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken)
	{
		if (documentStream == null || !documentStream.CanRead)
		{
			return false;
		}

		try
		{
			var originalPosition = documentStream.Position;
			documentStream.Position = 0;

			using var reader = new StreamReader(documentStream, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

			var buffer = new char[1024];
			var bytesRead = await reader.ReadAsync(buffer, cancellationToken);

			if (bytesRead == 0)
			{
				return false;
			}

			var sample = new string(buffer, 0, bytesRead);

			if (LooksLikeHtml(sample) || HasMarkdownSyntax(sample))
			{
				return false;
			}

			documentStream.Position = originalPosition;
			return PrintableRatio(sample) > 0.9;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Chunks a plain text document.
	/// </summary>
	public async Task<ChunkingResult> ChunkAsync(
		Stream documentStream,
		ChunkingOptions options,
		CancellationToken cancellationToken)
	{
		var startTime = DateTime.UtcNow;
		_sequenceNumber = 0;

		try
		{
			var text = await ReadAndNormalizeAsync(documentStream, cancellationToken);

			if (string.IsNullOrWhiteSpace(text))
			{
				return new ChunkingResult
				{
					Chunks = [],
					Statistics = ChunkResultHelper.CalculateStatistics([], startTime),
					Success = true
				};
			}

			var chunks = ProcessLines(text.Split('\n'));

			HierarchyBuilder.BuildHierarchy(chunks);

			var finalChunks = ApplyOutputFormat(chunks, options.OutputFormat);

			return ChunkResultHelper.CreateSuccess(finalChunks, startTime, options);
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking plain text document");

			return new ChunkingResult
			{
				Chunks = [],
				Statistics = ChunkResultHelper.CalculateStatistics([], startTime),
				Success = false,
				Warnings =
				[
					new ChunkingWarning
					{
						Level = WarningLevel.Error,
						Message = $"Failed to chunk document: {ex.Message}",
						Code = "PLAINTEXT_CHUNKING_ERROR"
					}
				]
			};
		}
	}

	private static bool LooksLikeHtml(string sample)
		=> sample.Contains('<') && sample.Contains('>') && HtmlTagRegex().IsMatch(sample);

	private static double PrintableRatio(string sample)
		=> sample.Count(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t') / (double)sample.Length;

	private static bool HasMarkdownSyntax(string text) =>
		text.Contains("**") || text.Contains("__") ||
			   text.Contains("```") || text.Contains("![](") ||
			   text.Contains("[](") || text.Contains("| ") ||
			   (text.Contains('#') && text.Contains('\n'));

	private static async Task<string> ReadAndNormalizeAsync(Stream stream, CancellationToken cancellationToken)
	{
		stream.Position = 0;
		using var reader = new StreamReader(stream, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		var text = await reader.ReadToEndAsync(cancellationToken);

		return text.Replace("\r\n", "\n").Replace("\r", "\n");
	}


	private static ChunkMetadata CreateMetadata(params string[] tags) => new()
	{
		DocumentType = "PlainText",
		SourceId = string.Empty,
		Hierarchy = string.Empty,
		Tags = [.. tags],
		CreatedAt = DateTimeOffset.UtcNow
	};

	private static List<ChunkerBase> ApplyOutputFormat(List<ChunkerBase> chunks, OutputFormat format) => format switch
	{
		OutputFormat.Flat => chunks,
		OutputFormat.Hierarchical => BuildHierarchicalStructure(chunks),
		OutputFormat.LeavesOnly => [.. chunks.Where(c => c is ContentChunk)],
		_ => chunks
	};

	private static List<ChunkerBase> BuildHierarchicalStructure(List<ChunkerBase> flatChunks)
	{
		foreach (var chunk in flatChunks.OfType<StructuralChunk>())
		{
			chunk.Children.Clear();
			chunk.Children.AddRange(flatChunks.Where(c => c.ParentId == chunk.Id));
		}

		return [.. flatChunks.Where(c => c.ParentId == null)];
	}

	[GeneratedRegex(@"<[a-zA-Z][^>]*>")]
	private static partial Regex HtmlTagRegex();
}
