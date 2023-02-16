#if UNO_HAS_MANAGED_POINTERS

namespace Windows.UI.Core;

internal interface ICoreWindowEvents
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
}
#endif
