#if __ANDROID__ || __IOS__
using Windows.Foundation.Collections;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackList : IMediaPlaybackList, IMediaPlaybackSource
	{
		public ObservableVector<MediaPlaybackItem> Items { get; set; } = new ObservableVector<MediaPlaybackItem>();
	}
}
#endif
