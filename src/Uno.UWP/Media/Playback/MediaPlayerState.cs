#if __ANDROID__ || __IOS__
namespace Windows.Media.Playback
{
	public enum MediaPlayerState 
	{
		Closed,

		Opening,

		Buffering,

		Playing,

		Paused,

		Stopped
	}
}
#endif
