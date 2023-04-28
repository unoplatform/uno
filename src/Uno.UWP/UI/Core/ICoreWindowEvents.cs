#if UNO_HAS_MANAGED_POINTERS
using System;
using System.Linq;

namespace Windows.UI.Core;

public interface ICoreWindowEvents
{
	void RaisePointerEntered(PointerEventArgs args);
	void RaisePointerExited(PointerEventArgs args);
	void RaisePointerMoved(PointerEventArgs args);
	void RaisePointerPressed(PointerEventArgs args);
	void RaisePointerReleased(PointerEventArgs args);
	void RaisePointerWheelChanged(PointerEventArgs args);
	void RaisePointerCancelled(PointerEventArgs args);

	void RaiseKeyUp(KeyEventArgs args);
	void RaiseKeyDown(KeyEventArgs args);
#if __SKIA__
	// Due to the way keyboard events are handled in Skia,
	// the CoreWindow is responsible for initiating the event
	// bubbling flow. However, a RaiseKeyUp/Down event should
	// be fired on CoreWindow itself AFTER the bubbling flow
	// is done. So, we add another events as a workaround.

	internal void RaiseNativeKeyDownReceived(KeyEventArgs args);
	internal void RaiseNativeKeyUpReceived(KeyEventArgs args);
#endif
}
#endif
