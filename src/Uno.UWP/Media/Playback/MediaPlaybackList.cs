#if __ANDROID__ || __IOS__ || __MACOS__
using Windows.Foundation.Collections;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackList : IMediaPlaybackList, IMediaPlaybackSource
	{
		public IObservableVector<MediaPlaybackItem> Items { get; } = new ObservableVector<MediaPlaybackItem>();
	}
}
#endif
