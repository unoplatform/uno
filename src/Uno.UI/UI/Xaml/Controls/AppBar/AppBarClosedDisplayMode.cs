namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify whether icon buttons are displayed when an app bar is not completely open.
/// </summary>
public enum AppBarClosedDisplayMode
{
	/// <summary>
	/// Icon buttons are displayed but labels are not visible.
	/// </summary>
	Compact,

	/// <summary>
	/// Only the ellipsis is displayed. Neither icon buttons nor labels are visible.
	/// </summary>
	Minimal,

	/// <summary>
	/// The app bar is not displayed.
	/// </summary>
	Hidden,
}
