namespace Microsoft.UI.Windowing;

/// <summary>
/// Defines constants that specify the preferred height of an app window title bar.
/// </summary>
public enum TitleBarHeightOption
{
	/// <summary>
	/// Caption buttons and the default drag region are rendered at the standard height based on system metrics.
	/// </summary>
	Standard = 0,

	/// <summary>
	/// Caption buttons and the default drag region are rendered taller than the standard height based on system metrics.
	/// </summary>
	Tall = 1,

	/// <summary>
	/// Title bar is collapsed.
	/// </summary>
	Collapsed = 2,
}
