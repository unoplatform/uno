#if !HAS_UNO_WINUI && !__ANDROID__ && !__IOS__ && !__MACOS__ && !__WASM__
using System;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Xaml.Media;
using Uno.Media.Playback;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter : Border
	{
		private void SetVideoSurface(IVideoSurface videoSurface)
		{

		}

		private void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
		}

		internal void ApplyStretch()
		{
		}
	}
}
#endif
