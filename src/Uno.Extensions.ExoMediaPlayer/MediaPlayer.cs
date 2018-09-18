using System;
using Android.App;
using Com.Google.Android.Exoplayer2;
using Uno.Media.Playback;

namespace Uno.Extensions.ExoMediaPlayer
{
    public class MediaPlayer : Windows.Media.Playback.MediaPlayer
	{
		//private SimpleExoPlayer _player;

		public override IVideoSurface RenderSurface { get; } = new VideoSurface(Application.Context);

		public override void Play()
		{
			// TODO
		}

		public override void Pause()
		{
			// TODO
		}

		public override void Stop()
		{
			// TODO
		}

		// TODO
		public override TimeSpan Position { get; set; }
	}
}
