#if __ANDROID__ || __IOS__ || __MACOS__
using Windows.Media.Core;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackItem : IMediaPlaybackSource
	{
		public MediaSource Source { get; }

		public MediaPlaybackItem(MediaSource source)
		{
			Source = source;
		}
	}
}
#endif
