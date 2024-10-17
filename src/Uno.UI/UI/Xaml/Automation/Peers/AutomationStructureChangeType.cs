namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Defines the types of change in the Microsoft UI Automation tree structure.
/// </summary>
public enum AutomationStructureChangeType
{
	/// <summary>
	/// A child has been added to the current node.
	/// </summary>
	ChildAdded = 0,

	/// <summary>
	/// A child has been removed from the current node.
	/// </summary>
	ChildRemoved = 1,

	/// <summary>
	/// One or more children of the current node have been invalidated.
	/// </summary>
	ChildrenInvalidated = 2,

	/// <summary>
	/// Children have been bulk added to the current node.
	/// </summary>
	ChildrenBulkAdded = 3,

	/// <summary>
	/// Children have been bulk removed from the current node.
	/// </summary>
	ChildrenBulkRemoved = 4,

	/// <summary>
	/// The children of the current node have been reordered.
	/// </summary>
	ChildrenReordered = 5,
}

