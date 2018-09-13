#if __ANDROID__ || __IOS__
namespace Windows.Media.Playback
{
	public enum MediaPlayerError 
	{
		Unknown,

		Aborted,

		NetworkError,

		DecodingError,

		SourceNotSupported,
	}
}
#endif
