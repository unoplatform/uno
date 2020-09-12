#if __ANDROID__ || __IOS__ || __MACOS__
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
