namespace Microsoft.UI.Input;

/// <summary>
/// Specifies the types of non-client regions.
/// </summary>
public enum NonClientRegionKind
{
	/// <summary>
	/// The Close button.
	/// </summary>
	Close = 0,

	/// <summary>
	/// The Maximize button
	/// </summary>
	Maximize = 1,

	/// <summary>
	/// The Minimize button.
	/// </summary>
	Minimize = 2,

	/// <summary>
	/// The application icon.
	/// </summary>
	Icon = 3,

	/// <summary>
	/// The application Title text.
	/// </summary>
	Caption = 4,

	/// <summary>
	/// The top border area.
	/// </summary>
	TopBorder = 5,

	/// <summary>
	/// The left border area.
	/// </summary>
	LeftBorder = 6,

	/// <summary>
	/// The bottom border area.
	/// </summary>
	BottomBorder = 7,

	/// <summary>
	/// The right border area.
	/// </summary>
	RightBorder = 8,

	/// <summary>
	/// The input passthrough area.
	/// </summary>
	Passthrough = 9,
}
