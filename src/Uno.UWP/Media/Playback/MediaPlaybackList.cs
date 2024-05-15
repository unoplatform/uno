#if __ANDROID__ || __IOS__ || __MACOS__ || __SKIA__ || __WASM__
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackList : IMediaPlaybackList, IMediaPlaybackSource
	{
		public IObservableVector<MediaPlaybackItem> Items { get; } = new ObservableVector<MediaPlaybackItem>();
	}
}
#endif
