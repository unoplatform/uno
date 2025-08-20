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
}
