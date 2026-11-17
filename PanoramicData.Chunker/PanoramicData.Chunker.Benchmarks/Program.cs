using BenchmarkDotNet.Running;

namespace PanoramicData.Chunker.Benchmarks;

/// <summary>
/// Entry point for running benchmarks.
/// </summary>
public static class Program
{
	public static void Main()
	{
		_ = BenchmarkRunner.Run(typeof(Program).Assembly);
	}
}