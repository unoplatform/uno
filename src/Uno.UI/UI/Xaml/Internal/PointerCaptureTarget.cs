#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal class PointerCaptureTarget
{
	internal PointerCaptureTarget(UIElement element, PointerCaptureKind kind)
	{
		NativeCaptureElement = Element = element;
		Kind = kind;
	}

	/// <summary>
	/// The target element to which event args should be forwarded
	/// </summary>
	public UIElement Element { get; }

	/// <summary>
	/// The element to be used for the native capture
	/// </summary>
	/// <remarks>
	/// On WASM this might be different than the <see cref="Element"/>:
	/// In the case of implicit capture, the element used for the capture will prevent any pointer event on the sub-element
	/// (sub-element will actually get a pointer 'leave' on capture, and an 'enter' on capture release).
	/// So instead of capturing using the actual element, we use the 'OriginalSource' of the 'relatedArgs',
	/// so an event will still be sent to sub-elements and we will then filter them out if needed.
	/// </remarks>
	public UIElement NativeCaptureElement { get; set; }

	/// <summary>
	/// Gets the current capture kinds that were enabled on the target
	/// </summary>
	internal PointerCaptureKind Kind { get; set; }

	/// <summary>
	/// Determines if the <see cref="Element"/> is in the native bubbling tree.
	/// If so we could rely on standard events bubbling to reach it.
	/// Otherwise this means that we have to bubble the event in managed only.
	///
	/// This makes sense only for a platform that has "implicit capture"
	/// (i.e. all pointers events are sent to the element on which the pointer pressed
	/// occurred at the beginning of the gesture). This is the case on iOS and Android.
	/// </summary>
	public bool? IsInNativeBubblingTree { get; set; }

	/// <summary>
	/// Gets the last event dispatched by the <see cref="Element"/>.
	/// In case of native bubbling (cf. <see cref="IsInNativeBubblingTree"/>),
	/// this helps to determine that an event was already dispatched by the Owner:
	/// if a UIElement is receiving an event with the same timestamp, it means that the element
	/// is a parent of the Owner and we are only bubbling the routed event, so this element can
	/// raise the event (if the opposite, it means that the element is a child, so it has to mute the event).
	/// </summary>
	public PointerRoutedEventArgs? LastDispatched { get; set; }
}
