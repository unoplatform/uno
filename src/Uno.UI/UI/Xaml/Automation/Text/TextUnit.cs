namespace Microsoft.UI.Xaml.Automation.Text;

/// <summary>
/// Specifies units of text for the purposes of text navigation and selection.
/// </summary>
public enum TextUnit
{
	/// <summary>
	/// A single character.
	/// </summary>
	Character = 0,

	/// <summary>
	/// A unit of formatting, such as bold or italic text.
	/// </summary>
	Format = 1,

	/// <summary>
	/// A single word.
	/// </summary>
	Word = 2,

	/// <summary>
	/// A single line of text.
	/// </summary>
	Line = 3,

	/// <summary>
	/// A block of text separated by paragraph boundaries.
	/// </summary>
	Paragraph = 4,

	/// <summary>
	/// A single page of text.
	/// </summary>
	Page = 5,

	/// <summary>
	/// The entire document.
	/// </summary>
	Document = 6,
}
