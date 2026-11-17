using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests;

public class BaseTest(ITestOutputHelper output)
{
	protected readonly ITestOutputHelper _output = output;

	protected static readonly CancellationToken CancellationToken = TestContext.Current.CancellationToken;

	static BaseTest()
	{
		CsvTestFileGenerator.GenerateAllTestFiles();
		XlsxTestFileGenerator.GenerateAllTestFiles();
		PdfTestFileGenerator.GenerateAllTestFiles();
	}
}