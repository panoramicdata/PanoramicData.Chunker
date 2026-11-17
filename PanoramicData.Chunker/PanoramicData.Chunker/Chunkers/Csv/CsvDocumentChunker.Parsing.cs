using System.Text;

namespace PanoramicData.Chunker.Chunkers.Csv;

public partial class CsvDocumentChunker
{
	private static readonly char[] CandidateDelimiters = [',', '\t', ';', '|'];

	private static bool ContainsDelimiter(string line) => line.IndexOfAny(CandidateDelimiters) >= 0;

	private static char DetectDelimiter(List<string> lines)
	{
		var sampleLines = lines.Take(5).ToList();
		var scores = new Dictionary<char, int>();

		foreach (var delimiter in CandidateDelimiters)
		{
			var counts = sampleLines.Select(line => CountDelimiter(line, delimiter)).ToList();

			// Consistent, non-zero counts per line earn a bonus
			scores[delimiter] = counts.Distinct().Count() == 1 && counts[0] > 0 ? counts[0] * 100 : counts.Max();
		}

		return scores.OrderByDescending(kvp => kvp.Value).First().Key;
	}

	private static int CountDelimiter(string line, char delimiter)
	{
		var count = 0;
		var inQuotes = false;

		foreach (var ch in line)
		{
			if (ch == '"')
			{
				inQuotes = !inQuotes;
			}
			else if (ch == delimiter && !inQuotes)
			{
				count++;
			}
		}

		return count;
	}

	// More than 70% non-numeric fields suggests a header
	private static bool DetectHeaderRow(List<string> fields)
	{
		var nonNumericCount = fields.Count(field => !string.IsNullOrWhiteSpace(field) && !IsNumeric(field));
		return fields.Count > 0 && (double)nonNumericCount / fields.Count >= 0.7;
	}

	private static bool IsNumeric(string value)
		=> double.TryParse(value, out _)
			|| DateTime.TryParse(value, out _)
			|| value.Equals("true", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("false", StringComparison.OrdinalIgnoreCase);

	private static List<string> ParseCsvLine(string line, char delimiter)
	{
		var fields = new List<string>();
		var currentField = new StringBuilder();
		var inQuotes = false;
		var index = 0;

		while (index < line.Length)
		{
			var ch = line[index++];

			if (ch == '"')
			{
				if (inQuotes && index < line.Length && line[index] == '"')
				{
					_ = currentField.Append('"');
					index++;
				}
				else
				{
					inQuotes = !inQuotes;
				}
			}
			else if (ch == delimiter && !inQuotes)
			{
				fields.Add(currentField.ToString().Trim());
				_ = currentField.Clear();
			}
			else
			{
				_ = currentField.Append(ch);
			}
		}

		fields.Add(currentField.ToString().Trim());

		return fields;
	}

	private static string SerializeRowAsMarkdown(List<string> headers, List<string> fields, bool hasHeader)
	{
		var sb = new StringBuilder();

		if (hasHeader && headers.Count > 0)
		{
			AppendRow(sb, headers);
			AppendRow(sb, headers.Select(_ => "---"));
		}

		AppendRow(sb, fields);

		return sb.ToString();
	}

	private static void AppendRow(StringBuilder sb, IEnumerable<string> cells)
		=> sb.Append("| ").AppendJoin(" | ", cells).AppendLine(" |");
}
