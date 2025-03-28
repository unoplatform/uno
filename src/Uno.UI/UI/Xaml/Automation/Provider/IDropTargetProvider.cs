namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Enables a Microsoft UI Automation element to describe itself as an element that can receive
/// a drop of a dragged element as part of a drag-and-drop operation. Implement this interface 
/// in order to support the capabilities that an automation client requests with a GetPattern 
/// call and PatternInterface.DropTarget.
/// </summary>
public partial interface IDropTargetProvider
{
	/// <summary>
	/// Gets a string that indicates what will happen when the item is dropped.
	/// </summary>
	string DropEffect { get; }

	/// <summary>
	/// Gets an array of strings that enumerates possible drop effects when this item is dropped.
	/// </summary>
	string[] DropEffects { get; }
}
