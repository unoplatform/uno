namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Defines the directions of navigation within the Microsoft UI Automation tree.
/// </summary>
public enum AutomationNavigationDirection
{
	/// <summary>
	/// Navigate to the parent of the current node.
	/// </summary>
	Parent = 0,

	/// <summary>
	/// Navigate to the next sibling of the current node.
	/// </summary>
	NextSibling = 1,

	/// <summary>
	/// Navigate to the previous sibling of the current node.
	/// </summary>
	PreviousSibling = 2,

	/// <summary>
	/// Navigate to the first child of the current node.
	/// </summary>
	FirstChild = 3,

	/// <summary>
	/// Navigate to the last child of the current node.
	/// </summary>
	LastChild = 4,
}
