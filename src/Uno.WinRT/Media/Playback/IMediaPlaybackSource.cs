using System.ComponentModel;

namespace Windows.Media.Playback
{
	[TypeConverter(typeof(MediaPlaybackSourceConverter))]
	public partial interface IMediaPlaybackSource
	{
	}
}
