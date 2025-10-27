using PanoramicData.Chunker.Interfaces;

namespace PanoramicData.Chunker.LLM;

/// <summary>
/// Manages prompt templates for LLM enrichment.
/// </summary>
public class PromptTemplateManager : IPromptTemplate
{
	private readonly Dictionary<string, string> _templates = new()
	{
		["summarize"] = """
			Summarize the following text in {max_words} words or less. Be concise and capture the main points:

			{content}

			Summary:
			""",

		["extract_keywords"] = """
			Extract the top {max_keywords} most important keywords from the following text. Return only the keywords, comma-separated:

			{content}

			Keywords:
			""",

		["extract_entities"] = """
			Extract named entities from the following text. Identify persons, organizations, and locations. Return as JSON array with format: [{"text": "entity name", "type": "Person|Organization|Location"}]

			{content}

			Entities (JSON):
			"""
	};

	/// <inheritdoc/>
	public string GetTemplate(string templateName)
	{
		if (_templates.TryGetValue(templateName, out var template))
		{
			return template;
		}

		throw new ArgumentException($"Unknown template: {templateName}", nameof(templateName));
	}

	/// <inheritdoc/>
	public string RenderTemplate(string templateName, Dictionary<string, string> variables)
	{
		var template = GetTemplate(templateName);

		foreach (var (key, value) in variables)
		{
			template = template.Replace($"{{{key}}}", value);
		}

		return template;
	}

	/// <inheritdoc/>
	public void SetTemplate(string templateName, string template)
		=> _templates[templateName] = template;

	/// <inheritdoc/>
	public IEnumerable<string> GetTemplateNames()
		=> _templates.Keys;
}
