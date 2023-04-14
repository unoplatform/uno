using System;
using System.Linq;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Core;

internal partial class PointerCapture
{
	partial void CapturePointerNative(UIElement target, Pointer pointer)
		=> WindowManagerInterop.SetPointerCapture(target.HtmlId, pointer.PointerId);

	partial void ReleasePointerNative(UIElement target, Pointer pointer)
		=> WindowManagerInterop.ReleasePointerCapture(target.HtmlId, pointer.PointerId);
}
