namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides data for the Scroll event.
/// </summary>
public partial class ScrollEventArgs : global::Microsoft.UI.Xaml.RoutedEventArgs
{
	/// <summary>
	/// Initializes a new instance of the ScrollEventArgs class.
	/// </summary>
	public ScrollEventArgs() : base()
	{
	}

	/// <summary>
	/// Gets the new Value of the ScrollBar.
	/// </summary>
	public double NewValue { get; internal set; }

	/// <summary>
	/// Gets a ScrollEventType describing the event.
	/// </summary>
	public ScrollEventType ScrollEventType { get; internal set; }
}
