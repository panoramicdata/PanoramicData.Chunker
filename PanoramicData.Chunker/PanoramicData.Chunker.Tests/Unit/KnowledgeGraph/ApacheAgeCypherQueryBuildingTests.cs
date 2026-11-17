using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.Chunker.KnowledgeGraph.Storage;
using System.Reflection;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

public class ApacheAgeCypherQueryBuildingTests
{
	private static string Build(string graphName, string cypher, Dictionary<string, object>? parameters)
	{
		var executor = new ApacheAgeCypherExecutor("Host=localhost", graphName, NullLogger<ApacheAgeCypherExecutor>.Instance);
		var method = typeof(ApacheAgeCypherExecutor).GetMethod("BuildCypherQuery", BindingFlags.Instance | BindingFlags.NonPublic)!;
		try
		{
			return (string)method.Invoke(executor, [cypher, parameters])!;
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			throw ex.InnerException;
		}
	}

	[Fact]
	public void Build_WrapsCypherInAgeSelect()
	{
		var query = Build("my_graph", "MATCH (n) RETURN n", null);

		_ = query.Should().StartWith("SELECT * FROM ag_catalog.cypher('my_graph', $$");
		_ = query.Should().Contain("MATCH (n) RETURN n");
		_ = query.Should().EndWith("$$) AS (result agtype);");
	}

	[Theory]
	[InlineData("a b")]
	[InlineData("x'); DROP TABLE t;--")]
	[InlineData("1graph")]
	[InlineData("")]
	public void Build_RejectsUnsafeGraphNames(string graphName)
	{
		var act = () => Build(graphName, "RETURN 1", null);

		_ = act.Should().Throw<InvalidOperationException>().WithMessage("*graph name*");
	}

	[Fact]
	public void Build_RejectsDollarQuoteTerminatorInCypherText()
	{
		var act = () => Build("g", "RETURN 1 $$ ; DROP TABLE x; --", null);

		_ = act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Build_EscapesQuotesBackslashesAndDollarsInStrings()
	{
		var query = Build("g", "RETURN $v", new() { ["v"] = "it's \\ $$" });

		_ = query.Should().Contain("RETURN 'it\\'s \\\\ \\u0024\\u0024'");
	}

	[Fact]
	public void Build_FormatsGuidsAsQuotedStrings()
	{
		var id = Guid.NewGuid();

		var query = Build("g", "RETURN $id", new() { ["id"] = id });

		_ = query.Should().Contain($"RETURN '{id}'");
	}

	[Fact]
	public void Build_FormatsNumbersAndBooleansInvariantly()
	{
		var original = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
		try
		{
			var query = Build("g", "RETURN $i, $d, $t, $f", new() { ["i"] = 42, ["d"] = 1.5, ["t"] = true, ["f"] = false });

			_ = query.Should().Contain("RETURN 42, 1.5, true, false");
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = original;
		}
	}

	[Fact]
	public void Build_SerialisesComplexValuesAsJson()
	{
		var query = Build("g", "RETURN $o", new() { ["o"] = new[] { 1, 2, 3 } });

		_ = query.Should().Contain("RETURN [1,2,3]");
	}
}
