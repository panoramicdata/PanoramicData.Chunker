using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Benchmarks;

/// <summary>
/// Benchmarks for Markdown document chunking performance.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MarkdownChunkingBenchmarks
{
	private MemoryStream _smallDocumentStream = null!;
	private MemoryStream _mediumDocumentStream = null!;
	private MemoryStream _largeDocumentStream = null!;
	private MemoryStream _veryLargeDocumentStream = null!;
	private MarkdownDocumentChunker _chunker = null!;
	private ChunkingOptions _defaultOptions = null!;
	private ChunkingOptions _largeChunkOptions = null!;

	[GlobalSetup]
	public void Setup()
	{
		// Initialize chunker
		var tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new MarkdownDocumentChunker(tokenCounter);

		// Create default options
		_defaultOptions = new ChunkingOptions
		{
			MaxTokens = 500,
			OverlapTokens = 50,
			Strategy = ChunkingStrategy.Structural,
			ValidateChunks = false  // Disable for pure parsing benchmarks
		};

		_largeChunkOptions = new ChunkingOptions
		{
			MaxTokens = 2000,
			OverlapTokens = 200,
			Strategy = ChunkingStrategy.Structural,
			ValidateChunks = false
		};

		// Generate test documents
		_smallDocumentStream = CreateDocument(GenerateSmallMarkdown());
		_mediumDocumentStream = CreateDocument(GenerateMediumMarkdown());
		_largeDocumentStream = CreateDocument(GenerateLargeMarkdown());
		_veryLargeDocumentStream = CreateDocument(GenerateVeryLargeMarkdown());
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		_smallDocumentStream?.Dispose();
		_mediumDocumentStream?.Dispose();
		_largeDocumentStream?.Dispose();
		_veryLargeDocumentStream?.Dispose();
	}

	#region Small Document Benchmarks (< 1KB)

	[Benchmark(Description = "Small Document (~500 bytes)")]
	public async Task<ChunkingResult> ChunkSmallDocument()
	{
		_smallDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_smallDocumentStream, _defaultOptions);
	}

	#endregion

	#region Medium Document Benchmarks (~10KB)

	[Benchmark(Description = "Medium Document (~10KB)")]
	public async Task<ChunkingResult> ChunkMediumDocument()
	{
		_mediumDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_mediumDocumentStream, _defaultOptions);
	}

	[Benchmark(Description = "Medium Document with Large Chunks")]
	public async Task<ChunkingResult> ChunkMediumDocumentLargeChunks()
	{
		_mediumDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_mediumDocumentStream, _largeChunkOptions);
	}

	#endregion

	#region Large Document Benchmarks (~100KB)

	[Benchmark(Description = "Large Document (~100KB)")]
	public async Task<ChunkingResult> ChunkLargeDocument()
	{
		_largeDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_largeDocumentStream, _defaultOptions);
	}

	[Benchmark(Description = "Large Document with Validation")]
	public async Task<ChunkingResult> ChunkLargeDocumentWithValidation()
	{
		_largeDocumentStream.Position = 0;
		var options = new ChunkingOptions
		{
			MaxTokens = _defaultOptions.MaxTokens,
			OverlapTokens = _defaultOptions.OverlapTokens,
			Strategy = _defaultOptions.Strategy,
			ValidateChunks = true
		};
		return await _chunker.ChunkAsync(_largeDocumentStream, options);
	}

	#endregion

	#region Very Large Document Benchmarks (~1MB)

	[Benchmark(Description = "Very Large Document (~1MB)")]
	public async Task<ChunkingResult> ChunkVeryLargeDocument()
	{
		_veryLargeDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_veryLargeDocumentStream, _defaultOptions);
	}

	#endregion

	#region Helper Methods

	private static MemoryStream CreateDocument(string markdown)
	{
		var bytes = Encoding.UTF8.GetBytes(markdown);
		return new MemoryStream(bytes);
	}

	private static string GenerateSmallMarkdown()
	{
		return @"# Introduction

This is a simple introduction paragraph.

## Features

- Feature 1
- Feature 2
- Feature 3

## Conclusion

Thank you for reading.
";
	}

	private static string GenerateMediumMarkdown()
	{
		var sb = new StringBuilder();
		
		sb.AppendLine("# Complete Guide to Markdown");
		sb.AppendLine();
		sb.AppendLine("This is a comprehensive guide to Markdown syntax and features.");
		sb.AppendLine();

		for (int i = 1; i <= 5; i++)
		{
			sb.AppendLine($"## Chapter {i}: Advanced Topics");
			sb.AppendLine();
			sb.AppendLine($"This chapter covers advanced topics in Markdown, including various formatting options and best practices for writing documentation.");
			sb.AppendLine();

			sb.AppendLine($"### Section {i}.1: Basics");
			sb.AppendLine();
			sb.AppendLine("Here are some important concepts to understand:");
			sb.AppendLine();
			sb.AppendLine("- Point one with detailed explanation");
			sb.AppendLine("- Point two with examples");
			sb.AppendLine("- Point three with code samples");
			sb.AppendLine();

			sb.AppendLine("```csharp");
			sb.AppendLine("public class Example");
			sb.AppendLine("{");
			sb.AppendLine("    public string Property { get; set; }");
			sb.AppendLine("}");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine($"### Section {i}.2: Advanced Concepts");
			sb.AppendLine();
			sb.AppendLine("More detailed content with multiple paragraphs.");
			sb.AppendLine();
			sb.AppendLine("Another paragraph with additional information.");
			sb.AppendLine();
		}

		return sb.ToString();
	}

	private static string GenerateLargeMarkdown()
	{
		var sb = new StringBuilder();
		
		sb.AppendLine("# Technical Documentation");
		sb.AppendLine();
		sb.AppendLine("## Table of Contents");
		sb.AppendLine();

		// Generate 20 chapters
		for (int chapter = 1; chapter <= 20; chapter++)
		{
			sb.AppendLine($"## Chapter {chapter}: Topic {chapter}");
			sb.AppendLine();
			sb.AppendLine($"This is the introduction to chapter {chapter}. It contains important information about various aspects of the topic.");
			sb.AppendLine();

			// 5 sections per chapter
			for (int section = 1; section <= 5; section++)
			{
				sb.AppendLine($"### Section {chapter}.{section}: Subtopic");
				sb.AppendLine();
				
				// Multiple paragraphs
				for (int para = 1; para <= 3; para++)
				{
					sb.AppendLine($"This is paragraph {para} of section {chapter}.{section}. It contains detailed explanations and examples to help understand the concept better.");
					sb.AppendLine();
				}

				// Add code block
				if (section % 2 == 0)
				{
					sb.AppendLine("```csharp");
					sb.AppendLine($"// Example code for section {chapter}.{section}");
					sb.AppendLine("public class Example");
					sb.AppendLine("{");
					sb.AppendLine("    public int Id { get; set; }");
					sb.AppendLine($"    public string Name {{ get; set; }} = \"Section {chapter}.{section}\";");
					sb.AppendLine("    ");
					sb.AppendLine("    public void Process()");
					sb.AppendLine("    {");
					sb.AppendLine("        Console.WriteLine($\"Processing {Name}\");");
					sb.AppendLine("    }");
					sb.AppendLine("}");
					sb.AppendLine("```");
					sb.AppendLine();
				}

				// Add list
				sb.AppendLine("Key points:");
				sb.AppendLine();
				for (int i = 1; i <= 5; i++)
				{
					sb.AppendLine($"- Point {i}: Important detail about the topic");
				}
				sb.AppendLine();

				// Add table occasionally
				if (section == 3)
				{
					sb.AppendLine("| Feature | Description | Status |");
					sb.AppendLine("|---------|-------------|--------|");
					sb.AppendLine("| Feature A | Description of feature A | Complete |");
					sb.AppendLine("| Feature B | Description of feature B | In Progress |");
					sb.AppendLine("| Feature C | Description of feature C | Planned |");
					sb.AppendLine();
				}
			}
		}

		return sb.ToString();
	}

	private static string GenerateVeryLargeMarkdown()
	{
		var sb = new StringBuilder();
		
		sb.AppendLine("# Comprehensive Technical Reference");
		sb.AppendLine();
		sb.AppendLine("This document contains extensive technical documentation spanning multiple topics.");
		sb.AppendLine();

		// Generate 100 sections
		for (int i = 1; i <= 100; i++)
		{
			sb.AppendLine($"## Section {i}: Component Documentation");
			sb.AppendLine();
			sb.AppendLine($"### Overview of Section {i}");
			sb.AppendLine();
			
			for (int para = 1; para <= 10; para++)
			{
				sb.AppendLine($"Paragraph {para}: This is detailed content for section {i}. It contains comprehensive information about the topic, including examples, best practices, and common pitfalls to avoid. The content is designed to be informative and practical for developers working with this technology.");
				sb.AppendLine();
			}

			sb.AppendLine("### Code Examples");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine($"// Code example for section {i}");
			sb.AppendLine("public class Component");
			sb.AppendLine("{");
			sb.AppendLine("    private readonly IService _service;");
			sb.AppendLine("    ");
			sb.AppendLine("    public Component(IService service)");
			sb.AppendLine("    {");
			sb.AppendLine("        _service = service;");
			sb.AppendLine("    }");
			sb.AppendLine("    ");
			sb.AppendLine("    public async Task<Result> ProcessAsync()");
			sb.AppendLine("    {");
			sb.AppendLine("        var data = await _service.GetDataAsync();");
			sb.AppendLine("        return Transform(data);");
			sb.AppendLine("    }");
			sb.AppendLine("}");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Implementation Details");
			sb.AppendLine();
			for (int j = 1; j <= 5; j++)
			{
				sb.AppendLine($"- Implementation point {j} with detailed explanation");
			}
			sb.AppendLine();
		}

		return sb.ToString();
	}

	#endregion
}
