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
		return await _chunker.ChunkAsync(_smallDocumentStream, _defaultOptions, CancellationToken.None);
	}

	#endregion

	#region Medium Document Benchmarks (~10KB)

	[Benchmark(Description = "Medium Document (~10KB)")]
	public async Task<ChunkingResult> ChunkMediumDocument()
	{
		_mediumDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_mediumDocumentStream, _defaultOptions, CancellationToken.None);
	}

	[Benchmark(Description = "Medium Document with Large Chunks")]
	public async Task<ChunkingResult> ChunkMediumDocumentLargeChunks()
	{
		_mediumDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_mediumDocumentStream, _largeChunkOptions, CancellationToken.None);
	}

	#endregion

	#region Large Document Benchmarks (~100KB)

	[Benchmark(Description = "Large Document (~100KB)")]
	public async Task<ChunkingResult> ChunkLargeDocument()
	{
		_largeDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_largeDocumentStream, _defaultOptions, CancellationToken.None);
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
		return await _chunker.ChunkAsync(_largeDocumentStream, options, CancellationToken.None);
	}

	#endregion

	#region Very Large Document Benchmarks (~1MB)

	[Benchmark(Description = "Very Large Document (~1MB)")]
	public async Task<ChunkingResult> ChunkVeryLargeDocument()
	{
		_veryLargeDocumentStream.Position = 0;
		return await _chunker.ChunkAsync(_veryLargeDocumentStream, _defaultOptions, CancellationToken.None);
	}

	#endregion

	#region Helper Methods

	private static MemoryStream CreateDocument(string markdown)
	{
		var bytes = Encoding.UTF8.GetBytes(markdown);
		return new MemoryStream(bytes);
	}

	private static string GenerateSmallMarkdown() => @"# Introduction

This is a simple introduction paragraph.

## Features

- Feature 1
- Feature 2
- Feature 3

## Conclusion

Thank you for reading.
";

	private static string GenerateMediumMarkdown()
	{
		var sb = new StringBuilder();

		_ = sb.AppendLine("# Complete Guide to Markdown");
		_ = sb.AppendLine();
		_ = sb.AppendLine("This is a comprehensive guide to Markdown syntax and features.");
		_ = sb.AppendLine();

		for (var i = 1; i <= 5; i++)
		{
			_ = sb.AppendLine($"## Chapter {i}: Advanced Topics");
			_ = sb.AppendLine();
			_ = sb.AppendLine($"This chapter covers advanced topics in Markdown, including various formatting options and best practices for writing documentation.");
			_ = sb.AppendLine();

			_ = sb.AppendLine($"### Section {i}.1: Basics");
			_ = sb.AppendLine();
			_ = sb.AppendLine("Here are some important concepts to understand:");
			_ = sb.AppendLine();
			_ = sb.AppendLine("- Point one with detailed explanation");
			_ = sb.AppendLine("- Point two with examples");
			_ = sb.AppendLine("- Point three with code samples");
			_ = sb.AppendLine();

			_ = sb.AppendLine("```csharp");
			_ = sb.AppendLine("public class Example");
			_ = sb.AppendLine("{");
			_ = sb.AppendLine("    public string Property { get; set; }");
			_ = sb.AppendLine("}");
			_ = sb.AppendLine("```");
			_ = sb.AppendLine();

			_ = sb.AppendLine($"### Section {i}.2: Advanced Concepts");
			_ = sb.AppendLine();
			_ = sb.AppendLine("More detailed content with multiple paragraphs.");
			_ = sb.AppendLine();
			_ = sb.AppendLine("Another paragraph with additional information.");
			_ = sb.AppendLine();
		}

		return sb.ToString();
	}

	private static string GenerateLargeMarkdown()
	{
		var sb = new StringBuilder();

		_ = sb.AppendLine("# Technical Documentation");
		_ = sb.AppendLine();
		_ = sb.AppendLine("## Table of Contents");
		_ = sb.AppendLine();

		for (var chapter = 1; chapter <= 20; chapter++)
		{
			_ = sb.AppendLine($"## Chapter {chapter}: Topic {chapter}");
			_ = sb.AppendLine();
			_ = sb.AppendLine($"This is the introduction to chapter {chapter}. It contains important information about various aspects of the topic.");
			_ = sb.AppendLine();

			for (var section = 1; section <= 5; section++)
			{
				AppendSection(sb, chapter, section);
			}
		}

		return sb.ToString();
	}

	private static void AppendSection(StringBuilder sb, int chapter, int section)
	{
		_ = sb.AppendLine($"### Section {chapter}.{section}: Subtopic");
		_ = sb.AppendLine();

		for (var para = 1; para <= 3; para++)
		{
			_ = sb.AppendLine($"This is paragraph {para} of section {chapter}.{section}. It contains detailed explanations and examples to help understand the concept better.");
			_ = sb.AppendLine();
		}

		if (section % 2 == 0)
		{
			AppendCodeBlock(sb, chapter, section);
		}

		_ = sb.AppendLine("Key points:");
		_ = sb.AppendLine();
		for (var i = 1; i <= 5; i++)
		{
			_ = sb.AppendLine($"- Point {i}: Important detail about the topic");
		}
		_ = sb.AppendLine();

		if (section == 3)
		{
			AppendTable(sb);
		}
	}

	private static void AppendCodeBlock(StringBuilder sb, int chapter, int section)
	{
		string[] lines =
		[
			"```csharp",
			$"// Example code for section {chapter}.{section}",
			"public class Example",
			"{",
			"    public int Id { get; set; }",
			$"    public string Name {{ get; set; }} = \"Section {chapter}.{section}\";",
			"    ",
			"    public void Process()",
			"    {",
			"        Console.WriteLine($\"Processing {Name}\");",
			"    }",
			"}",
			"```"
		];

		foreach (var line in lines)
		{
			_ = sb.AppendLine(line);
		}

		_ = sb.AppendLine();
	}

	private static void AppendTable(StringBuilder sb)
	{
		string[] lines =
		[
			"| Feature | Description | Status |",
			"|---------|-------------|--------|",
			"| Feature A | Description of feature A | Complete |",
			"| Feature B | Description of feature B | In Progress |",
			"| Feature C | Description of feature C | Planned |"
		];

		foreach (var line in lines)
		{
			_ = sb.AppendLine(line);
		}

		_ = sb.AppendLine();
	}
	private static string GenerateVeryLargeMarkdown()
	{
		var sb = new StringBuilder();

		_ = sb.AppendLine("# Comprehensive Technical Reference");
		_ = sb.AppendLine();
		_ = sb.AppendLine("This document contains extensive technical documentation spanning multiple topics.");
		_ = sb.AppendLine();

		// Generate 100 sections
		for (var i = 1; i <= 100; i++)
		{
			_ = sb.AppendLine($"## Section {i}: Component Documentation");
			_ = sb.AppendLine();
			_ = sb.AppendLine($"### Overview of Section {i}");
			_ = sb.AppendLine();

			for (var para = 1; para <= 10; para++)
			{
				_ = sb.AppendLine($"Paragraph {para}: This is detailed content for section {i}. It contains comprehensive information about the topic, including examples, best practices, and common pitfalls to avoid. The content is designed to be informative and practical for developers working with this technology.");
				_ = sb.AppendLine();
			}

			_ = sb.AppendLine("### Code Examples");
			_ = sb.AppendLine();
			_ = sb.AppendLine("```csharp");
			_ = sb.AppendLine($"// Code example for section {i}");
			_ = sb.AppendLine("public class Component");
			_ = sb.AppendLine("{");
			_ = sb.AppendLine("    private readonly IService _service;");
			_ = sb.AppendLine("    ");
			_ = sb.AppendLine("    public Component(IService service)");
			_ = sb.AppendLine("    {");
			_ = sb.AppendLine("        _service = service;");
			_ = sb.AppendLine("    }");
			_ = sb.AppendLine("    ");
			_ = sb.AppendLine("    public async Task<Result> ProcessAsync()");
			_ = sb.AppendLine("    {");
			_ = sb.AppendLine("        var data = await _service.GetDataAsync();");
			_ = sb.AppendLine("        return Transform(data);");
			_ = sb.AppendLine("    }");
			_ = sb.AppendLine("}");
			_ = sb.AppendLine("```");
			_ = sb.AppendLine();

			_ = sb.AppendLine("### Implementation Details");
			_ = sb.AppendLine();
			for (var j = 1; j <= 5; j++)
			{
				_ = sb.AppendLine($"- Implementation point {j} with detailed explanation");
			}
			_ = sb.AppendLine();
		}

		return sb.ToString();
	}

	#endregion
}
