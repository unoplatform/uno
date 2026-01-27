using System;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

/// <summary>
/// Top layer that receives all touch events and forwards them to the
/// managed pointer handling. It also contains  <see cref="NativeOverlayLayer" /> which
/// will only capture input of the native element within.
/// Input handling must be in a layer (compared to override RootViewController.TouchesXXX methods)
/// in order to be able to properly get the multitouch events.
/// </summary>
internal partial class TopViewLayer : UIView
{
#if __IOS__
	public TopViewLayer()
	{
		MultipleTouchEnabled = true;
		SetupScrollGestureRecognizer();
	}

	private void SetupScrollGestureRecognizer()
	{
		// For iOS, we need to use gesture recognizers to handle mouse/trackpad scrolling
		// since ScrollWheel method is only available on Mac Catalyst
		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
		{
			var scrollGesture = new UIPanGestureRecognizer(HandleScrollGesture)
			{
				AllowedScrollTypesMask = UIScrollTypeMask.All,
				MaximumNumberOfTouches = 0 // Only mouse/trackpad
			};
			AddGestureRecognizer(scrollGesture);
		}
	}

	private void HandleScrollGesture(UIPanGestureRecognizer gesture)
	{
		// Convert pan gesture to scroll wheel event for iOS
		var translation = gesture.TranslationInView(this);
		var location = gesture.LocationInView(this);

		if (Math.Abs(translation.X) < 0.1 && Math.Abs(translation.Y) < 0.1)
		{
			return;
		}

		AppleUIKitCorePointerInputSource.Instance.HandleScrollFromGesture(this, translation, location);

		gesture.SetTranslation(CGPoint.Empty, this);
	}
#endif

	public override void TouchesBegan(NSSet touches, UIEvent? evt)
	{
		AppleUIKitCorePointerInputSource.Instance.TouchesBegan(this, touches, evt);
		base.TouchesBegan(touches, evt);
	}

	public override void TouchesMoved(NSSet touches, UIEvent? evt)
	{
		AppleUIKitCorePointerInputSource.Instance.TouchesMoved(this, touches, evt);
		base.TouchesMoved(touches, evt);
	}

	public override void TouchesEnded(NSSet touches, UIEvent? evt)
	{
		AppleUIKitCorePointerInputSource.Instance.TouchesEnded(this, touches, evt);
		base.TouchesEnded(touches, evt);
	}

	public override void TouchesCancelled(NSSet touches, UIEvent? evt)
	{
		AppleUIKitCorePointerInputSource.Instance.TouchesCancelled(this, touches, evt);
		base.TouchesCancelled(touches, evt);
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
#endif
}
