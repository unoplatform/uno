using System;
using AVFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Uno.Media.Playback
{
	public class VideoSurface : UIView, IVideoSurface
	{
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

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
