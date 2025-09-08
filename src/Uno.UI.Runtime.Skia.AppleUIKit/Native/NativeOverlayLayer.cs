using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

[Register("NativeOverlayLayer")]
internal class NativeOverlayLayer : UIView
{
	public NativeOverlayLayer()
	{
		UserInteractionEnabled = false;
	}

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

		UserInteractionEnabled = Subviews.Length > 0;

		SubviewsChanged?.Invoke(this, EventArgs.Empty);
	}

	public override UIView? HitTest(CGPoint point, UIEvent? uiEvent)
	{
		// When there are any Subviews, we want to hit test against the native clipping
		// mask, so we can let input pass-through the "holes" in the mask.
		if (!Frame.Contains(point) || Subviews.Length == 0)
		{
			return null;
		}

		if (Layer.Mask is CAShapeLayer shape)
		{
			if (shape.Path is { } path)
			{
				var pointInPath = path.ContainsPoint(point, eoFill: true);

				if (pointInPath)
				{
					return base.HitTest(point, uiEvent);
				}
			}
		}

		return null;
	}
}
