using System;
using Android.App;
using Android.Widget;
using Uno.Media.Playback;
using Windows.Media.Core;
using Android.Media;
using AndroidMediaPlayer = Android.Media.MediaPlayer;
using Android.OS;
using Java.Lang;
using Java.Util.Concurrent;
using Uno.Extensions;
using Android.Content.Res;
using System.IO;
using Android.Content;
using Android.Views;
using Android.Graphics;
using Android.Runtime;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer :
		Java.Lang.Object,
		ISurfaceHolderCallback,
		AndroidMediaPlayer.IOnCompletionListener,
		AndroidMediaPlayer.IOnErrorListener,
		AndroidMediaPlayer.IOnPreparedListener,
		AndroidMediaPlayer.IOnSeekCompleteListener
	{
		private AndroidMediaPlayer _player;

		private int _lastPosition = 0;
		private bool _isPlayRequested = false;
		private bool _isPlayerPrepared = false;
		private bool _hasValidHolder = false;
		private IScheduledExecutorService _executorService = Executors.NewSingleThreadScheduledExecutor();
		private IScheduledFuture _scheduledFuture;

		const string MsAppXScheme = "ms-appx";
		const string MsAppDataScheme = "ms-appdata";

		public virtual IVideoSurface RenderSurface { get; } = new VideoSurface(Application.Context);

		private void TryDisposePlayer()
		{
			if (_player != null)
			{
				try
				{
					_isPlayRequested = false;
					_isPlayerPrepared = false;
					_player.Release();

					var surfaceView = RenderSurface as SurfaceView;
					var surfaceHolder = surfaceView.Holder;
					surfaceHolder.RemoveCallback(this);
				}
				finally
				{
					_player?.Dispose();
					_player = null;
				}
			}
		}

		private void InitializePlayer()
		{
			_player = new AndroidMediaPlayer();
			var surfaceView = RenderSurface as SurfaceView;
			var surfaceHolder = surfaceView.Holder;

			if (_hasValidHolder)
			{
				_player.SetDisplay(surfaceHolder);
				_player.SetScreenOnWhilePlaying(true);
			}
			else
			{
				surfaceHolder.AddCallback(this);
			}

			_player.SetOnErrorListener(this);
			_player.SetOnPreparedListener(this);
			_player.SetOnSeekCompleteListener(this);

			PlaybackSession.PlaybackStateChanged -= OnStatusChanged;
			PlaybackSession.PlaybackStateChanged += OnStatusChanged;
		}

		private void OnStatusChanged(MediaPlaybackSession sender, object args)
		{
			CancelPlayingHandler();

			if ((MediaPlaybackState)args == MediaPlaybackState.Playing)
			{
				StartPlayingHandler();
			}
		}

		protected virtual void InitializeSource()
		{
			PlaybackSession.NaturalDuration = TimeSpan.Zero;
			PlaybackSession.PositionFromPlayer = TimeSpan.Zero;
			_lastPosition = 0;

			if (Source == null)
			{
				return;
			}

			try
			{
				// Reset player
				TryDisposePlayer();
				InitializePlayer();
				
				PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
				SetVideoSource(((MediaSource)Source).Uri);

				_player.PrepareAsync();

				MediaOpened?.Invoke(this, null);
			}
			catch (global::System.Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		private void SetVideoSource(Uri uri)
		{
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart("/"));
			}

			var isResource = uri.Scheme.Equals(MsAppXScheme, StringComparison.OrdinalIgnoreCase)
							|| uri.Scheme.Equals(MsAppDataScheme, StringComparison.OrdinalIgnoreCase);

			if (isResource)
			{
				var filename = global::System.IO.Path.GetFileName(uri.LocalPath);
				var afd = Application.Context.Assets.OpenFd(filename);
				_player.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
				return;
			}

			if (uri.IsFile)
			{
				_player.SetDataSource(Application.Context, Android.Net.Uri.Parse(uri.PathAndQuery));
				return;
			}

			_player.SetDataSource(Application.Context, Android.Net.Uri.Parse(uri.ToString()));
		}

		public virtual void Play()
		{
			if (Source == null || _player == null)
			{
				return;
			}

			try
			{
				// If we reached the end of media, we need to reset position to 0
				if (PlaybackSession.PlaybackState == MediaPlaybackState.None)
				{
					PlaybackSession.Position = TimeSpan.Zero;
				}

				_isPlayRequested = true;

				if (_isPlayerPrepared)
				{
					_player.SeekTo(_lastPosition);
					_player.Start();
					PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				}
			}
			catch (global::System.Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		private void CancelPlayingHandler()
		{
			_scheduledFuture?.Cancel(false);
		}

		private void StartPlayingHandler()
		{
			var handler = new Handler();
			var runnable = new Runnable(() => { handler.Post(OnPlaying); });
			if (!_executorService.IsShutdown)
			{
				_scheduledFuture = _executorService.ScheduleAtFixedRate(runnable, 100, 1000, TimeUnit.Milliseconds);
			}
		}

		private void OnPlaying()
		{
			PlaybackSession.PositionFromPlayer = Position;
		}

		public void OnPrepared(AndroidMediaPlayer mp)
		{
			PlaybackSession.NaturalDuration = TimeSpan.FromMilliseconds(_player.Duration);

			if (_isPlayRequested && PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
			{
				_player.Start();
				PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}
			else
			{
				// To display first image of media when setting a new source. Otherwise, last image of previous source remains visible
				_player.Start();
				_player.Pause();
				_player.SeekTo(0);
			}

			_isPlayerPrepared = true;
		}
		
		public bool OnError(AndroidMediaPlayer mp, MediaError what, int extra)
		{
			if (PlaybackSession.PlaybackState != MediaPlaybackState.None)
			{
				_player?.Stop();
				PlaybackSession.PlaybackState = MediaPlaybackState.None;
			}

			OnMediaFailed(message: $"MediaPlayer Error: {what}");
			return true;
		}

		public void OnCompletion(AndroidMediaPlayer mp)
		{
			MediaEnded?.Invoke(this, null);
		}

		private void OnMediaFailed(global::System.Exception ex = null, string message = null)
		{
			MediaFailed?.Invoke(this, new MediaPlayerFailedEventArgs()
			{
				Error = MediaPlayerError.Unknown,
				ExtendedErrorCode = ex,
				ErrorMessage = message ?? ex?.Message
			});

			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		public virtual void Pause()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
			{
				_lastPosition = _player.CurrentPosition;
				_player?.Pause();
				PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}

		public virtual void Stop()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing || PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
			{
				_player?.Pause(); // Do not call stop, otherwise player will need to be prepared again
				_player?.SeekTo(0);
				_lastPosition = 0;
				PlaybackSession.PlaybackState = MediaPlaybackState.None;
			}
		}

		private void ToggleMute()
		{
			if (IsMuted)
			{
				_player?.SetVolume(0, 0);
			}
			else
			{
				var volume = (float)Volume / 100;
				_player?.SetVolume(volume, volume);
			}
		}

		private void OnVolumeChanged()
		{
			var volume = (float)Volume / 100;
			_player?.SetVolume(volume, volume);
		}

		#region ISurfaceHolderCallback implementation

		public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
		{
		}

		public void SurfaceCreated(ISurfaceHolder holder)
		{
			_player.SetDisplay(holder);
			_player.SetScreenOnWhilePlaying(true);
			_hasValidHolder = true;
		}

		public void SurfaceDestroyed(ISurfaceHolder holder)
		{
			_hasValidHolder = false;
		}

		#endregion

		#region AndroidMediaPlayer.IOnSeekCompleteListener implementation

		public void OnSeekComplete(AndroidMediaPlayer mp)
		{
			SeekCompleted?.Invoke(this, null);
		}

		#endregion

		public virtual TimeSpan Position
		{
			get
			{
				return TimeSpan.FromMilliseconds(_player.CurrentPosition);
			}
			set
			{
				if (PlaybackSession.PlaybackState != MediaPlaybackState.None)
				{
					_player?.SeekTo((int)value.TotalMilliseconds);
				}
			}
		}
	}
}
