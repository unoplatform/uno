namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class ScrollBar
{
	/// <summary>
	/// Gets or sets a value that results in different input indicator modes for the ScrollBar.
	/// </summary>
	public ScrollingIndicatorMode IndicatorMode
	{
		get => (ScrollingIndicatorMode)GetValue(IndicatorModeProperty);
		set => SetValue(IndicatorModeProperty, value);
	}

	/// <summary>
	/// Identifies the IndicatorMode dependency property.
	/// </summary>
	public static DependencyProperty IndicatorModeProperty { get; } =
		DependencyProperty.Register(
			nameof(IndicatorMode),
			typeof(ScrollingIndicatorMode),
			typeof(ScrollBar),
			new FrameworkPropertyMetadata(ScrollingIndicatorMode.None));

	/// <summary>
	/// Gets or sets a value that indicates whether the ScrollBar is displayed horizontally or vertically.
	/// </summary>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the Orientation dependency property.
	/// </summary>
	public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(ScrollBar),
			new FrameworkPropertyMetadata(Orientation.Vertical)); // TODO:MZ: Default value should be Horizontal according to docs?

	/// <summary>
	/// Gets or sets the amount of the scrollable content that is currently visible.
	/// </summary>
	/// <remarks>Default is 0.0.</remarks>
	public double ViewportSize
	{
		get => (double)GetValue(ViewportSizeProperty);
		set => SetValue(ViewportSizeProperty, value);
	}

	/// <summary>
	/// Identifies the ViewportSize dependency property.
	/// </summary>
	public static DependencyProperty ViewportSizeProperty { get; } =
		DependencyProperty.Register(
			nameof(ViewportSize),
			typeof(double),
			typeof(ScrollBar),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Occurs one or more times as content scrolls in a ScrollBar when the user moves the Thumb by using the mouse.
	/// </summary>
	public event ScrollEventHandler Scroll;

	internal event Microsoft.UI.Xaml.Controls.Primitives.DragStartedEventHandler ThumbDragStarted;

	internal event Microsoft.UI.Xaml.Controls.Primitives.DragCompletedEventHandler ThumbDragCompleted;
}
