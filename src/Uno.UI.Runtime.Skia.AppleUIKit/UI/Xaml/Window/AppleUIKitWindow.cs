using System;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using ObjCRuntime;
using UIKit;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal partial class AppleUIKitWindow : UIWindow
{
#if __MACCATALYST__
	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	private static extern double double_objc_msgSend(IntPtr receiver, IntPtr selector);

	private static readonly Selector _scrollDeltaXSelector = new Selector("scrollingDeltaX");
	private static readonly Selector _scrollDeltaYSelector = new Selector("scrollingDeltaY");
#endif
	internal event Action? FrameChanged;

	public override CGRect Frame
	{
		get => base.Frame;
		set
		{
			var frameChanged = base.Frame != value;

			base.Frame = value;

			if (frameChanged)
			{
				FrameChanged?.Invoke();
			}
		}
	}

#if __MACCATALYST__
	internal ICoreWindowEvents? OwnerEvents { get; private set; }
#endif

	public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
	{
		if (!UnoKeyboardInputSource.Instance.TryHandlePresses(presses, evt, this))
		{
			base.PressesEnded(presses, evt);
		}
	}

#if __MACCATALYST__
	internal void SetOwner(CoreWindow? owner) => OwnerEvents = owner;
#endif

#if __MACCATALYST__
	public override void SendEvent(UIEvent evt)
	{
		base.SendEvent(evt);
		if (evt.Type == UIEventType.Scroll)
		{
			var deltaX = double_objc_msgSend(evt.Handle, _scrollDeltaXSelector.Handle);
			var deltaY = double_objc_msgSend(evt.Handle, _scrollDeltaYSelector.Handle);
			var location = evt.LocationInWindow(this);
			AppleUIKitCorePointerInputSource.Instance.HandleScrollEvent(deltaX, deltaY, location);
		}
	}
#endif
}
