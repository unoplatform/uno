using System;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Windows.Media.Core
{
	public partial class MediaSource : IDisposable, IMediaPlaybackSource
	{
		public Uri Uri { get; private set; }

		public IRandomAccessStream Stream { get; private set; }

		public static MediaSource CreateFromUri(Uri uri)
		{
			return new MediaSource()
			{
				Uri = uri
			};
		}
#if __ANDROID__ || __SKIA__
		public static MediaSource CreateFromStream(IRandomAccessStream stream, string _)
		{
			return new MediaSource()
			{
				Stream = stream
			};
		}

#endif
	}
}
