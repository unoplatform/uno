using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using AppKit;
using CoreAnimation;
using CoreGraphics;

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
				BackgroundColor = NSColor.Clear.CGColor,
				MasksToBounds = true,
			};

			layer.InsertSublayer(acrylicContainerLayer, insertionIndex++);
			var gradientFrame = new CGRect(new CGPoint(insideArea.X, insideArea.Y), insideArea.Size);

			var blurView = new NSVisualEffectView()
			{
				BlendingMode = BackgroundSource == AcrylicBackgroundSource.HostBackdrop ?
					NSVisualEffectBlendingMode.BehindWindow : NSVisualEffectBlendingMode.WithinWindow,
				Material = NSVisualEffectMaterial.Dark,
				State = NSVisualEffectState.Active
			};
			blurView.Frame = gradientFrame;

			owner.AddSubview(blurView, NSWindowOrderingMode.Below,null);

			var tintView = new NSView()
			{
				WantsLayer = true,
				Frame = gradientFrame
			};
			tintView.Layer.BackgroundColor = TintColor;
			tintView.Layer.Opacity = (float)TintOpacity;

			owner.AddSubview(tintView, NSWindowOrderingMode.Above, blurView);

			sublayers.Add(acrylicContainerLayer);
		}
	}
}
