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
}
#endif
