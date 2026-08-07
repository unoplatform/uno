namespace Windows.Media.Playback;

public partial class MediaPlayerRateChangedEventArgs
{
	internal MediaPlayerRateChangedEventArgs(double newRate)
	{
		NewRate = newRate;
	}

	public double NewRate
	{
		get;
	}
}
