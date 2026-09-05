namespace Windows.Media.Playback;

public partial class PlaybackMediaMarkerReachedEventArgs
{
	internal PlaybackMediaMarkerReachedEventArgs(PlaybackMediaMarker playbackMediaMarker)
	{
		PlaybackMediaMarker = playbackMediaMarker;
	}

	public global::Windows.Media.Playback.PlaybackMediaMarker PlaybackMediaMarker
	{
		get;
	}
}
