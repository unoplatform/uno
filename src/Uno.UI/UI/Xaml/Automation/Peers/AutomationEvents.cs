namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Specifies the event that is raised by the element through the associated AutomationPeer. 
/// Used by RaiseAutomationEvent.
/// </summary>
public enum AutomationEvents
{
	/// <summary>
	/// The event that is raised when a tooltip is opened.
	/// </summary>
	ToolTipOpened = 0,

	/// <summary>
	/// The event that is raised when a tooltip is closed.
	/// </summary>
	ToolTipClosed = 1,

	/// <summary>
	/// The event that is raised when a menu is opened.
	/// </summary>
	MenuOpened = 2,

	/// <summary>
	/// The event that is raised when a menu is closed.
	/// </summary>
	MenuClosed = 3,

	/// <summary>
	/// The event that is raised when the focus has changed. 
	/// </summary>
	AutomationFocusChanged = 4,

	/// <summary>
	/// The event that is raised when a control is activated.
	/// </summary>
	InvokePatternOnInvoked = 5,

	/// <summary>
	/// The event that is raised when an item is added to a collection of selected items.
	/// </summary>
	SelectionItemPatternOnElementAddedToSelection = 6,

	/// <summary>
	/// The event that is raised when an item is removed from a collection of selected items.
	/// </summary>
	SelectionItemPatternOnElementRemovedFromSelection = 7,

	/// <summary>
	/// The event that is raised when a single item is selected (which clears any previous selection).
	/// </summary>
	SelectionItemPatternOnElementSelected = 8,

	/// <summary>
	/// The event that is raised when a selection in a container has changed significantly.
	/// </summary>
	SelectionPatternOnInvalidated = 9,

	/// <summary>
	/// The event that is raised when the text selection is modified.
	/// </summary>
	TextPatternOnTextSelectionChanged = 10,

	/// <summary>
	/// The event that is raised when textual content is modified.
	/// </summary>
	TextPatternOnTextChanged = 11,

	/// <summary>
	/// The event that is raised when content is loaded asynchronously.
	/// </summary>
	AsyncContentLoaded = 12,

	/// <summary>
	/// The event that is raised when a property has changed.
	/// </summary>
	PropertyChanged = 13,

	/// <summary>
	/// The event that is raised when the UI Automation tree structure is changed.
	/// </summary>
	StructureChanged = 14,

	/// <summary>
	/// The event that is raised when a drag operation originates from a peer.
	/// </summary>
	DragStart = 15,

	/// <summary>
	/// The event that is raised when a drag operation is canceled from a peer.
	/// </summary>
	DragCancel = 16,

	/// <summary>
	/// The event that is raised when a drag operation finishes from a peer.
	/// </summary>
	DragComplete = 17,

	/// <summary>
	/// The event that is raised when a drag operation targets a peer.
	/// </summary>
	DragEnter = 18,

	/// <summary>
	/// The event that is raised when a drag operation switches targets away from a peer.
	/// </summary>
	DragLeave = 19,

	/// <summary>
	/// The event that is raised when a drag operation drops on a peer.
	/// </summary>
	Dropped = 20,

	/// <summary>
	/// The event that is raised as notification when a live region refreshes its content without having focus.
	/// </summary>
	LiveRegionChanged = 21,

	/// <summary>
	/// The event that is raised when user input has reached its target.
	/// </summary>
	InputReachedTarget = 22,

	/// <summary>
	/// The event that is raised when user input has reached the other element.
	/// </summary>
	InputReachedOtherElement = 23,

	/// <summary>
	/// The event that is raised when user input has been discarded.
	/// </summary>
	InputDiscarded = 24,

	/// <summary>
	/// The event that is raised when a window is closed.
	/// </summary>
	WindowClosed = 25,

	/// <summary>
	/// The event that is raised when a window is opened.
	/// </summary>
	WindowOpened = 26,

	/// <summary>
	/// The event that is raised when the conversion target has changed.
	/// </summary>
	ConversionTargetChanged = 27,

	/// <summary>
	/// The event that is raised when the text was changed in an edit control.
	/// </summary>
	TextEditTextChanged = 28,

	/// <summary>
	/// The event that is raised when the window layout has become invalidated. 
	/// This event is also used for Auto-suggest accessibility.
	/// </summary>
	LayoutInvalidated = 29,

	//TODO (DOTI): Should we add this? it is in the winui source as experimental, but not in the docs
	/// <summary>
	/// The event that is raised for general notifications (e.g. RaiseNotificationEvent).
	/// </summary>
	Notification = 30,
}
