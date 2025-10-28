namespace PanoramicData.Chunker.Tests;

public class BaseTest(ITestOutputHelper output)
{
	protected readonly ITestOutputHelper _output = output;

	protected static readonly CancellationToken CancellationToken = TestContext.Current.CancellationToken;
}