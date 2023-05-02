using System;
using Windows.Media.Playback;

namespace Windows.Media.Core
{
	public partial class MediaSource : IDisposable, IMediaPlaybackSource
	{
		public Uri Uri { get; private set; }
#if __ANDROID__ || __IOS__ ||  __MACOS__
		public void Dispose()
		{
		}
#endif
		public static MediaSource CreateFromUri(Uri uri)
		{
			return new MediaSource()
			{
				Uri = uri
			};
		}
	}
}
