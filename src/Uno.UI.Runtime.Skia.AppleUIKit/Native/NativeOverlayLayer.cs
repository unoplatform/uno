using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

[Register("NativeOverlayLayer")]
internal class NativeOverlayLayer : UIView
{
#if __IOS__
	public NativeOverlayLayer()
	{
		MultipleTouchEnabled = true;
	}
#endif

	public event EventHandler? SubviewsChanged;

	public override void SubviewAdded(UIView uiview)
	{
		base.SubviewAdded(uiview);
		SubviewsChanged?.Invoke(this, EventArgs.Empty);
	}

	public override void WillRemoveSubview(UIView uiview)
	{
		base.WillRemoveSubview(uiview);
		SubviewsChanged?.Invoke(this, EventArgs.Empty);
	}

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		SubviewsChanged?.Invoke(this, EventArgs.Empty);
	}

	// Note: The pointers are listen here in the NativeOverlayLayer as it's the topmost layer in the view hierarchy.
	// Note2: This must be in a layer (compared to override RootViewController.TouchesXXX methods) in order to be able to properly get the multitouch events.
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
