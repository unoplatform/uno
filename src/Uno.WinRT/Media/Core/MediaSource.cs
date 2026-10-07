using System;
using Windows.Media.Playback;

namespace Windows.Media.Core
{
	public partial class MediaSource : IDisposable, IMediaPlaybackSource
	{
		internal MediaSource()
		{
		}

		public Uri Uri { get; private set; }

		public static MediaSource CreateFromUri(Uri uri)
		{
			return new MediaSource()
			{
				Uri = uri
			};
		}
	}
}
