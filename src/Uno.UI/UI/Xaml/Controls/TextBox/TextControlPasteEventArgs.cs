#nullable enable

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the text control Paste event.
/// </summary>
public partial class TextControlPasteEventArgs
{
	internal TextControlPasteEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that marks the routed event as handled.
	/// A true value for Handled prevents most handlers along the event 
	/// route from handling the same event again.
	/// </summary>
	public bool Handled { get; set; }
}
