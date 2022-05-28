using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input;

/// <summary>
/// Provides event data for the ContextRequested event.
/// </summary>
public partial class ContextRequestedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
{
	/// <summary>
	/// Initializes a new instance of the ContextRequestedEventArgs class.
	/// </summary>
	public ContextRequestedEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that marks the routed event as handled.
	/// A true value for Handled prevents most handlers along the event route from handling the same event again.
	/// </summary>
	public bool Handled { get; set; }

	bool IHandleableRoutedEventArgs.Handled
	{
		get => Handled;
		set => Handled = value;
	}
}
