namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Supported document types.
/// </summary>
public enum DocumentType
{
	/// <summary>
	/// Microsoft Word document (.docx).
	/// </summary>
	Docx,

	/// <summary>
	/// Microsoft PowerPoint presentation (.pptx).
	/// </summary>
	Pptx,

	/// <summary>
	/// Microsoft Excel spreadsheet (.xlsx).
	/// </summary>
	Xlsx,

	/// <summary>
	/// HTML document (.html, .htm).
	/// </summary>
	Html,

	/// <summary>
	/// PDF document (.pdf).
	/// </summary>
	Pdf,

	/// <summary>
	/// Markdown document (.md).
	/// </summary>
	Markdown,

	/// <summary>
	/// Plain text document (.txt).
	/// </summary>
	PlainText,

	/// <summary>
	/// Rich Text Format document (.rtf).
	/// </summary>
	Rtf,

	/// <summary>
	/// CSV file (.csv).
	/// </summary>
	Csv,

	/// <summary>
	/// JSON file (.json).
	/// </summary>
	Json,

	/// <summary>
	/// XML file (.xml).
	/// </summary>
	Xml,

	/// <summary>
	/// Email message (.eml, .msg).
	/// </summary>
	Email,

	/// <summary>
	/// Auto-detect document type.
	/// </summary>
	Auto
}
