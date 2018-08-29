using System;
using UIKit;
using Uno.Media.Playback;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter : Border
	{
		private void SetVideoSurface(IVideoSurface videoSurface)
		{
			Console.WriteLine($"MEDIAPLAYERIMPL - MediaPlayerPresenter SetVideoSurface : {this} / {videoSurface}");
			this.Child = videoSurface as UIView;
			((UIView)videoSurface).Frame = this.Frame;
		}
	}
}
