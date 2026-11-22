using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using PanoramicData.Chunker.Tests.Fixtures;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

/// <summary>
/// End-to-end integration test that processes real-world data from Project Gutenberg,
/// builds a knowledge graph, and queries it to answer questions.
/// </summary>
[Collection("PostgreSQL")]
[Trait("Category", "Integration")]
public partial class EndToEndKnowledgeGraphTests(ApacheAgeFixture fixture, ITestOutputHelper output) : IClassFixture<ApacheAgeFixture>
{
	private readonly ApacheAgeFixture _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
	private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));

	private static readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	[Fact]
	public async Task EndToEnd_ProcessGutenbergDocument_ShouldAnswerQuestionAboutPlinianSociety()
	{
		// This test demonstrates the full pipeline:
		// 1. Download HTML from Project Gutenberg
		// 2. Chunk the document using HtmlDocumentChunker
		// 3. Extract entities from chunks
		// 4. Build a knowledge graph
		// 5. Query the graph to answer: "Who founded The Plinian Society?"
		// Expected answer: Professor Jameson founded the Plinian Society (Darwin was a member)

		_output.WriteLine("=== Starting End-to-End Knowledge Graph Test ===");
		_output.WriteLine("Testing with: The Voyage of the Beagle by Charles Darwin");
		_output.WriteLine("URL: https://www.gutenberg.org/files/2010/2010-h/2010-h.htm");
		_output.WriteLine("Question: Who founded The Plinian Society?");
		_output.WriteLine("Expected Answer: Professor Jameson (Darwin was a member who attended)");
		_output.WriteLine("");

		var htmlContent = await DownloadDocumentAsync();

		var chunkingResult = await ChunkDocumentAsync(htmlContent);

		// === STEP 3: Extract Entities ===
		_output.WriteLine("Step 3: Extracting entities using Hybrid Extractor (TF-IDF + Capitalization)...");

		var entities = await ExtractEntitiesWithHybridExtractorAsync(chunkingResult);

		LogPlinianRelevantEntities(entities);

		var graph = await BuildGraphWithEntitiesAsync(entities);

		await AddRelationshipsToGraphAsync(graph, chunkingResult);

		// === STEP 5: Save to Database ===
		_output.WriteLine("Step 5: Persisting graph to PostgreSQL...");

		var graphStore = _fixture.Services.GetRequiredService<IGraphStore>();
		await graphStore.SaveGraphAsync(graph, _cancellationToken);

		_output.WriteLine("Graph saved successfully");
		_output.WriteLine("");

		// === STEP 6: Query the Graph Using Cypher ===
		_output.WriteLine("Step 6: Querying graph using Cypher to answer: 'Who founded The Plinian Society?'");
		_output.WriteLine("");

		// Get the Cypher executor from DI
		var cypherExecutor = _fixture.Services.GetRequiredService<ICypherQueryExecutor>();

		var plinianEntities = await QueryPlinianEntitiesAsync(cypherExecutor, graphStore, graph);

		await QueryPlinianRelationshipsAsync(cypherExecutor, graphStore, graph, plinianEntities);

		await QueryDarwinEntitiesAsync(cypherExecutor, graphStore, graph);

		var plinianChunks = LogPlinianChunks(chunkingResult);

		LogAnswerVerification(plinianChunks);

		// === STEP 9: Validate Graph Structure ===
		_output.WriteLine("Step 9: Validating knowledge graph structure...");

		// Load the graph for validation
		var loadedGraphForValidation = await graphStore.LoadGraphAsync(graph.Id, _cancellationToken);
		loadedGraphForValidation.Should().NotBeNull();

		var validation = loadedGraphForValidation!.Validate();
		var validationResult = validation.IsValid;

		LogGraphValidation(validationResult, [.. validation.Errors]);

		// === ASSERTIONS ===
		_output.WriteLine("=== Test Assertions ===");
		_output.WriteLine("");

		// Verify we successfully processed the document
		chunkingResult.Chunks.Should().NotBeEmpty("Should have chunked the document");
		entities.Should().NotBeEmpty("Should have extracted entities");
		graph.Entities.Should().NotBeEmpty("Graph should contain entities");

		// Verify graph was persisted and can be loaded
		loadedGraphForValidation.Should().NotBeNull("Should be able to load graph from database");
		loadedGraphForValidation.Id.Should().Be(graph.Id, "Loaded graph should have same ID");

		// Verify graph structure is valid
		validationResult.Should().BeTrue("Graph should be valid");

		VerifyPlinianDarwinRelationship(plinianEntities, loadedGraphForValidation);

		WriteEndToEndSummary(htmlContent, chunkingResult, entities, graph);
	}

	[Fact]
	public async Task EndToEnd_SmallDocument_ShouldBuildValidGraph()
	{
		// This is a simpler test with controlled input to ensure the pipeline works
		_output.WriteLine("=== Testing End-to-End Pipeline with Controlled Input ===");
		_output.WriteLine("");

		// === STEP 1: Create Test HTML ===
		var testHtml = CreateTestHtml();

		// === STEP 2: Chunk ===
		var result = await ChunkTestHtmlAsync(testHtml);

		// === STEP 3: Extract Entities ===
		var entities = await ExtractTestEntitiesAsync(result);

		// === STEP 4: Build Graph ===
		var graph = await BuildTestGraphAsync(entities, result);

		// === STEP 5: Persist ===
		var graphStore = _fixture.Services.GetRequiredService<IGraphStore>();
		await graphStore.SaveGraphAsync(graph, _cancellationToken);

		_output.WriteLine("Saved graph to database");
		_output.WriteLine("");

		// === STEP 6: Load and Verify (bypassing Cypher for now) ===
		_output.WriteLine("Step 6: Loading and verifying graph from database...");
		_output.WriteLine("");

		var (loaded, loadedDarwin, darwinRels) = await LoadAndLogDarwinAsync(graphStore, graph);

		// === STEP 7: Test Cypher Query (demonstrating future capability) ===
		await TryCypherDarwinQueryAsync();

		// === ASSERTIONS ===
		_output.WriteLine("=== Test Assertions ===");
		_output.WriteLine("");

		// Core functionality assertions
		loaded.Should().NotBeNull("Should load graph from database");
		loaded.Entities.Should().NotBeEmpty("Loaded graph should contain entities");
		loaded.Relationships.Should().NotBeEmpty("Loaded graph should contain relationships");
		loaded.Validate().IsValid.Should().BeTrue("Loaded graph should be valid");

		// Entity assertions
		loadedDarwin.Should().NotBeNull("Should find Darwin entity");
		loadedDarwin.Name.ToLower().Should().Be("darwin", "Darwin entity should have correct name (case-insensitive)");

		// Relationship assertions
		darwinRels.Should().NotBeEmpty("Darwin should have relationships");

		// Verify we can find Plinian and Jameson entities
		var plinianInLoaded = loaded.GetEntitiesByName("plinian").FirstOrDefault();
		plinianInLoaded.Should().NotBeNull("Should find Plinian entity in loaded graph");

		VerifyJamesonRelationship(loaded, plinianInLoaded);

		// Verify Darwin has a relationship with Plinian (he was a member)
		var darwinToPlinianRel = darwinRels.FirstOrDefault(r =>
			r.ToEntityId == plinianInLoaded!.Id || r.FromEntityId == plinianInLoaded.Id);
		darwinToPlinianRel.Should().NotBeNull("Darwin should have a relationship with Plinian (he was a member)");

		_output.WriteLine($"✅ Verified Darwin-Plinian relationship: {darwinToPlinianRel!.Type} (confidence: {darwinToPlinianRel.Confidence:F2})");

		WriteSmallDocumentSummary(loaded);
	}
}
