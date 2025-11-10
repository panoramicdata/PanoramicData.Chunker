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
	Extends = 40,

	// Phase 12: Additional relationship types for better entity relationship coverage

	/// <summary>
	/// One entity studied at or attended another (educational institution).
	/// Example: "Darwin studied at Edinburgh University"
	/// </summary>
	StudiedAt = 41,

	/// <summary>
	/// One entity traveled on or journeyed aboard another (vessel, vehicle).
	/// Example: "Darwin traveled on HMS Beagle"
	/// </summary>
	TraveledOn = 42,

	/// <summary>
	/// One entity was taught by, mentored by, or guided by another.
	/// Example: "Darwin was mentored by Professor Henslow"
	/// </summary>
	MentorOf = 43,

	/// <summary>
	/// One entity presented work to or read papers before another (organization).
	/// Example: "Darwin presented to the Plinian Society"
	/// </summary>
	PresentedTo = 44,

	/// <summary>
	/// One entity visited another during a specific event or period.
	/// Example: "Darwin visited Galapagos Islands during the voyage"
	/// </summary>
	VisitedDuring = 45,

	/// <summary>
	/// One entity was born in or originated from another (location).
	/// Example: "Darwin was born in Shrewsbury"
	/// </summary>
	BornIn = 46,

	/// <summary>
	/// One entity is the father of another.
	/// </summary>
	FatherOf = 47,

	/// <summary>
	/// One entity is the mother of another.
	/// </summary>
	MotherOf = 48,

	/// <summary>
	/// One entity is the grandfather of another.
	/// </summary>
	GrandfatherOf = 49,

	/// <summary>
	/// One entity is the grandmother of another.
	/// </summary>
	GrandmotherOf = 50,

	/// <summary>
	/// One entity is married to another.
	/// </summary>
	MarriedTo = 51,

	/// <summary>
	/// One entity visited another (location, person, organization).
	/// Example: "Darwin visited the Galapagos Islands"
	/// </summary>
	Visited = 52,

	/// <summary>
	/// One entity discovered another.
	/// Example: "Darwin discovered fossils in South America"
	/// </summary>
	Discovered = 53,

	/// <summary>
	/// One entity observed or witnessed another.
	/// Example: "Darwin observed finches on the Galapagos"
	/// </summary>
	Observed = 54,

	/// <summary>
	/// One entity studied or researched another.
	/// Example: "Darwin studied barnacles for eight years"
	/// </summary>
	Studied = 55,

	/// <summary>
	/// One entity collected samples or specimens from another.
	/// Example: "Darwin collected specimens from South America"
	/// </summary>
	Collected = 56,

	/// <summary>
	/// One entity wrote or authored another work.
	/// Example: "Darwin wrote 'Voyage of the Beagle'"
	/// </summary>
	Wrote = 57,

	/// <summary>
	/// One entity developed or created another theory/concept.
	/// Example: "Darwin developed the theory of evolution"
	/// </summary>
	Developed = 58,

	/// <summary>
	/// One entity proposed another idea/theory.
	/// Example: "Darwin proposed descent with modification"
	/// </summary>
	Proposed = 59,

	/// <summary>
	/// One entity influenced the thinking or work of another.
	/// Example: "Charles Lyell influenced Darwin's thinking"
	/// </summary>
	InfluencedBy = 60,

	/// <summary>
	/// One entity lived in or resided in another location.
	/// Example: "Darwin lived in Down House"
	/// </summary>
	LivedIn = 61,

	/// <summary>
	/// One entity corresponded with or exchanged letters with another.
	/// Example: "Darwin corresponded with scientists worldwide"
	/// </summary>
	Corresponded = 62,

	/// <summary>
	/// One entity was supported by another.
	/// Example: "Darwin was supported by Thomas Huxley"
	/// </summary>
	SupportedBy = 63,

	/// <summary>
	/// One entity invited another.
	/// Example: "FitzRoy invited Darwin to join the Beagle voyage"
	/// </summary>
	Invited = 64
}
