using System;
using Android.App;
using Android.Widget;
using Uno.Media.Playback;
using Windows.Media.Core;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer
	{
		private VideoView VideoViewCanvas => RenderSurface as VideoView;
		private int lastPosition = 0;

		public IVideoSurface RenderSurface { get; } = new VideoSurface(Application.Context);

		public void Play()
		{
			if (Source == null)
			{
				return;
			}

			if (CurrentState == MediaPlayerState.Paused)
			{
				//We are simply paused so just continue
				VideoViewCanvas.SeekTo(lastPosition);
				VideoViewCanvas.Start();
				CurrentState = MediaPlayerState.Playing;
				return;
			}

			try
			{
				CurrentState = MediaPlayerState.Buffering;
				VideoViewCanvas.SetVideoURI(Android.Net.Uri.Parse(((MediaSource)Source).Uri.ToString()));
			}
			catch (Exception)
			{
				//OnMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
				CurrentState = MediaPlayerState.Stopped;
			}
		}

		public void Pause()
		{
			lastPosition = VideoViewCanvas.CurrentPosition;
			VideoViewCanvas.Pause();
			CurrentState = MediaPlayerState.Paused;
		}
	}
}
