namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines members to manage the command bar overflow menu.
/// </summary>
public partial interface ICommandBarElement2
{
	/// <summary>
	/// Gets or sets the order in which this item is moved to the CommandBar overflow menu.
	/// </summary>
	int DynamicOverflowOrder { get; set; }

	/// <summary>
	/// Gets a value that indicates whether this item is in the overflow menu.
	/// </summary>
	bool IsInOverflow { get; }
}
