namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Specifies the style of text decoration applied to text (such as underlining).
/// </summary>
public enum AutomationTextDecorationLineStyle
{
	/// <summary>
	/// No text decoration is applied.
	/// </summary>
	None = 0,

	/// <summary>
	/// A single line (for example, a single underline).
	/// </summary>
	Single = 1,

	/// <summary>
	/// A single line applied only to words, not to spaces.
	/// </summary>
	WordsOnly = 2,

	/// <summary>
	/// A double line (for example, a double underline).
	/// </summary>
	Double = 3,

	/// <summary>
	/// A dotted line.
	/// </summary>
	Dot = 4,

	/// <summary>
	/// A dashed line.
	/// </summary>
	Dash = 5,

	/// <summary>
	/// A dash-dot line.
	/// </summary>
	DashDot = 6,

	/// <summary>
	/// A dash-dot-dot line.
	/// </summary>
	DashDotDot = 7,

	/// <summary>
	/// A wavy line.
	/// </summary>
	Wavy = 8,

	/// <summary>
	/// A thick single line.
	/// </summary>
	ThickSingle = 9,

	/// <summary>
	/// A double wavy line.
	/// </summary>
	DoubleWavy = 10,

	/// <summary>
	/// A thick wavy line.
	/// </summary>
	ThickWavy = 11,

	/// <summary>
	/// A long dashed line.
	/// </summary>
	LongDash = 12,

	/// <summary>
	/// A thick dashed line.
	/// </summary>
	ThickDash = 13,

	/// <summary>
	/// A thick dash-dot line.
	/// </summary>
	ThickDashDot = 14,

	/// <summary>
	/// A thick dash-dot-dot line.
	/// </summary>
	ThickDashDotDot = 15,

	/// <summary>
	/// A thick dotted line.
	/// </summary>
	ThickDot = 16,

	/// <summary>
	/// A thick long dashed line.
	/// </summary>
	ThickLongDash = 17,

	/// <summary>
	/// A text decoration style not covered by the other values.
	/// </summary>
	Other = 18,
}
