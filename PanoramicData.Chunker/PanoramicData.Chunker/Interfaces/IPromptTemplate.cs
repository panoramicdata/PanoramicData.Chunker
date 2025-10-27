namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for prompt template management.
/// </summary>
public interface IPromptTemplate
{
	/// <summary>
	/// Gets a template by name.
	/// </summary>
	/// <param name="templateName">The name of the template.</param>
	/// <returns>The template string.</returns>
	string GetTemplate(string templateName);

	/// <summary>
	/// Renders a template with variables.
	/// </summary>
	/// <param name="templateName">The name of the template.</param>
	/// <param name="variables">Variables to substitute in the template.</param>
	/// <returns>The rendered template.</returns>
	string RenderTemplate(string templateName, Dictionary<string, string> variables);

	/// <summary>
	/// Adds or updates a custom template.
	/// </summary>
	/// <param name="templateName">The name of the template.</param>
	/// <param name="template">The template string.</param>
	void SetTemplate(string templateName, string template);

	/// <summary>
	/// Gets all available template names.
	/// </summary>
	/// <returns>List of template names.</returns>
	IEnumerable<string> GetTemplateNames();
}
