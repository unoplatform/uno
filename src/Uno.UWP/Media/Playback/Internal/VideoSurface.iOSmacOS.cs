using System;
using AVFoundation;
using CoreGraphics;
using Foundation;
#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#else
using AppKit;
using _View = AppKit.NSView;
using CoreAnimation;
#endif

namespace Uno.Media.Playback
{
	public class VideoSurface : _View, IVideoSurface
	{
#if __MACOS__
		public VideoSurface()
		{
			var layer = new CALayer();
			this.Layer = layer;
			this.WantsLayer = true;
		}
#endif

#if __IOS__
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
#else
		public override void Layout()
		{
			base.Layout();
#endif
			if (Layer.Sublayers == null || Layer.Sublayers.Length == 0)
			{
				return;
			}

			foreach (var layer in Layer.Sublayers)
			{
				var avPlayerLayer = layer as AVPlayerLayer;
				if (avPlayerLayer != null)
				{
					avPlayerLayer.Frame = Bounds;
				}
			}
		}
	}
}
