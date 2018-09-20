using System;
using Android.Views;
using Uno.Media.Playback;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter
	{
		private void SetVideoSurface(IVideoSurface videoSurface)
		{
			this.Child = videoSurface as SurfaceView;
		}

		private void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
		}
	}
}
