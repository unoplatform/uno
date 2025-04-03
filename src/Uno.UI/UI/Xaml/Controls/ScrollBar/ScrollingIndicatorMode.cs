namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify input-specific transition animations that are part of the default template for ScrollBar.
/// </summary>
public enum ScrollingIndicatorMode
{
	/// <summary>
	/// Do not use input-specific transitions.
	/// </summary>
	None = 0,

	/// <summary>
	/// Use input-specific transitions that are appropriate for touch input.
	/// </summary>
	TouchIndicator = 1,

	/// <summary>
	/// Use input-specific transitions that are appropriate for mouse input.
	/// </summary>
	MouseIndicator = 2,
}
