using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

public partial class ApacheAgeCypherExecutor
{
	/// <summary>
	/// Builds a Cypher query for Apache AGE, inlining escaped parameter values.
	/// </summary>
	private string BuildCypherQuery(string cypherQuery, Dictionary<string, object>? parameters)
	{
		if (!GraphNameRegex().IsMatch(_graphName))
		{
			throw new InvalidOperationException($"Invalid Apache AGE graph name '{_graphName}'. Use letters, digits and underscores only.");
		}

		if (cypherQuery.Contains("$$", StringComparison.Ordinal))
		{
			throw new ArgumentException("Cypher text must not contain a dollar-quote terminator.", nameof(cypherQuery));
		}

		// Apache AGE requires a constant graph name, so the validated name is part of the statement text
		var query = string.Concat("SELECT * FROM ag_catalog.cypher('", _graphName, "', $$\n", cypherQuery, "\n$$) AS (result agtype);");

		if (parameters != null)
		{
			foreach (var param in parameters)
			{
				query = query.Replace($"${param.Key}", FormatParameterValue(param.Value), StringComparison.Ordinal);
			}
		}

		return query;
	}

	private static string FormatParameterValue(object? value) => value switch
	{
		string s => $"'{EscapeCypherString(s)}'",
		Guid g => $"'{g}'",
		int i => i.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		bool b => b ? "true" : "false",
		_ => JsonSerializer.Serialize(value).Replace("$", "\\u0024", StringComparison.Ordinal)
	};

	private static string EscapeCypherString(string value)
	{
		var builder = new StringBuilder(value.Length);
		foreach (var c in value)
		{
			_ = c switch
			{
				'\\' => builder.Append("\\\\"),
				'\'' => builder.Append("\\'"),
				'$' => builder.Append("\\u0024"),
				_ => builder.Append(c)
			};
		}

		return builder.ToString();
	}
}
