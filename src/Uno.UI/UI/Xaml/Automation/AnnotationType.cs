namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Specifies the type of an annotation in a document.
/// </summary>
public enum AnnotationType
{
	/// <summary>
	/// An unknown annotation type.
	/// </summary>
	Unknown = 60000,

	/// <summary>
	/// A spelling error annotation.
	/// </summary>
	SpellingError = 60001,

	/// <summary>
	/// A grammar error annotation.
	/// </summary>
	GrammarError = 60002,

	/// <summary>
	/// A comment annotation.
	/// </summary>
	Comment = 60003,

	/// <summary>
	/// A formula error annotation.
	/// </summary>
	FormulaError = 60004,

	/// <summary>
	/// A tracked change annotation.
	/// </summary>
	TrackChanges = 60005,

	/// <summary>
	/// A header annotation.
	/// </summary>
	Header = 60006,

	/// <summary>
	/// A footer annotation.
	/// </summary>
	Footer = 60007,

	/// <summary>
	/// A highlight annotation.
	/// </summary>
	Highlighted = 60008,

	/// <summary>
	/// An endnote annotation.
	/// </summary>
	Endnote = 60009,

	/// <summary>
	/// A footnote annotation.
	/// </summary>
	Footnote = 60010,

	/// <summary>
	/// An insertion change annotation.
	/// </summary>
	InsertionChange = 60011,

	/// <summary>
	/// A deletion change annotation.
	/// </summary>
	DeletionChange = 60012,

	/// <summary>
	/// A move change annotation.
	/// </summary>
	MoveChange = 60013,

	/// <summary>
	/// A format change annotation.
	/// </summary>
	FormatChange = 60014,

	/// <summary>
	/// An unsynced change annotation.
	/// </summary>
	UnsyncedChange = 60015,

	/// <summary>
	/// An editing-locked change annotation.
	/// </summary>
	EditingLockedChange = 60016,

	/// <summary>
	/// An external change annotation.
	/// </summary>
	ExternalChange = 60017,

	/// <summary>
	/// A conflicting change annotation.
	/// </summary>
	ConflictingChange = 60018,

	/// <summary>
	/// An author annotation.
	/// </summary>
	Author = 60019,

	/// <summary>
	/// An advanced proofing issue annotation.
	/// </summary>
	AdvancedProofingIssue = 60020,

	/// <summary>
	/// A data validation error annotation.
	/// </summary>
	DataValidationError = 60021,

	/// <summary>
	/// A circular reference error annotation.
	/// </summary>
	CircularReferenceError = 60022,
}
