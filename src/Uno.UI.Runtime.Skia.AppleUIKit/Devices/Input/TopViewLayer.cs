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
	public TopViewLayer()
	{
#if __IOS__
		MultipleTouchEnabled = true;
		SetupScrollGestureRecognizer();
#endif
	}

#if __IOS__
	private const string NaturalScrollingKey = "com.apple.swipescrolldirection";
	private bool _isNaturalScrollingEnabled;
	private NSObject? _userDefaultsObserver;

	private void SetupScrollGestureRecognizer()
	{
		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
		{
			var scrollGesture = new UIPanGestureRecognizer(HandleScrollGesture)
			{
				AllowedScrollTypesMask = UIScrollTypeMask.All,
				MaximumNumberOfTouches = 0,
				MinimumNumberOfTouches = 0,
				ShouldRecognizeSimultaneously = (recognizer, otherRecognizer) => true
			};
			AddGestureRecognizer(scrollGesture);

			// Initialize natural scrolling setting
			if (IsRunningOnMac())
			{
				RemoveObserver();
				UpdateNaturalScrollingValue();
				_userDefaultsObserver = NSNotificationCenter.DefaultCenter.AddObserver(
					NSUserDefaults.DidChangeNotification,
					notification => UpdateNaturalScrollingValue()
				);
			}
		}
	}

	private static bool IsRunningOnMac()
	{
		if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
		{
			return NSProcessInfo.ProcessInfo.IsiOSApplicationOnMac;
		}
		return false;
	}

	private void UpdateNaturalScrollingValue()
	{
		var defaults = NSUserDefaults.StandardUserDefaults;
		_isNaturalScrollingEnabled = defaults[NaturalScrollingKey] == null || defaults.BoolForKey(NaturalScrollingKey);
	}

	private void HandleScrollGesture(UIPanGestureRecognizer gesture)
	{
		var translation = gesture.TranslationInView(this);
		var location = gesture.LocationInView(this);
		var gestureState = gesture.State;

		AppleUIKitCorePointerInputSource.Instance.HandleScrollFromGesture(this, translation, location, gestureState, _isNaturalScrollingEnabled);

		if (gestureState == UIGestureRecognizerState.Changed)
		{
			gesture.SetTranslation(CGPoint.Empty, this);
		}
	}

	private void RemoveObserver()
	{
		if (_userDefaultsObserver != null)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(_userDefaultsObserver);
			_userDefaultsObserver = null;
		}
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
}
