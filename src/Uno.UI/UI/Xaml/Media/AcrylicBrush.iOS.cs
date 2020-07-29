using System.Collections.Generic;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
	{
		internal void CreateAcrylicBrushLayers(
			UIElement owner,
			CGRect fullArea,
			CGRect insideArea,
			CALayer layer,
			List<CALayer> sublayers,
			ref int insertionIndex,
			CAShapeLayer fillMask)
		{
			// This layer is the one we apply the mask on. It's the full size of the shape because the mask is as well.
			var acrylicContainerLayer = new CALayer
			{
				Frame = fullArea,
				Mask = fillMask,
				BackgroundColor = UIColor.Clear.CGColor,
				MasksToBounds = true,
			};

			layer.InsertSublayer(acrylicContainerLayer, insertionIndex++);
			var gradientFrame = new CGRect(new CGPoint(insideArea.X, insideArea.Y), insideArea.Size);

			var acrylicLayer = new CALayer
			{
				Frame = gradientFrame,
				MasksToBounds = true,
				Opacity = (float)TintOpacity,
				BackgroundColor = TintColor
			};

			var blurView = new UIVisualEffectView()
			{				
				ClipsToBounds = true,
				BackgroundColor = UIColor.Clear
			};
			blurView.Frame = gradientFrame;
			blurView.Effect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Dark);

			owner.InsertSubview(blurView, 0);

			acrylicContainerLayer.InsertSublayer(acrylicLayer, 0);

			sublayers.Add(acrylicContainerLayer);
		}
	}
}
