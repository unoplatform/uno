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
		private int _lastPosition = 0;
		private bool _isPlayerReady = false;
		private bool _isPlayRequested = false;
		private bool _isPlayerPrepared = false;

		public virtual IVideoSurface RenderSurface { get; } = new VideoSurface(Application.Context);
		
		private void Init()
		{
			VideoViewCanvas.SetOnErrorListener(this);
			VideoViewCanvas.SetOnPreparedListener(this);
		}

		protected virtual void InitializeSource()
		{
			PlaybackSession.NaturalDuration = TimeSpan.Zero;
			PlaybackSession.Position = TimeSpan.Zero;
			_lastPosition = 0;

			if (Source == null)
			{
				return;
			}

			try
			{
				if (_isPlayerReady == false)
				{
					Init();
					_isPlayerReady = true;
				}
				
				PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
				VideoViewCanvas.SetVideoURI(Android.Net.Uri.Parse(((MediaSource)Source).Uri.ToString()));

				MediaOpened?.Invoke(this, null);
			}
			catch (Exception)
			{
				OnMediaFailed();
			}
		}

		public virtual void Play()
		{
			if (Source == null || !_isPlayerReady)
			{
				return;
			}

			try
			{
				_isPlayRequested = true;

				if (_isPlayerPrepared)
				{
					VideoViewCanvas.SeekTo(_lastPosition);
					VideoViewCanvas.Start();
					PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				}
			}
			catch (Exception)
			{
				OnMediaFailed();
			}
		}
		
		public void OnPrepared(AndroidMediaPlayer mp)
		{
			PlaybackSession.NaturalDuration = TimeSpan.FromSeconds(VideoViewCanvas.Duration);

			if (_isPlayRequested && PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
			{
				VideoViewCanvas.Start();
				PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}

			_isPlayerPrepared = true;
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

		public virtual void Pause()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
			{
				_lastPosition = VideoViewCanvas.CurrentPosition;
				VideoViewCanvas.Pause();
				PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}

		public virtual void Stop()
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

		public virtual TimeSpan Position { get; set; }
	}
}
