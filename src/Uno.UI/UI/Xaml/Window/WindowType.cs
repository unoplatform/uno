namespace Uno.UI.Xaml;

/// <summary>
/// Represents the type of a window.
/// </summary>
internal enum WindowType
{
	/// <summary>
	/// Represents a Window backed by a CoreWindow (UWP-style window).
	/// </summary>
	CoreWindow,

	/// <summary>
	/// Represents a standalone Window, with a full-window XamlIsland.
	/// </summary>
	DesktopXamlSource,
}
