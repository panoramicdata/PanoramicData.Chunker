using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Serialization;

namespace PanoramicData.Chunker.Tests.Unit.Serialization;

/// <summary>
/// Tests for JsonChunkSerializer.
/// </summary>
public class JsonChunkSerializerTests
{
	private readonly JsonChunkSerializer _serializer;

	public JsonChunkSerializerTests()
	{
		_serializer = new JsonChunkSerializer();
	}

	[Fact]
	public async Task SerializeAsync_WithSingleChunk_ShouldProduceValidJson()
	{
		// Arrange
		var chunks = new List<ChunkerBase>
		{
			new MarkdownParagraphChunk
			{
				Content = "Test content",
				MarkdownContent = "Test content",
				SpecificType = "Paragraph",
				SequenceNumber = 0
			}
		};

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeAsync(chunks, stream);

		// Assert
		stream.Position = 0;
		var json = await new StreamReader(stream).ReadToEndAsync();
		_ = json.Should().NotBeNullOrEmpty();
		_ = json.Should().Contain("$type");
		_ = json.Should().Contain("MarkdownParagraph");
		_ = json.Should().Contain("Test content");
	}

	[Fact]
	public async Task SerializeAsync_WithMultipleChunkTypes_ShouldIncludeTypeDiscriminators()
	{
		// Arrange
		var chunks = new List<ChunkerBase>
		{
			new MarkdownSectionChunk
			{
				HeadingLevel = 1,
				HeadingText = "Header",
				SequenceNumber = 0
			},
			new MarkdownParagraphChunk
			{
				Content = "Paragraph",
				SequenceNumber = 1
			},
			new MarkdownCodeBlockChunk
			{
				Content = "code",
				Language = "csharp",
				SequenceNumber = 2
			}
		};

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeAsync(chunks, stream);

		// Assert
		stream.Position = 0;
		var json = await new StreamReader(stream).ReadToEndAsync();
		_ = json.Should().Contain("MarkdownSection");
		_ = json.Should().Contain("MarkdownParagraph");
		_ = json.Should().Contain("MarkdownCodeBlock");
	}

	[Fact]
	public async Task DeserializeAsync_WithValidJson_ShouldRecreateChunks()
	{
		// Arrange
		var original = new List<ChunkerBase>
		{
			new MarkdownParagraphChunk
			{
				Content = "Test content",
				SequenceNumber = 0
			}
		};

		using var stream = new MemoryStream();
		await _serializer.SerializeAsync(original, stream);
		stream.Position = 0;

		// Act
		var deserialized = await _serializer.DeserializeAsync(stream);

		// Assert
		_ = deserialized.Should().ContainSingle();
		var chunk = deserialized.First();
		_ = chunk.Should().BeOfType<MarkdownParagraphChunk>();
		_ = ((MarkdownParagraphChunk)chunk).Content.Should().Be("Test content");
	}

	[Fact]
	public async Task RoundTrip_WithHierarchicalChunks_ShouldPreserveRelationships()
	{
		// Arrange
		var parent = new MarkdownSectionChunk
		{
			HeadingLevel = 1,
			HeadingText = "Parent",
			SequenceNumber = 0
		};

		var child = new MarkdownParagraphChunk
		{
			Content = "Child",
			ParentId = parent.Id,
			SequenceNumber = 1
		};

		var chunks = new List<ChunkerBase> { parent, child };

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeAsync(chunks, stream);
		stream.Position = 0;
		var deserialized = await _serializer.DeserializeAsync(stream);

		// Assert
		var deserializedList = deserialized.ToList();
		_ = deserializedList.Should().HaveCount(2);
		_ = deserializedList[0].Id.Should().Be(parent.Id);
		_ = deserializedList[1].ParentId.Should().Be(parent.Id);
	}

	[Fact]
	public void SerializeToString_ShouldProduceValidJsonString()
	{
		// Arrange
		var chunks = new List<ChunkerBase>
		{
			new MarkdownParagraphChunk
			{
				Content = "Test",
				SequenceNumber = 0
			}
		};

		// Act
		var json = _serializer.SerializeToString(chunks);

		// Assert
		_ = json.Should().NotBeNullOrEmpty();
		_ = json.Should().Contain("$type");
		_ = json.Should().Contain("MarkdownParagraph");
	}

	[Fact]
	public void DeserializeFromString_ShouldRecreateChunks()
	{
		// Arrange
		var original = new List<ChunkerBase>
		{
			new MarkdownParagraphChunk
			{
				Content = "Test",
				SequenceNumber = 0
			}
		};

		var json = _serializer.SerializeToString(original);

		// Act
		var deserialized = _serializer.DeserializeFromString(json);

		// Assert
		_ = deserialized.Should().ContainSingle();
		_ = deserialized.First().Should().BeOfType<MarkdownParagraphChunk>();
	}

	[Fact]
	public void DeserializeFromString_WithEmptyString_ShouldReturnEmptyCollection()
	{
		// Act
		var result = _serializer.DeserializeFromString("");

		// Assert
		_ = result.Should().BeEmpty();
	}

	[Fact]
	public async Task SerializeResultAsync_WithFullResult_ShouldIncludeAllProperties()
	{
		// Arrange
		var result = new ChunkingResult
		{
			Chunks =
			[
				new MarkdownParagraphChunk
				{
					Content = "Test",
					SequenceNumber = 0
				}
			],
			Statistics = new ChunkingStatistics
			{
				TotalChunks = 1,
				ProcessingTime = TimeSpan.FromMilliseconds(100)
			},
			Success = true
		};

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeResultAsync(result, stream);

		// Assert
		stream.Position = 0;
		var json = await new StreamReader(stream).ReadToEndAsync();
		_ = json.Should().Contain("chunks");
		_ = json.Should().Contain("statistics");
		_ = json.Should().Contain("success");
		_ = json.Should().Contain("totalChunks");
	}

	[Fact]
	public async Task DeserializeResultAsync_ShouldRecreateChunkingResult()
	{
		// Arrange
		var original = new ChunkingResult
		{
			Chunks =
			[
				new MarkdownParagraphChunk
				{
					Content = "Test",
					SequenceNumber = 0
				}
			],
			Statistics = new ChunkingStatistics
			{
				TotalChunks = 1
			},
			Success = true
		};

		using var stream = new MemoryStream();
		await _serializer.SerializeResultAsync(original, stream);
		stream.Position = 0;

		// Act
		var deserialized = await _serializer.DeserializeResultAsync(stream);

		// Assert
		_ = deserialized.Should().NotBeNull();
		_ = deserialized!.Success.Should().BeTrue();
		_ = deserialized.Chunks.Should().ContainSingle();
		_ = deserialized.Statistics.TotalChunks.Should().Be(1);
	}

	[Fact]
	public async Task SerializeAsync_WithQualityMetrics_ShouldPreserveMetrics()
	{
		// Arrange
		var chunks = new List<ChunkerBase>
		{
			new MarkdownParagraphChunk
			{
				Content = "Test",
				SequenceNumber = 0,
				QualityMetrics = new ChunkQualityMetrics
				{
					TokenCount = 10,
					CharacterCount = 50,
					WordCount = 8,
					SemanticCompleteness = 1.0
				}
			}
		};

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeAsync(chunks, stream);
		stream.Position = 0;
		var deserialized = await _serializer.DeserializeAsync(stream);

		// Assert
		var chunk = deserialized.First();
		_ = chunk.QualityMetrics.Should().NotBeNull();
		_ = chunk.QualityMetrics!.TokenCount.Should().Be(10);
		_ = chunk.QualityMetrics.CharacterCount.Should().Be(50);
		_ = chunk.QualityMetrics.SemanticCompleteness.Should().Be(1.0);
	}

	[Fact]
	public async Task SerializeAsync_WithMetadata_ShouldPreserveAllFields()
	{
		// Arrange
		var chunks = new List<ChunkerBase>
		{
			new MarkdownParagraphChunk
			{
				Content = "Test",
				SequenceNumber = 0,
				Metadata = new ChunkMetadata
				{
					DocumentType = "Markdown",
					SourceId = "doc123",
					Hierarchy = "Section > Paragraph",
					Tags = ["test", "markdown"],
					CreatedAt = DateTimeOffset.UtcNow
				}
			}
		};

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeAsync(chunks, stream);
		stream.Position = 0;
		var deserialized = await _serializer.DeserializeAsync(stream);

		// Assert
		var chunk = deserialized.First();
		_ = chunk.Metadata.Should().NotBeNull();
		_ = chunk.Metadata.DocumentType.Should().Be("Markdown");
		_ = chunk.Metadata.SourceId.Should().Be("doc123");
		_ = chunk.Metadata.Tags.Should().Contain("test");
	}

	[Fact]
	public async Task SerializeAsync_WithTableChunk_ShouldPreserveTableStructure()
	{
		// Arrange
		var chunks = new List<ChunkerBase>
		{
			new MarkdownTableChunk
			{
				Content = "Table content",
				SequenceNumber = 0,
				TableInfo = new TableMetadata
				{
					RowCount = 2,
					ColumnCount = 3,
					Headers = ["Col1", "Col2", "Col3"]
				},
				ColumnAlignments = [TableColumnAlignment.Left, TableColumnAlignment.Center, TableColumnAlignment.Right]
			}
		};

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeAsync(chunks, stream);
		stream.Position = 0;
		var deserialized = await _serializer.DeserializeAsync(stream);

		// Assert
		var chunk = deserialized.First() as MarkdownTableChunk;
		_ = chunk.Should().NotBeNull();
		_ = chunk!.TableInfo.ColumnCount.Should().Be(3);
		_ = chunk.ColumnAlignments.Should().HaveCount(3);
	}

	[Fact]
	public async Task SerializeAsync_WithNullValues_ShouldOmitNullProperties()
	{
		// Arrange
		var chunks = new List<ChunkerBase>
		{
			new MarkdownParagraphChunk
			{
				Content = "Test",
				MarkdownContent = null, // Explicitly null
				SequenceNumber = 0
			}
		};

		using var stream = new MemoryStream();

		// Act
		await _serializer.SerializeAsync(chunks, stream);

		// Assert
		stream.Position = 0;
		var json = await new StreamReader(stream).ReadToEndAsync();
		_ = json.Should().NotContain("markdownContent"); // Should be omitted
	}
}
