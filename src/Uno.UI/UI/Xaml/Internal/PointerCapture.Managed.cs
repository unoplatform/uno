#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;
internal partial class PointerCapture
{
	partial void CaptureNative(UIElement target, Pointer pointer)
		=> target.XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.SetPointerCapture(pointer.UniqueId);

	partial void ReleaseNative(UIElement target, Pointer pointer)
		=> target.XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.ReleasePointerCapture(pointer.UniqueId);
}
#endif
