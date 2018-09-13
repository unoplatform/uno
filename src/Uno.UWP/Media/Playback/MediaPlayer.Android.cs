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
		AndroidMediaPlayer.IOnCompletionListener,
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

			if (PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
			{
				VideoViewCanvas.SeekTo(lastPosition);
				VideoViewCanvas.Start();
				PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				return;
			}

			try
			{
				lastPosition = 0;
				PlaybackSession.PlaybackState = MediaPlaybackState.Buffering;
				VideoViewCanvas.SetVideoURI(Android.Net.Uri.Parse(((MediaSource)Source).Uri.ToString()));
			}
			catch (Exception)
			{
				OnMediaFailed();
			}
		}
		
		public void OnPrepared(AndroidMediaPlayer mp)
		{
			if (CurrentState == MediaPlayerState.Buffering)
			{
				VideoViewCanvas.Start();
			}

			PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
		}

		public bool OnError(AndroidMediaPlayer mp, MediaError what, int extra)
		{
			VideoViewCanvas?.StopPlayback();
			PlaybackSession.PlaybackState = MediaPlaybackState.None;
			OnMediaFailed();
			return true;
		}

		public void OnCompletion(AndroidMediaPlayer mp)
		{
			MediaEnded?.Invoke(this, null);
		}

		private void OnMediaFailed()
		{
			MediaFailed?.Invoke(this, new MediaPlayerFailedEventArgs());
			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		public void Pause()
		{
			if (CurrentState == MediaPlayerState.Playing)
			{
				lastPosition = VideoViewCanvas.CurrentPosition;
				VideoViewCanvas.Pause();
				PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}

		internal void Stop()
		{
			VideoViewCanvas.StopPlayback();
			PlaybackSession.PlaybackState = MediaPlaybackState.None;
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
