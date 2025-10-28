namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Represents the type of relationship between two entities in the knowledge graph.
/// </summary>
public enum RelationshipType
{
	/// <summary>
	/// Unknown or unclassified relationship type.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Both entities are mentioned in the same chunk or context.
	/// </summary>
	Mentions = 1,

	/// <summary>
	/// One entity is related to another (generic relationship).
	/// </summary>
	RelatedTo = 2,

	/// <summary>
	/// One entity is part of another.
	/// </summary>
	PartOf = 3,

	/// <summary>
	/// One entity is a type or instance of another.
	/// </summary>
	IsA = 4,

	/// <summary>
	/// One entity has or possesses another.
	/// </summary>
	Has = 5,

	/// <summary>
	/// One entity uses or utilizes another.
	/// </summary>
	Uses = 6,

	/// <summary>
	/// One entity creates or produces another.
	/// </summary>
	Creates = 7,

	/// <summary>
	/// One entity works for another.
	/// </summary>
	WorksFor = 8,

	/// <summary>
	/// One entity is located in or at another.
	/// </summary>
	LocatedIn = 9,

	/// <summary>
	/// One entity owns another.
	/// </summary>
	Owns = 10,

	/// <summary>
	/// One entity manages or leads another.
	/// </summary>
	Manages = 11,

	/// <summary>
	/// One entity reports to another.
	/// </summary>
	ReportsTo = 12,

	/// <summary>
	/// Two entities collaborate or work together.
	/// </summary>
	CollaboratesWith = 13,

	/// <summary>
	/// One entity competes with another.
	/// </summary>
	CompetesWith = 14,

	/// <summary>
	/// One entity depends on another.
	/// </summary>
	DependsOn = 15,

	/// <summary>
	/// One entity causes or leads to another.
	/// </summary>
	Causes = 16,

	/// <summary>
	/// One entity prevents or inhibits another.
	/// </summary>
	Prevents = 17,

	/// <summary>
	/// One entity influences another.
	/// </summary>
	Influences = 18,

	/// <summary>
	/// One entity supports or aids another.
	/// </summary>
	Supports = 19,

	/// <summary>
	/// One entity opposes another.
	/// </summary>
	Opposes = 20,

	/// <summary>
	/// One entity is similar to another.
	/// </summary>
	SimilarTo = 21,

	/// <summary>
	/// One entity is different from another.
	/// </summary>
	DifferentFrom = 22,

	/// <summary>
	/// One entity is equivalent to another.
	/// </summary>
	EquivalentTo = 23,

	/// <summary>
	/// One entity precedes another in time.
	/// </summary>
	Precedes = 24,

	/// <summary>
	/// One entity follows another in time.
	/// </summary>
	Follows = 25,

	/// <summary>
	/// One entity occurs at the same time as another.
	/// </summary>
	CooccursWith = 26,

	/// <summary>
	/// One entity is the author or creator of another.
	/// </summary>
	AuthorOf = 27,

	/// <summary>
	/// One entity is a member of another.
	/// </summary>
	MemberOf = 28,

	/// <summary>
	/// One entity founded or established another.
	/// </summary>
	Founded = 29,

	/// <summary>
	/// One entity acquired or purchased another.
	/// </summary>
	Acquired = 30,

	/// <summary>
	/// One entity merged with another.
	/// </summary>
	MergedWith = 31,

	/// <summary>
	/// One entity is a subsidiary of another.
	/// </summary>
	SubsidiaryOf = 32,

	/// <summary>
	/// One entity is a parent company of another.
	/// </summary>
	ParentOf = 33,

	/// <summary>
	/// One entity is a competitor of another.
	/// </summary>
	CompetitorOf = 34,

	/// <summary>
	/// One entity is a supplier or vendor to another.
	/// </summary>
	SupplierOf = 35,

	/// <summary>
	/// One entity is a customer or client of another.
	/// </summary>
	CustomerOf = 36,

	/// <summary>
	/// One entity is a partner with another.
	/// </summary>
	PartnerWith = 37,

	/// <summary>
	/// One entity is derived from another.
	/// </summary>
	DerivedFrom = 38,

	/// <summary>
	/// One entity implements or realizes another.
	/// </summary>
	Implements = 39,

	/// <summary>
	/// One entity extends or inherits from another.
	/// </summary>
	Extends = 40
}
