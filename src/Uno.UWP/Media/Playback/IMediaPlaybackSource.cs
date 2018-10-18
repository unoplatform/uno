#if __IOS__ || __ANDROID__
using System.ComponentModel;

namespace Windows.Media.Playback
{
	[TypeConverter(typeof(MediaPlaybackSourceConverter))]
	public partial interface IMediaPlaybackSource 
	{
	}
}
#endif
