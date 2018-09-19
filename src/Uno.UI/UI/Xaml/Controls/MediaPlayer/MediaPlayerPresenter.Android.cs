using System;
using Android.Views;
using Uno.Media.Playback;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter
	{
		private void SetVideoSurface(IVideoSurface videoSurface)
		{
			this.Child = videoSurface as SurfaceView;
		}
	}
}
