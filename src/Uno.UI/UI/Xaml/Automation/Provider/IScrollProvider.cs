namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation client 
/// to a control that acts as a scrollable container for a collection of child objects. 
/// The children of this element must implement IScrollItemProvider. Implement IScrollProvider 
/// in order to support the capabilities that an automation client requests with a GetPattern 
/// call and PatternInterface.Scroll.
/// </summary>
public partial interface IScrollProvider
{
	/// <summary>
	/// Gets the current horizontal scroll position.
	/// </summary>
	double HorizontalScrollPercent { get; }

	/// <summary>
	/// Gets the current horizontal view size.
	/// </summary>
	double HorizontalViewSize { get; }

	/// <summary>
	/// Gets a value that indicates whether the control can scroll horizontally.
	/// </summary>
	bool HorizontallyScrollable { get; }

	/// <summary>
	/// Gets the current vertical scroll position.
	/// </summary>
	double VerticalScrollPercent { get; }

	/// <summary>
	/// Gets the vertical view size.
	/// </summary>
	double VerticalViewSize { get; }

	/// <summary>
	/// Gets a value that indicates whether the control can scroll vertically.
	/// </summary>
	bool VerticallyScrollable { get; }

	/// <summary>
	/// Scrolls the visible region of the content area horizontally, vertically, or both.
	/// </summary>
	/// <param name="horizontalAmount">
	/// The horizontal increment that is specific to the control. 
	/// Pass NoScroll if the control cannot be scrolled in this direction.
	/// </param>
	/// <param name="verticalAmount">
	/// The vertical increment that is specific to the control. 
	/// Pass NoScroll if the control cannot be scrolled in this direction.
	/// </param>
	void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);

	/// <summary>
	/// Sets the horizontal and vertical scroll position as a percentage of the total content area within the control.
	/// </summary>
	/// <param name="horizontalPercent">
	/// The horizontal position as a percentage of the content area's total range. 
	/// Pass NoScroll if the control cannot be scrolled in this direction.
	/// </param>
	/// <param name="verticalPercent">
	/// The vertical position as a percentage of the content area's total range. 
	/// Pass NoScroll if the control cannot be scrolled in this direction.
	/// </param>
	void SetScrollPercent(double horizontalPercent, double verticalPercent);
}
