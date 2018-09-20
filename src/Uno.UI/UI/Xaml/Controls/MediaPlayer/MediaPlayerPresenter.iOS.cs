using System;
using AVFoundation;
using UIKit;
using Uno.Extensions;
using Uno.Media.Playback;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter
	{
		private void SetVideoSurface(IVideoSurface videoSurface)
		{
			Child = videoSurface as UIView;
			((UIView)videoSurface).Frame = this.Frame;
		}

		private void UpdateContentMode(Stretch stretch)
		{
			switch (stretch)
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
					throw new NotSupportedException($"Strech mode {stretch} is not supported");
			}
		}
		
		private void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
			UpdateContentMode(newValue);
		}
	}
}
