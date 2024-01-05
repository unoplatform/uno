using System;
using Windows.Media.Playback;

namespace Windows.Media.Core
{
	public partial class MediaSource : IDisposable, IMediaPlaybackSource
	{
		public Uri Uri { get; private set; } = null!;

		public static MediaSource CreateFromUri(Uri uri)
		{
			return new MediaSource()
			{
				Uri = uri
			};
		}
	}
}
