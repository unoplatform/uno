using System;
using AVFoundation;
using Uno.Extensions;
using Uno.Media.Playback;
using Windows.UI.Xaml.Media;
#if __IOS__
using _View = UIKit.UIView;
#else
using _View = AppKit.NSView;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter
	{
		private void SetVideoSurface(IVideoSurface videoSurface)
		{
			Child = videoSurface as _View;
			((_View)videoSurface).Frame = this.Frame;
		}
		
		private void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
			ApplyStretch();
		}

		internal void ApplyStretch()
		{
			if (MediaPlayer == null)
			{
				return;
			}

			switch (Stretch)
			{
				case Stretch.Uniform:
					MediaPlayer.UpdateVideoGravity(AVLayerVideoGravity.ResizeAspect);
					break;

				case Stretch.Fill:
					MediaPlayer.UpdateVideoGravity(AVLayerVideoGravity.Resize);
					break;

				case Stretch.None:
				case Stretch.UniformToFill:
					MediaPlayer.UpdateVideoGravity(AVLayerVideoGravity.ResizeAspectFill);
					break;

				default:
					throw new NotSupportedException($"Stretch mode {Stretch} is not supported");
			}
		}
	}
}
