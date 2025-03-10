namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data when the user enters custom text into the ComboBox.
/// </summary>
public sealed partial class ComboBoxTextSubmittedEventArgs
{
	internal ComboBoxTextSubmittedEventArgs(string text)
	{
		Text = text;
	}

	/// <summary>
	/// Gets or sets whether the TextSubmitted event was handled or not.
	/// If true, the framework will not automatically update the selected
	/// item of the ComboBox to the new value.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets the custom text value entered by the user.
	/// </summary>
	public string Text { get; }
}
