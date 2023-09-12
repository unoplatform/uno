namespace Microsoft.UI.Xaml;

/// <summary>
/// Contains the window's state information returned by the Window.Closed event.
/// </summary>
#if HAS_UNO_WINUI
public
#else
internal
#endif
sealed class WindowEventArgs
{
	internal WindowEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets whether a Window event was handled.
	/// </summary>
	public bool Handled { get; set; }
}
