#nullable enable

using System;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.UI.Core;

public partial class CoreWindow
{
	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyUp;

	internal event TypedEventHandler<CoreWindow, KeyEventArgs>? NativeKeyDownReceived;
	internal event TypedEventHandler<CoreWindow, KeyEventArgs>? NativeKeyUpReceived;

	internal void RaiseNativeKeyDownReceived(KeyEventArgs args)
	{
		NativeKeyDownReceived?.Invoke(this, args);
		KeyDown?.Invoke(this, args);
	}
	internal void RaiseNativeKeyUpReceived(KeyEventArgs args)
	{
		NativeKeyUpReceived?.Invoke(this, args);
		KeyUp?.Invoke(this, args);
	}
}
