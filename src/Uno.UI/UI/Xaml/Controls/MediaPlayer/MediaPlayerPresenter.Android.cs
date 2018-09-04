using System;
using Android.Widget;
using Uno.Media.Playback;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter : Border
	{
		private void SetVideoSurface(IVideoSurface videoSurface)
		{
			this.Child = videoSurface as VideoView;
		}
	}
}
