using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Windows.UI.Xaml;

/// <summary>
/// Provides data for the UIElement.BringIntoViewRequested event.
/// </summary>
public partial class BringIntoViewRequestedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
{
	internal BringIntoViewRequestedEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that specifies whether the scrolling should be animated.
	/// </summary>
	public bool AnimationDesired { get; set; }

	/// <summary>
	/// Gets or sets a value that marks the routed event as handled.
	/// A true value prevents most handlers along the event route
	/// from handling the same event again.
	/// </summary>
	public bool Handled { get; set; }

	bool IHandleableRoutedEventArgs.Handled
	{
		get => Handled;
		set => Handled = value;
	}

	/// <summary>
	/// Gets the requested horizontal alignment ratio which controls the alignment
	/// of the vertical axis of the TargetRect with respect to the vertical axis of the viewport.
	/// </summary>
	public double HorizontalAlignmentRatio { get; internal set; } = double.NaN;

	/// <summary>
	/// Gets or sets the horizontal distance to add to the viewport-relative
	/// position of the TargetRect after satisfying the requested HorizontalAlignmentRatio.
	/// </summary>
	public double HorizontalOffset { get; set; }

	/// <summary>
	/// Gets or sets the element that should be made visible in response to the event.
	/// </summary>
	public UIElement TargetElement { get; set; }

	/// <summary>
	/// Gets or sets the Rect in the TargetElement’s coordinate space to bring into view.
	/// </summary>
	public Rect TargetRect { get; set; }

	/// <summary>
	/// Gets the requested vertical alignment ratio which controls the alignment
	/// of the horizontal axis of the TargetRect with respect to the horizontal axis of the viewport.
	/// </summary>
	public double VerticalAlignmentRatio { get; internal set; } = double.NaN;

	/// <summary>
	/// Gets or sets the vertical distance to add to the viewport-relative
	/// position of the TargetRect after satisfying the requested VerticalAlignmentRatio.
	/// </summary>
	public double VerticalOffset { get; set; }
}
