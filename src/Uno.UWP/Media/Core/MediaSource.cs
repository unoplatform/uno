#if __ANDROID__ || __IOS__ || __MACOS__
using System;
using Windows.Media.Playback;

namespace Windows.Media.Core
{
	public partial class MediaSource : IDisposable, IMediaPlaybackSource
	{
		public Uri Uri { get; private set; }
		
		public  void Dispose()
		{
		}
		
		public static MediaSource CreateFromUri(Uri uri)
		{
			return new MediaSource()
			{
				Uri = uri
			};
		}
	}
}
#endif
