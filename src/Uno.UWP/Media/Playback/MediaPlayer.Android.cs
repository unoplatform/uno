using System;
using Android.App;
using Android.Widget;
using Uno.Media.Playback;
using Windows.Media.Core;
using Android.Media;
using AndroidMediaPlayer = Android.Media.MediaPlayer;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer :
		Java.Lang.Object,
		AndroidMediaPlayer.IOnErrorListener,
		AndroidMediaPlayer.IOnPreparedListener
	{
		private VideoView VideoViewCanvas => RenderSurface as VideoView;
		private int lastPosition = 0;
		private bool isPlayerReady = false;

		public IVideoSurface RenderSurface { get; } = new VideoSurface(Application.Context);
		
		private void Init()
		{
			VideoViewCanvas.SetOnErrorListener(this);
			VideoViewCanvas.SetOnPreparedListener(this);
		}

		public void Play()
		{
			if (Source == null)
			{
				return;
			}

			if (isPlayerReady == false)
			{
				Init();
				isPlayerReady = true;
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
				lastPosition = 0;
				CurrentState = MediaPlayerState.Buffering;
				VideoViewCanvas.SetVideoURI(Android.Net.Uri.Parse(((MediaSource)Source).Uri.ToString()));
			}
			catch (Exception)
			{
				//OnMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
				CurrentState = MediaPlayerState.Stopped;
			}
		}
		
		public void OnPrepared(AndroidMediaPlayer mp)
		{
			if (CurrentState == MediaPlayerState.Buffering)
			{
				VideoViewCanvas.Start();
			}

			CurrentState = MediaPlayerState.Playing;
		}

		public bool OnError(AndroidMediaPlayer mp, MediaError what, int extra)
		{
			VideoViewCanvas?.StopPlayback();
			CurrentState = MediaPlayerState.Stopped;
			Console.WriteLine($"MEDIAPLAYERIMPL - Play - Exception {what.ToString()}");
			//OnMediaFailed(new MediaFailedEventArgs(what.ToString(), new System.Exception()));
			return true;
		}

		public void Pause()
		{
			if (CurrentState == MediaPlayerState.Playing)
			{
				lastPosition = VideoViewCanvas.CurrentPosition;
				VideoViewCanvas.Pause();
				CurrentState = MediaPlayerState.Paused;
			}
		}

		private void ToggleMute()
		{
			// TODO
		}

		private void OnVolumeChanged()
		{
			// TODO
		}
	}
}
