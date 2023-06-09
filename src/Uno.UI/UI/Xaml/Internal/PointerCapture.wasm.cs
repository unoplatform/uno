#nullable enable

using System;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Core;

internal partial class PointerCapture
{
	partial void CaptureNative(UIElement target, Pointer pointer)
	{
		if (PointerIdentifierPool.TryGetNative(pointer.UniqueId, out var native))
		{
			WindowManagerInterop.SetPointerCapture(target.HtmlId, native.Id);
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Cannot capture pointer, could not find native pointer id for managed pointer id {pointer.UniqueId}");
		}
	}

	partial void ReleaseNative(UIElement target, Pointer pointer)
	{
		if (PointerIdentifierPool.TryGetNative(pointer.UniqueId, out var native))
		{
			WindowManagerInterop.ReleasePointerCapture(target.HtmlId, native.Id);
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Cannot release pointer, could not find native pointer id for managed pointer id {pointer.UniqueId}");
		}
	}
}
