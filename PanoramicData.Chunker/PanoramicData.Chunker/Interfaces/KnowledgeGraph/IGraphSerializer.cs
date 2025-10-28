using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for serializing and deserializing knowledge graphs.
/// </summary>
public interface IGraphSerializer
{
	/// <summary>
	/// Serializes a knowledge graph to a string.
	/// </summary>
	/// <param name="graph">The knowledge graph to serialize.</param>
	/// <returns>Serialized representation.</returns>
	string Serialize(Graph graph);

	/// <summary>
	/// Serializes a knowledge graph to a stream.
	/// </summary>
	/// <param name="graph">The knowledge graph to serialize.</param>
	/// <param name="stream">The output stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SerializeAsync(Graph graph, Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deserializes a knowledge graph from a string.
	/// </summary>
	/// <param name="data">The serialized data.</param>
	/// <returns>The deserialized knowledge graph.</returns>
	Graph Deserialize(string data);

	/// <summary>
	/// Deserializes a knowledge graph from a stream.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The deserialized knowledge graph.</returns>
	Task<Graph> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the format name of this serializer (e.g., "JSON", "XML", "GraphML").
	/// </summary>
	string FormatName { get; }

	/// <summary>
	/// Gets the file extension for this format (e.g., ".json", ".xml").
	/// </summary>
	string FileExtension { get; }
}
