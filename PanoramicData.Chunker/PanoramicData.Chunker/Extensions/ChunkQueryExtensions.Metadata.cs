using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Extensions;

public static partial class ChunkQueryExtensions
{
	/// <summary>
	/// Filters chunks by tag.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="tag">The tag to match.</param>
	public static IEnumerable<ChunkerBase> WithTag(this IEnumerable<ChunkerBase> chunks, string tag)
		=> chunks.Where(c => c.Metadata?.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks that have any of the specified tags.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="tags">The tags to match.</param>
	public static IEnumerable<ChunkerBase> WithAnyTag(this IEnumerable<ChunkerBase> chunks, params string[] tags)
		=> chunks.Where(c => c.Metadata?.Tags?.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)) == true);

	/// <summary>
	/// Filters chunks that have all of the specified tags.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="tags">The tags that must all be present.</param>
	public static IEnumerable<ChunkerBase> WithAllTags(this IEnumerable<ChunkerBase> chunks, params string[] tags)
		=> chunks.Where(c => tags.All(tag => c.Metadata?.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true));

	/// <summary>
	/// Filters chunks on a specific page number.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="pageNumber">The page number.</param>
	public static IEnumerable<ChunkerBase> OnPage(this IEnumerable<ChunkerBase> chunks, int pageNumber)
		=> chunks.Where(c => c.Metadata?.PageNumber == pageNumber);

	/// <summary>
	/// Filters chunks within a page range.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="startPage">Starting page (inclusive).</param>
	/// <param name="endPage">Ending page (inclusive).</param>
	public static IEnumerable<ChunkerBase> OnPageRange(this IEnumerable<ChunkerBase> chunks, int startPage, int endPage)
		=> chunks.Where(c => c.Metadata?.PageNumber >= startPage && c.Metadata?.PageNumber <= endPage);

	/// <summary>
	/// Filters chunks whose hierarchy path starts with the specified prefix.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="hierarchyPrefix">The hierarchy prefix to match.</param>
	public static IEnumerable<ChunkerBase> InHierarchy(this IEnumerable<ChunkerBase> chunks, string hierarchyPrefix)
		=> chunks.Where(c => c.Metadata?.Hierarchy?.StartsWith(hierarchyPrefix, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks whose external hierarchy starts with the specified prefix.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="externalHierarchyPrefix">The external hierarchy prefix to match.</param>
	public static IEnumerable<ChunkerBase> InExternalHierarchy(
		this IEnumerable<ChunkerBase> chunks,
		string externalHierarchyPrefix)
		=> chunks.Where(c => c.Metadata?.ExternalHierarchy?.StartsWith(externalHierarchyPrefix, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks by source ID.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="sourceId">The source ID.</param>
	public static IEnumerable<ChunkerBase> FromSource(this IEnumerable<ChunkerBase> chunks, string sourceId)
		=> chunks.Where(c => c.Metadata?.SourceId?.Equals(sourceId, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks by document type.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="documentType">The document type to match.</param>
	public static IEnumerable<ChunkerBase> OfDocumentType(this IEnumerable<ChunkerBase> chunks, string documentType)
		=> chunks.Where(c => c.Metadata?.DocumentType?.Equals(documentType, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks with a specific sheet name (XLSX documents).
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="sheetName">The sheet name.</param>
	public static IEnumerable<ChunkerBase> OnSheet(this IEnumerable<ChunkerBase> chunks, string sheetName)
		=> chunks.Where(c => c.Metadata?.SheetName?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true);
}
