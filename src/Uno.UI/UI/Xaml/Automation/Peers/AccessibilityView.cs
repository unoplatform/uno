namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Declares how a control should included in different views of a Microsoft UI Automation tree.
/// </summary>
public enum AccessibilityView
{
	/// <summary>
	/// The control is included in the Raw view of a Microsoft UI Automation tree.
	/// </summary>
	Raw = 0,

	/// <summary>
	/// The control is included in the Control view of a Microsoft UI Automation tree.
	/// </summary>
	Control = 1,

	/// <summary>
	/// The control is included in the Content view of a Microsoft UI Automation tree. This is the default.
	/// </summary>
	Content = 2,
}

