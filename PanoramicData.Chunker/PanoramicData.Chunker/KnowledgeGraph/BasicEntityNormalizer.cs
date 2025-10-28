using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.KnowledgeGraph;

/// <summary>
/// Basic implementation of entity normalization for comparison and deduplication.
/// </summary>
public partial class BasicEntityNormalizer : IEntityNormalizer
{
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex(@"^https?://", RegexOptions.IgnoreCase)]
	private static partial Regex UrlProtocolRegex();

	[GeneratedRegex(@"^www\.", RegexOptions.IgnoreCase)]
	private static partial Regex UrlWwwRegex();

	[GeneratedRegex(@"[^\d]")]
	private static partial Regex NonDigitRegex();

	[GeneratedRegex(@"[$€£¥₹,]")]
	private static partial Regex CurrencySymbolRegex();

	/// <inheritdoc/>
	public string Normalize(string name, EntityType entityType)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return string.Empty;
		}

		// Basic normalization: lowercase, trim, normalize whitespace
		var normalized = name.ToLowerInvariant().Trim();
		normalized = WhitespaceRegex().Replace(normalized, " ");

		// Type-specific normalization
		normalized = entityType switch
		{
			EntityType.Url => NormalizeUrl(normalized),
			EntityType.Email => NormalizeEmail(normalized),
			EntityType.Phone => NormalizePhone(normalized),
			EntityType.Date => NormalizeDate(normalized),
			EntityType.Money => NormalizeMoney(normalized),
			EntityType.Percent => NormalizePercent(normalized),
			_ => normalized
		};

		return normalized;
	}

	/// <inheritdoc/>
	public bool AreEquivalent(string name1, string name2, EntityType entityType)
	{
		if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
		{
			return false;
		}

		var normalized1 = Normalize(name1, entityType);
		var normalized2 = Normalize(name2, entityType);

		return normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase);
	}

	/// <inheritdoc/>
	public List<string> GenerateAliases(string name, EntityType entityType)
	{
		var aliases = new List<string>();

		if (string.IsNullOrWhiteSpace(name))
		{
			return aliases;
		}

		// Type-specific alias generation
		switch (entityType)
		{
			case EntityType.Person:
				aliases.AddRange(GeneratePersonAliases(name));
				break;

			case EntityType.Organization:
				aliases.AddRange(GenerateOrganizationAliases(name));
				break;

			case EntityType.Keyword:
				aliases.AddRange(GenerateKeywordAliases(name));
				break;
		}

		// Remove duplicates and the original name
		return [.. aliases
			.Where(a => !string.IsNullOrWhiteSpace(a))
			.Where(a => !a.Equals(name, StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)];
	}

	private static string NormalizeUrl(string url)
	{
		// Remove protocol, trailing slash, www prefix
		url = UrlProtocolRegex().Replace(url, "");
		url = UrlWwwRegex().Replace(url, "");
		url = url.TrimEnd('/');
		return url;
	}

	private static string NormalizeEmail(string email) =>
		// Email addresses are case-insensitive
		email.ToLowerInvariant();

	private static string NormalizePhone(string phone) =>
		// Remove all non-digit characters
		NonDigitRegex().Replace(phone, "");

	private static string NormalizeDate(string date) =>
		// Keep as-is for basic normalizer
		// More sophisticated date parsing can be added later
		date;

	private static string NormalizeMoney(string money) =>
		// Remove currency symbols and normalize
		CurrencySymbolRegex().Replace(money, "").Trim();

	private static string NormalizePercent(string percent) =>
		// Remove percent sign
		percent.Replace("%", "").Trim();

	private static List<string> GeneratePersonAliases(string name)
	{
		var aliases = new List<string>();

		// Split into parts
		var parts = name.Split([' '], StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length == 2)
		{
			// First Last -> Last, First
			aliases.Add($"{parts[1]}, {parts[0]}");
			// First Last -> F. Last
			aliases.Add($"{parts[0][0]}. {parts[1]}");
		}
		else if (parts.Length == 3)
		{
			// First Middle Last -> First Last
			aliases.Add($"{parts[0]} {parts[2]}");
			// First Middle Last -> F. M. Last
			aliases.Add($"{parts[0][0]}. {parts[1][0]}. {parts[2]}");
			// First Middle Last -> Last, First Middle
			aliases.Add($"{parts[2]}, {parts[0]} {parts[1]}");
		}

		return aliases;
	}

	private static List<string> GenerateOrganizationAliases(string name)
	{
		var aliases = new List<string>();

		// Remove common suffixes
		var suffixes = new[] { "Inc", "Inc.", "Corporation", "Corp", "Corp.", "LLC", "Ltd", "Ltd.", "Limited" };
		foreach (var suffix in suffixes)
		{
			if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
			{
				var withoutSuffix = name[..^suffix.Length].Trim().TrimEnd(',', '.');
				aliases.Add(withoutSuffix);
			}
		}

		// Generate acronym if multiple words
		var words = name.Split([' '], StringSplitOptions.RemoveEmptyEntries);
		if (words.Length is >= 2 and <= 5)
		{
			var acronym = string.Join("", words.Select(w => w[0]));
			if (acronym.Length >= 2)
			{
				aliases.Add(acronym);
			}
		}

		return aliases;
	}

	private static List<string> GenerateKeywordAliases(string name)
	{
		var aliases = new List<string>();

		// Add singular/plural variations
		if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
		{
			aliases.Add(name[..^1]); // Remove trailing 's'
		}
		else if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
		{
			aliases.Add(name[..^3] + "y"); // cities -> city
		}
		else if (name.EndsWith("es", StringComparison.OrdinalIgnoreCase))
		{
			aliases.Add(name[..^2]); // boxes -> box
		}
		else
		{
			aliases.Add(name + "s"); // Add plural
		}

		return aliases;
	}
}
