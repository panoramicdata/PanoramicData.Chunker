using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Chunkers.PlainText;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Serialization;

/// <summary>
/// Configures polymorphic serialization for chunk types.
/// </summary>
internal class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
{
	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var jsonTypeInfo = base.GetTypeInfo(type, options);

		// Configure polymorphic serialization for ChunkerBase
		if (jsonTypeInfo.Type == typeof(ChunkerBase))
		{
			jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
			{
				TypeDiscriminatorPropertyName = "$type",
				IgnoreUnrecognizedTypeDiscriminators = true,
				UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
				DerivedTypes =
				{
					// Markdown chunk types (concrete types only)
					new JsonDerivedType(typeof(MarkdownSectionChunk), "MarkdownSection"),
					new JsonDerivedType(typeof(MarkdownParagraphChunk), "MarkdownParagraph"),
					new JsonDerivedType(typeof(MarkdownListItemChunk), "MarkdownListItem"),
					new JsonDerivedType(typeof(MarkdownCodeBlockChunk), "MarkdownCodeBlock"),
					new JsonDerivedType(typeof(MarkdownQuoteChunk), "MarkdownQuote"),
					new JsonDerivedType(typeof(MarkdownTableChunk), "MarkdownTable"),
					new JsonDerivedType(typeof(MarkdownImageChunk), "MarkdownImage"),

					// HTML chunk types (concrete types only)
					new JsonDerivedType(typeof(HtmlSectionChunk), "HtmlSection"),
					new JsonDerivedType(typeof(HtmlParagraphChunk), "HtmlParagraph"),
					new JsonDerivedType(typeof(HtmlListItemChunk), "HtmlListItem"),
					new JsonDerivedType(typeof(HtmlCodeBlockChunk), "HtmlCodeBlock"),
					new JsonDerivedType(typeof(HtmlBlockquoteChunk), "HtmlBlockquote"),
					new JsonDerivedType(typeof(HtmlTableChunk), "HtmlTable"),
					new JsonDerivedType(typeof(HtmlImageChunk), "HtmlImage"),

					// Plain Text chunk types
					new JsonDerivedType(typeof(PlainTextSectionChunk), "PlainTextSection"),
					new JsonDerivedType(typeof(PlainTextParagraphChunk), "PlainTextParagraph"),
					new JsonDerivedType(typeof(PlainTextListItemChunk), "PlainTextListItem"),
					new JsonDerivedType(typeof(PlainTextCodeBlockChunk), "PlainTextCodeBlock"),

					// DOCX chunk types
					new JsonDerivedType(typeof(DocxSectionChunk), "DocxSection"),
					new JsonDerivedType(typeof(DocxParagraphChunk), "DocxParagraph"),
					new JsonDerivedType(typeof(DocxListItemChunk), "DocxListItem"),
					new JsonDerivedType(typeof(DocxCodeBlockChunk), "DocxCodeBlock"),
					new JsonDerivedType(typeof(DocxTableChunk), "DocxTable"),
					new JsonDerivedType(typeof(DocxImageChunk), "DocxImage")

					// Note: Abstract types (StructuralChunk, ContentChunk, VisualChunk, TableChunk)
					// cannot be included as they are not instantiable.
					// Format-specific chunkers should register their concrete types here.
				}
			};
		}

		return jsonTypeInfo;
	}
}
