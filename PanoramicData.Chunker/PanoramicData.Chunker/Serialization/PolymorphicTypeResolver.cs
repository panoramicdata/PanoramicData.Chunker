using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PanoramicData.Chunker.Chunkers.Markdown;
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
					new JsonDerivedType(typeof(MarkdownImageChunk), "MarkdownImage")

					// Note: Abstract types (StructuralChunk, ContentChunk, VisualChunk, TableChunk)
					// cannot be included as they are not instantiable.
					// Format-specific chunkers should register their concrete types here.
				}
			};
		}

		return jsonTypeInfo;
	}
}
