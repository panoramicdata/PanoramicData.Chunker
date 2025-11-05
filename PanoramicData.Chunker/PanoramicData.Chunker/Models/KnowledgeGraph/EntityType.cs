namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Represents the type of an entity in the knowledge graph.
/// </summary>
public enum EntityType
{
	/// <summary>
	/// Unknown or unclassified entity type.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// A keyword or significant term extracted from content.
	/// </summary>
	Keyword = 1,

	/// <summary>
	/// A proper noun extracted based on capitalization (person, organization, place name, etc.).
	/// May require further classification into specific subtypes.
	/// </summary>
	ProperNoun = 2,

	/// <summary>
	/// A person's name.
	/// </summary>
	Person = 3,

	/// <summary>
	/// An organization, company, or institution.
	/// </summary>
	Organization = 4,

	/// <summary>
	/// A geographical location (city, country, region).
	/// </summary>
	Location = 5,

	/// <summary>
	/// A specific date or time reference.
	/// </summary>
	Date = 6,

	/// <summary>
	/// A monetary amount or financial value.
	/// </summary>
	Money = 7,

	/// <summary>
	/// A percentage value.
	/// </summary>
	Percent = 8,

	/// <summary>
	/// A product or service name.
	/// </summary>
	Product = 9,

	/// <summary>
	/// A specific event.
	/// </summary>
	Event = 10,

	/// <summary>
	/// A work of art, book, movie, or creative work.
	/// </summary>
	Work = 11,

	/// <summary>
	/// A law, regulation, or legal document.
	/// </summary>
	Law = 12,

	/// <summary>
	/// A programming language or technology.
	/// </summary>
	Technology = 13,

	/// <summary>
	/// A software framework or library.
	/// </summary>
	Framework = 14,

	/// <summary>
	/// A software library or package.
	/// </summary>
	Library = 15,

	/// <summary>
	/// A specific version number or identifier.
	/// </summary>
	Version = 16,

	/// <summary>
	/// A file or document reference.
	/// </summary>
	File = 17,

	/// <summary>
	/// A URL or web address.
	/// </summary>
	Url = 18,

	/// <summary>
	/// An email address.
	/// </summary>
	Email = 19,

	/// <summary>
	/// A phone number.
	/// </summary>
	Phone = 20,

	/// <summary>
	/// A medical term, condition, or treatment.
	/// </summary>
	Medical = 21,

	/// <summary>
	/// A chemical compound or substance.
	/// </summary>
	Chemical = 22,

	/// <summary>
	/// A biological entity (species, gene, protein).
	/// </summary>
	Biological = 23,

	/// <summary>
	/// A mathematical concept or formula.
	/// </summary>
	Mathematical = 24,

	/// <summary>
	/// A scientific concept or theory.
	/// </summary>
	Scientific = 25,

	/// <summary>
	/// A business or economic concept.
	/// </summary>
	Business = 26,

	/// <summary>
	/// A legal concept or term.
	/// </summary>
	Legal = 27,

	/// <summary>
	/// An educational institution or program.
	/// </summary>
	Educational = 28,

	/// <summary>
	/// A department or division within an organization.
	/// </summary>
	Department = 29,

	/// <summary>
	/// A job title or role.
	/// </summary>
	JobTitle = 30,

	/// <summary>
	/// A skill or competency.
	/// </summary>
	Skill = 31,

	/// <summary>
	/// A certification or qualification.
	/// </summary>
	Certification = 32,

	/// <summary>
	/// A project or initiative.
	/// </summary>
	Project = 33,

	/// <summary>
	/// A task or activity.
	/// </summary>
	Task = 34,

	/// <summary>
	/// A measurement or quantity with units.
	/// </summary>
	Measurement = 35,

	/// <summary>
	/// A unit of measurement.
	/// </summary>
	Unit = 36,

	/// <summary>
	/// A currency type.
	/// </summary>
	Currency = 37,

	/// <summary>
	/// A language (natural language).
	/// </summary>
	Language = 38,

	/// <summary>
	/// A nationality or ethnic group.
	/// </summary>
	Nationality = 39,

	/// <summary>
	/// A religion or belief system.
	/// </summary>
	Religion = 40,

	/// <summary>
	/// A political party or movement.
	/// </summary>
	Political = 41,

	/// <summary>
	/// A facility or physical structure.
	/// </summary>
	Facility = 42,

	/// <summary>
	/// A vehicle or transportation method.
	/// </summary>
	Vehicle = 43,

	/// <summary>
	/// A weather or climate phenomenon.
	/// </summary>
	Weather = 44,

	/// <summary>
	/// A topic or subject area.
	/// </summary>
	Topic = 45,

	/// <summary>
	/// A concept or abstract idea.
	/// </summary>
	Concept = 46
}
