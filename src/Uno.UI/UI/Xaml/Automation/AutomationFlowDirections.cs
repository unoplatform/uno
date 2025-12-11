namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Specifies the direction of text flow in a container.
/// </summary>
public enum AutomationFlowDirections
{
	/// <summary>
	/// The default text flow direction.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Text flows from right to left.
	/// </summary>
	RightToLeft = 1,

	/// <summary>
	/// Text flows from bottom to top.
	/// </summary>
	BottomToTop = 2,

	/// <summary>
	/// Text flows vertically.
	/// </summary>
	Vertical = 3,
}
