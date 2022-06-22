using CoreGraphics;
using UIKit;

namespace Microsoft.UI.Xaml.Controls;

internal partial class NativeRefreshControl : UIRefreshControl
{
	public CGPoint IndicatorOffset { get; set; }
	
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();

		foreach (var view in Subviews)
		{
			var transform = !IndicatorOffset.IsEmpty ?
				CGAffineTransform.MakeTranslation(IndicatorOffset.X, IndicatorOffset.Y) :
				CGAffineTransform.MakeIdentity();
			
			view.Transform = transform;
		}
	}
}
