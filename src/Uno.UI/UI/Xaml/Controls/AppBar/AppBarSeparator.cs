namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a line that separates items in an AppBar or CommandBar.
/// </summary>
public partial class AppBarSeparator : Control, ICommandBarElement, ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement
{
	/// <summary>
	/// Initializes a new instance of the AppBarSeparator class.
	/// </summary>
	public AppBarSeparator()
	{
		DefaultStyleKey = typeof(AppBarSeparator);
	}
}
