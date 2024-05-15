using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Core;


#if !UNO_HAS_MANAGED_POINTERS
[global::Uno.NotImplemented]
#endif
public partial interface ICorePointerInputSource
{
	event TypedEventHandler<object, PointerEventArgs> PointerCaptureLost;
	event TypedEventHandler<object, PointerEventArgs> PointerEntered;
	event TypedEventHandler<object, PointerEventArgs> PointerExited;
	event TypedEventHandler<object, PointerEventArgs> PointerMoved;
	event TypedEventHandler<object, PointerEventArgs> PointerPressed;
	event TypedEventHandler<object, PointerEventArgs> PointerReleased;
	event TypedEventHandler<object, PointerEventArgs> PointerWheelChanged;

	bool HasCapture { get; }

	/// <remarks>
	/// Setting to null should hide the cursor. This only works on some platforms. Other platforms return to the
	/// default shape if set to null.
	/// </remarks>>
	CoreCursor PointerCursor { get; set; }

	Point PointerPosition { get; }

	void ReleasePointerCapture();

	void SetPointerCapture();
}

internal interface IUnoCorePointerInputSource : ICorePointerInputSource
{
	// Uno only
	internal event TypedEventHandler<object, PointerEventArgs> PointerCancelled;

	internal void SetPointerCapture(PointerIdentifier pointer);

	internal void ReleasePointerCapture(PointerIdentifier pointer);
}
