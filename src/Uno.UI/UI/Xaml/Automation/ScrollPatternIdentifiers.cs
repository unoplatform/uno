namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IScrollProvider, and also contains the NoScroll constant.
/// </summary>
public partial class ScrollPatternIdentifiers
{
	/// <summary>
	/// Identifies the HorizontalScrollPercent automation property.
	/// </summary>
	public static AutomationProperty HorizontalScrollPercentProperty { get; } = new();

	/// <summary>
	/// Identifies the HorizontalViewSize automation property.
	/// </summary>
	public static AutomationProperty HorizontalViewSizeProperty { get; } = new();

	/// <summary>
	/// Identifies the HorizontallyScrollable automation property.
	/// </summary>
	public static AutomationProperty HorizontallyScrollableProperty { get; } = new();

	/// <summary>
	/// Specifies that scrolling should not be performed.
	/// </summary>
	public static double NoScroll { get; }

	/// <summary>
	/// Identifies the VerticalScrollPercent automation property.
	/// </summary>
	public static AutomationProperty VerticalScrollPercentProperty { get; } = new();

	/// <summary>
	/// Identifies the VerticalViewSize automation property.
	/// </summary>
	public static AutomationProperty VerticalViewSizeProperty { get; } = new();

	/// <summary>
	/// Identifies the VerticallyScrollable automation property.
	/// </summary>
	public static AutomationProperty VerticallyScrollableProperty { get; } = new();
}
