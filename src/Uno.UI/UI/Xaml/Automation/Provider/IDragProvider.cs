namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Enables a Microsoft UI Automation element to describe itself as an element that can 
/// be dragged as part of a drag-and-drop operation. Implement this interface in order to
/// support the capabilities that an automation client requests with a GetPattern call and 
/// PatternInterface.Drag.
/// </summary>
public partial interface IDragProvider
{
	/// <summary>
	/// Gets a string that indicates what will happen when the item is dropped.
	/// </summary>
	string DropEffect { get; }

	/// <summary>
	/// Gets an array of strings that enumerates possible drop effects when this item is dropped.
	/// </summary>
	string[] DropEffects { get; }

	/// <summary>
	/// Gets a value indicating whether an item is currently being dragged.
	/// </summary>
	bool IsGrabbed { get; }

	/// <summary>
	/// Gets an array of UI Automation elements that are being dragged as part of this drag operation.
	/// </summary>
	/// <returns>
	/// An array of UI Automation elements that are being dragged. Null if this item is an 
	/// individual item being dragged. Used to enable providers that support dragging multiple items 
	/// at a time to create an intermediary IDragProvider that encapsulates all of the items being 
	/// dragged.
	/// </returns>
	IRawElementProviderSimple[] GetGrabbedItems();
}
