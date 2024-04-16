namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines the compact view for command bar elements.
/// </summary>
public partial interface ICommandBarElement
{
	/// <summary>
	/// Gets or sets a value that indicates the order in which a primary command
	/// in a CommandBar should be moved to the overflow menu when there is not enough
	/// room to display all primary commands.
	/// </summary>
	int DynamicOverflowOrder { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the element is shown with no label
	/// and reduced padding.
	/// </summary>
	bool IsCompact { get; set; }

	/// <summary>
	/// Gets a value that indicates whether the CommandBar command is currently located
	/// in the overflow menu
	/// </summary>
	bool IsInOverflow { get; }
}
