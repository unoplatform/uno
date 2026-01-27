using System;
using CoreGraphics;
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

internal partial class AppleUIKitWindow : UIWindow
{
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
	public override void ScrollWheel(UIEvent evt)
	{
		if (evt != null && evt.Type == UIEventType.Scroll)
		{
			AppleUIKitCorePointerInputSource.Instance.ScrollWheelChanged(this, evt);
		}
		base.ScrollWheel(evt);
	}

	internal void SetOwner(CoreWindow? owner) => OwnerEvents = owner;
#endif
}
