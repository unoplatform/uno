namespace Microsoft.UI.Xaml;

/// <summary>
/// Contains the window visibility state returned by the <see cref="Window.VisibilityChanged"/> event.
/// </summary>
public sealed partial class WindowVisibilityChangedEventArgs
{
	internal WindowVisibilityChangedEventArgs(bool visible)
	{
		Visible = visible;
	}

	/// <summary>
	/// Gets or sets whether the visibility changed event was handled.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets whether the window is visible.
	/// </summary>
	public bool Visible { get; }
}
