using System;
using System.Linq;

namespace Windows.UI.Xaml;

[Flags]
internal enum HtmlEventDispatchResult : byte
{
	/// <summary>
	/// Event has been dispatched properly, but there is no specific action to take.
	/// </summary>
	Ok = 0,

	/// <summary>
	/// Stops **native** propagation of the event to parent elements (a.k.a. Handled).
	/// </summary>
	StopPropagation = 1, // a.k.a. Handled

	/// <summary>
	/// This prevents the default native behavior of the event.
	/// For instance mouse wheel to scroll the view, tab to changed focus, etc.
	/// WARNING: Cf. remarks
	/// </summary>
	/// <remarks>
	/// The "default behavior" is applied only once the event as reached the root element.
	/// This means that if a parent element requires to prevent the default behavior, it will also prevent the default for all its children.
	/// For instance preventing the default behavior for the wheel event on a `Popup`, will also disable the mouse wheel scrolling for its content.
	/// </remarks>
	PreventDefault = 2,

	/// <summary>
	/// The event has not been dispatch.
	/// WARNING: This must not be used by application.
	/// It only indicates that there is no active listener for that event and it should not be raised anymore.
	/// It should not in anyway indicates an error in event processing.
	/// </summary>
	NotDispatched = 128
}
