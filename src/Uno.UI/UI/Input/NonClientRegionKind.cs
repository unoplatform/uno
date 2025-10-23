namespace Microsoft.UI.Input;

/// <summary>
/// Specifies the region of a window's non-client area where a pointer event occurred.
/// Used to identify title bar buttons, borders, caption area and passthrough regions
/// when processing non-client pointer input.
/// </summary>
public enum NonClientRegionKind
{
	/// <summary>
	/// The pointer hit the window's Close button.
	/// </summary>
	Close = 0,

	/// <summary>
	/// The pointer hit the window's Maximize (or Restore) button.
	/// </summary>
	Maximize = 1,

	/// <summary>
	/// The pointer hit the window's Minimize button.
	/// </summary>
	Minimize = 2,

	/// <summary>
	/// The pointer hit the window icon (the icon displayed in the title bar).
	/// </summary>
	Icon = 3,

	/// <summary>
	/// The pointer hit the window caption (title bar) area.
	/// </summary>
	Caption = 4,

	/// <summary>
	/// The pointer hit the top resize border of the window (non-client area).
	/// </summary>
	TopBorder = 5,

	/// <summary>
	/// The pointer hit the left resize border of the window (non-client area).
	/// </summary>
	LeftBorder = 6,

	/// <summary>
	/// The pointer hit the bottom resize border of the window (non-client area).
	/// </summary>
	BottomBorder = 7,

	/// <summary>
	/// The pointer hit the right resize border of the window (non-client area).
	/// </summary>
	RightBorder = 8,

	/// <summary>
	/// The pointer hit a passthrough region where input should be forwarded to client content
	/// rather than treated as a non-client hit.
	/// </summary>
	Passthrough = 9,
}
