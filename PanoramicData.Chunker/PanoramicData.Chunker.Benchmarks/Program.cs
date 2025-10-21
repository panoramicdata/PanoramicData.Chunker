using BenchmarkDotNet.Running;

namespace PanoramicData.Chunker.Benchmarks;

/// <summary>
/// Entry point for running benchmarks.
/// </summary>
public class Program
{
	public static void Main(string[] args)
	{
		// Run all benchmarks
		var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
		
		// Or run specific benchmark:
		// BenchmarkRunner.Run<MarkdownChunkingBenchmarks>();
	}
}
