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
	/// A person's name.
	/// </summary>
	Person = 2,

	/// <summary>
	/// An organization, company, or institution.
	/// </summary>
	Organization = 3,

	/// <summary>
	/// A geographical location (city, country, region).
	/// </summary>
	Location = 4,

	/// <summary>
	/// A specific date or time reference.
	/// </summary>
	Date = 5,

	/// <summary>
	/// A monetary amount or financial value.
	/// </summary>
	Money = 6,

	/// <summary>
	/// A percentage value.
	/// </summary>
	Percent = 7,

	/// <summary>
	/// A product or service name.
	/// </summary>
	Product = 8,

	/// <summary>
	/// A specific event.
	/// </summary>
	Event = 9,

	/// <summary>
	/// A work of art, book, movie, or creative work.
	/// </summary>
	Work = 10,

	/// <summary>
	/// A law, regulation, or legal document.
	/// </summary>
	Law = 11,

	/// <summary>
	/// A programming language or technology.
	/// </summary>
	Technology = 12,

	/// <summary>
	/// A software framework or library.
	/// </summary>
	Framework = 13,

	/// <summary>
	/// A software library or package.
	/// </summary>
	Library = 14,

	/// <summary>
	/// A specific version number or identifier.
	/// </summary>
	Version = 15,

	/// <summary>
	/// A file or document reference.
	/// </summary>
	File = 16,

	/// <summary>
	/// A URL or web address.
	/// </summary>
	Url = 17,

	/// <summary>
	/// An email address.
	/// </summary>
	Email = 18,

	/// <summary>
	/// A phone number.
	/// </summary>
	Phone = 19,

	/// <summary>
	/// A medical term, condition, or treatment.
	/// </summary>
	Medical = 20,

	/// <summary>
	/// A chemical compound or substance.
	/// </summary>
	Chemical = 21,

	/// <summary>
	/// A biological entity (species, gene, protein).
	/// </summary>
	Biological = 22,

	/// <summary>
	/// A mathematical concept or formula.
	/// </summary>
	Mathematical = 23,

	/// <summary>
	/// A scientific concept or theory.
	/// </summary>
	Scientific = 24,

	/// <summary>
	/// A business or economic concept.
	/// </summary>
	Business = 25,

	/// <summary>
	/// A legal concept or term.
	/// </summary>
	Legal = 26,

	/// <summary>
	/// An educational institution or program.
	/// </summary>
	Educational = 27,

	/// <summary>
	/// A department or division within an organization.
	/// </summary>
	Department = 28,

	/// <summary>
	/// A job title or role.
	/// </summary>
	JobTitle = 29,

	/// <summary>
	/// A skill or competency.
	/// </summary>
	Skill = 30,

	/// <summary>
	/// A certification or qualification.
	/// </summary>
	Certification = 31,

	/// <summary>
	/// A project or initiative.
	/// </summary>
	Project = 32,

	/// <summary>
	/// A task or activity.
	/// </summary>
	Task = 33,

	/// <summary>
	/// A measurement or quantity with units.
	/// </summary>
	Measurement = 34,

	/// <summary>
	/// A unit of measurement.
	/// </summary>
	Unit = 35,

	/// <summary>
	/// A currency type.
	/// </summary>
	Currency = 36,

	/// <summary>
	/// A language (natural language).
	/// </summary>
	Language = 37,

	/// <summary>
	/// A nationality or ethnic group.
	/// </summary>
	Nationality = 38,

	/// <summary>
	/// A religion or belief system.
	/// </summary>
	Religion = 39,

	/// <summary>
	/// A political party or movement.
	/// </summary>
	Political = 40,

	/// <summary>
	/// A facility or physical structure.
	/// </summary>
	Facility = 41,

	/// <summary>
	/// A vehicle or transportation method.
	/// </summary>
	Vehicle = 42,

	/// <summary>
	/// A weather or climate phenomenon.
	/// </summary>
	Weather = 43,

	/// <summary>
	/// A topic or subject area.
	/// </summary>
	Topic = 44,

	/// <summary>
	/// A concept or abstract idea.
	/// </summary>
	Concept = 45
}
