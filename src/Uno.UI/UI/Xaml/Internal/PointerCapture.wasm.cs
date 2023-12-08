#nullable enable

using System;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Controls;
using NativeMethods = __Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation.NativeMethods;

namespace Uno.UI.Xaml.Core;

internal partial class PointerCapture
{
	partial void CaptureNative(UIElement target, Pointer pointer)
	{
		if (PointerIdentifierPool.TryGetNative(pointer.UniqueId, out var native))
		{
			if (NativeMethods.GetBrowserName() == "Firefox" && target is TextBox textbox)
			{
				// Enable selecting the text in TextBox with pointer on FireFox because
				// Firefox is no longer able to select the text while TextBox is capturing the pointer.
				// Also capturing of TextBoxView trigger the TextBox event using bubbling. 
				WindowManagerInterop.SetPointerCapture(textbox.TextBoxView.HtmlId, native.Id);
			}
			else
			{
				WindowManagerInterop.SetPointerCapture(target.HtmlId, native.Id);
			}
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
			if (NativeMethods.GetBrowserName() == "Firefox" && target is TextBox textbox)
			{
				WindowManagerInterop.ReleasePointerCapture(textbox.TextBoxView.HtmlId, native.Id);
			}
			WindowManagerInterop.ReleasePointerCapture(target.HtmlId, native.Id);
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Cannot release pointer, could not find native pointer id for managed pointer id {pointer.UniqueId}");
		}
	}
}
