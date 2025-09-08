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
[Register("TopViewLayer")]
internal class TopViewLayer : UIView
{
#if __IOS__
	public TopViewLayer()
	{
		MultipleTouchEnabled = true;
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
