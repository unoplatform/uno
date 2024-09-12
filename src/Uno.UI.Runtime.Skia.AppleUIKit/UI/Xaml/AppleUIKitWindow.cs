using System;
using Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using UIKit;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class AppleUIKitWindow : UIWindow
{
#if __MACCATALYST__
	internal ICoreWindowEvents? OwnerEvents { get; private set; }
#endif

	public override void TouchesBegan(NSSet touches, UIEvent? evt) =>
		AppleUIKitCorePointerInputSource.Instance.TouchesBegan(this, touches, evt);

	public override void TouchesMoved(NSSet touches, UIEvent? evt) =>
		AppleUIKitCorePointerInputSource.Instance.TouchesMoved(this, touches, evt);

	public override void TouchesEnded(NSSet touches, UIEvent? evt) =>
		AppleUIKitCorePointerInputSource.Instance.TouchesEnded(this, touches, evt);

	public override void TouchesCancelled(NSSet touches, UIEvent? evt) =>
		AppleUIKitCorePointerInputSource.Instance.TouchesCancelled(this, touches, evt);

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
}
