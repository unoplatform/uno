#nullable enable

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents the method that will handle a Paste event.
/// </summary>
/// <param name="sender">The object where the handler is attached.</param>
/// <param name="e">Event data for the event.</param>
public delegate void TextControlPasteEventHandler(object sender, TextControlPasteEventArgs e);
