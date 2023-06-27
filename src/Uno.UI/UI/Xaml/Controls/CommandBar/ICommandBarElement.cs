namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines the compact view for command bar elements.
/// </summary>
public partial interface ICommandBarElement
{
	/// <summary>
	/// Gets or sets a value that indicates whether the element is shown with no label and reduced padding.
	/// </summary>
	bool IsCompact { get; set; }
}
