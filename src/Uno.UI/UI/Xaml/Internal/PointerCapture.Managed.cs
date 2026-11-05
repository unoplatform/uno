#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal partial class PointerCapture
{
	// MUX Reference PointerInputProcessor.cpp: an element inside a windowed popup captures through the popup's window.
	partial void CaptureNative(UIElement target, Pointer pointer)
	{
		if (Microsoft.UI.Xaml.Controls.Primitives.PopupRoot.GetOpenPopupForElement(target) is { IsWindowed: true, WindowedPopupSite: not null } popup)
		{
			popup.SetPointerCapture();
		}
		else
		{
			target.XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.SetPointerCapture(pointer.UniqueId);
		}
	}

	partial void ReleaseNative(UIElement target, Pointer pointer)
	{
		if (Microsoft.UI.Xaml.Controls.Primitives.PopupRoot.GetOpenPopupForElement(target) is { } popup && popup.WindowHasPointerCapture())
		{
			popup.ReleasePointerCapture();
		}
		else
		{
			target.XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.ReleasePointerCapture(pointer.UniqueId);
		}
	}
}
#endif
