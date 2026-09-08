namespace Microsoft.UI.Windowing;

/// <summary>
/// Provides data for the AppWindow.Closing event.
/// </summary>
#if HAS_UNO_WINUI
public
#else
internal
#endif
partial class AppWindowClosingEventArgs
{
	internal AppWindowClosingEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the event should be canceled.
	/// </summary>
	public bool Cancel { get; set; }
}
