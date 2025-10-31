using System;
using Android.App;
using Android.Widget;
using Uno.Media.Playback;
using Windows.Media.Core;
using Android.Media;
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
using Uno.Foundation.Logging;
using AndroidMediaPlayer = Android.Media.MediaPlayer;
using System.Collections.Generic;
using Uno;
using Uno.Helpers;
using System.Linq;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer :
		Java.Lang.Object,
		TextureView.ISurfaceTextureListener,
		AndroidMediaPlayer.IOnCompletionListener,
		AndroidMediaPlayer.IOnErrorListener,
		AndroidMediaPlayer.IOnPreparedListener,
		AndroidMediaPlayer.IOnSeekCompleteListener,
		AndroidMediaPlayer.IOnBufferingUpdateListener,
		AndroidMediaPlayer.IOnVideoSizeChangedListener,
		View.IOnLayoutChangeListener
	{
		private readonly VideoSurface _renderSurface = new VideoSurface(Application.Context);
		private AndroidMediaPlayer _player;

		private bool _isPlayRequested;
		private bool _isPlayerPrepared;
		private VideoStretch _currentStretch = VideoStretch.Uniform;
		private bool _isUpdatingStretch;

		private IScheduledExecutorService _executorService = Executors.NewSingleThreadScheduledExecutor();
		private IScheduledFuture _scheduledFuture;
		private AudioPlayerBroadcastReceiver _noisyAudioStreamReceiver;
		private List<Uri> _playlistItems;
		private int _playlistIndex;

		const string MsAppXScheme = "ms-appx";

		internal uint NaturalVideoHeight { get; private set; }
		internal uint NaturalVideoWidth { get; private set; }

		internal IVideoSurface RenderSurface => _renderSurface;

		private void Initialize()
		{
			_renderSurface.AddOnLayoutChangeListener(this);

			// Register intent to pause media when audio become noisy (unplugged headphones, for example)
			_noisyAudioStreamReceiver = new AudioPlayerBroadcastReceiver(this);
			var intentFilter = new IntentFilter(AudioManager.ActionAudioBecomingNoisy);
			Application.Context.RegisterReceiver(_noisyAudioStreamReceiver, intentFilter);

			InitializePlayer();
		}

		#region Player Initialization

		private void TryDisposePlayer()
		{
			if (_player != null)
			{
				try
				{
					_isPlayRequested = false;
					_isPlayerPrepared = false;
					_player.Release();

					// Clear the surface view so we don't see
					// the previous video rendering.
					_renderSurface.Clear();
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

			_renderSurface.SurfaceTextureListener = this;
			if (_renderSurface.SurfaceTexture is { } texture)
			{
				OnSurfaceTextureAvailable(texture, _renderSurface.Width, _renderSurface.Height);
			}

			_player.SetOnErrorListener(this);
			_player.SetOnPreparedListener(this);
			_player.SetOnSeekCompleteListener(this);
			_player.SetOnBufferingUpdateListener(this);
			_player.SetOnCompletionListener(this);

			PlaybackSession.PlaybackStateChanged -= OnStatusChanged;
			PlaybackSession.PlaybackStateChanged += OnStatusChanged;
		}

		private void InitializeSource()
		{
			PlaybackSession.NaturalDuration = TimeSpan.Zero;
			PlaybackSession.PositionFromPlayer = TimeSpan.Zero;

			// Reset player
			TryDisposePlayer();

			if (Source == null)
			{
				return;
			}

			try
			{
				InitializePlayer();

				PlaybackSession.PlaybackState = MediaPlaybackState.Opening;

				Uri uri;
				switch (Source)
				{
					case MediaPlaybackList playlist when playlist.Items.Count > 0:
						SetPlaylistItems(playlist);
						uri = _playlistItems[0];
						break;
					case MediaPlaybackItem item:
						uri = item.Source.Uri;
						break;
					case MediaSource source:
						uri = source.Uri;
						break;
					default:
						throw new InvalidOperationException("Unsupported media source type");
				}

				SetVideoSource(uri);

				_player.PrepareAsync();
			}
			catch (global::System.Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		private void SetPlaylistItems(MediaPlaybackList playlist)
		{
			_playlistItems = playlist.Items
				.Select(i => i.Source.Uri)
				.ToList();
		}

		private void SetVideoSource(Uri uri)
		{
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart("/"));
			}

			if (uri.IsLocalResource())
			{
				var filename = uri.PathAndQuery.TrimStart('/');
				var afd = Application.Context.Assets.OpenFd(filename);
				_player.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
				return;
			}

			if (uri.IsAppData())
			{
				var filePath = AppDataUriEvaluator.ToPath(uri);
				_player.SetDataSource(filePath);
				return;
			}

			if (uri.IsFile)
			{
				_player.SetDataSource(Application.Context, Android.Net.Uri.Parse(uri.PathAndQuery));
				return;
			}

			_player.SetDataSource(uri.ToString());
		}

		#endregion

		private void OnStatusChanged(MediaPlaybackSession sender, object args)
		{
			CancelPlayingHandler();

			if ((MediaPlaybackState)args == MediaPlaybackState.Playing)
			{
				StartPlayingHandler();
			}
		}

		public void Play()
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
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			var handler = new Handler();
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

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
			if (mp is not null)
			{
				PlaybackSession.NaturalDuration = TimeSpan.FromMilliseconds(_player.Duration);
				NaturalVideoWidth = (uint)mp.VideoWidth;
				NaturalVideoHeight = (uint)mp.VideoHeight;
				NaturalVideoDimensionChanged?.Invoke(this, null);

				IsVideo = mp.GetTrackInfo()?.Any(x => x.TrackType == MediaTrackType.Video) == true;

				if (PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
				{
					UpdateVideoStretch(_currentStretch);

					if (_isPlayRequested)
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
						PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
					}
				}

				_isPlayerPrepared = true;

				MediaOpened?.Invoke(this, null);
			}
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
			PlaybackSession.PlaybackState = MediaPlaybackState.None;

			// Play next item in playlist, if any
			if (_playlistItems != null && _playlistIndex < _playlistItems.Count - 1)
			{
				_player.Reset();
				SetVideoSource(_playlistItems[++_playlistIndex]);
				_player.Prepare();
				_player.Start();
			}
		}

		private void OnMediaFailed(global::System.Exception ex = null, string message = null)
		{
			MediaFailed?.Invoke(this, new(MediaPlayerError.Unknown, message ?? ex?.Message, ex));

			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		public void Pause()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
			{
				_player?.Pause();
				PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}

		public void Stop()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing || PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
			{
				_player?.Pause(); // Do not call stop, otherwise player will need to be prepared again
				_player?.SeekTo(0);
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
				var volume = (float)Volume;
				_player?.SetVolume(volume, volume);
			}
		}

		private void OnVolumeChanged()
		{
			var volume = (float)Volume;
			_player?.SetVolume(volume, volume);
		}

		public TimeSpan Position
		{
			get
			{
				return TimeSpan.FromMilliseconds(_player?.CurrentPosition ?? 0);
			}
			set
			{
				if (PlaybackSession.PlaybackState != MediaPlaybackState.None)
				{
					_player?.SeekTo((int)value.TotalMilliseconds, MediaPlayerSeekMode.Closest);
				}
			}
		}

		public bool IsVideo { get; set; }

		internal void UpdateVideoStretch(VideoStretch stretch)
		{
			_currentStretch = stretch;

			if (_player != null
				&& RenderSurface is View surface
				&& surface.Parent is View parentView
				&& !_isUpdatingStretch)
			{
				try
				{
					_isUpdatingStretch = true;

					var width = parentView.Width;
					var height = parentView.Height;
					var parentRatio = (double)width / global::System.Math.Max(1, height);

					var videoWidth = _player.VideoWidth;
					var videoHeight = _player.VideoHeight;

					if (videoWidth == 0 || videoHeight == 0)
					{
						return;
					}

					var ratio = (double)_player.VideoWidth / global::System.Math.Max(1, _player.VideoHeight);

					switch (stretch)
					{
						case VideoStretch.Fill:
							var fillHeight = height != 0 ? height : width / ratio;
							surface.Layout(0, 0, width, (int)fillHeight);
							break;

						case VideoStretch.Uniform:
							if (parentRatio < ratio)
							{
								var uniformHeight = height - (width / ratio);
								surface.Layout(0, (int)(uniformHeight / 2), width, height - (int)(uniformHeight / 2));
							}
							else
							{
								var uniformWidth = width - (height * ratio);
								surface.Layout((int)(uniformWidth / 2), 0, width - (int)(uniformWidth / 2), height);
							}

							break;

						case VideoStretch.UniformToFill:
							if (parentRatio < ratio)
							{
								var uniformFillWidth = (height * ratio) - width;
								surface.Layout(-(int)(uniformFillWidth / 2), 0, width + (int)(uniformFillWidth / 2), height);
							}
							else
							{
								var uniformFillHeight = (width / ratio) - height;
								surface.Layout(0, -(int)(uniformFillHeight / 2), width, height + (int)(uniformFillHeight / 2));
							}

							break;

						case VideoStretch.None:
						default:
							var noneHeight = videoHeight - height;
							var nonewidth = videoWidth - width;
							surface.Layout(-nonewidth / 2, -noneHeight / 2, width + (nonewidth / 2), height + (noneHeight / 2));
							break;
					}
				}
				finally
				{
					_isUpdatingStretch = false;
				}
			}
		}

		public enum VideoStretch
		{
			Uniform,
			Fill,
			None,
			UniformToFill
		}

		#region ISurfaceTextureListener implementation

		public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			_renderSurface.Surface = new Surface(surface);
			_player?.SetSurface(_renderSurface.Surface);
			_player?.SetScreenOnWhilePlaying(true);

			UpdateVideoStretch(_currentStretch);
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			_renderSurface.Surface = null;
			_player?.SetSurface(null);
			return true;
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
		{
			UpdateVideoStretch(_currentStretch);
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface)
		{
			UpdateVideoStretch(_currentStretch);
		}

		#endregion

		#region AndroidMediaPlayer.IOnSeekCompleteListener implementation

		public void OnSeekComplete(AndroidMediaPlayer mp)
		{
			SeekCompleted?.Invoke(this, null);
		}

		#endregion

		#region AndroidMediaPlayer.IOnBufferingUpdateListener implementation

		public void OnBufferingUpdate(AndroidMediaPlayer mp, int percent)
		{
			PlaybackSession.BufferingProgress = percent;
		}

		#endregion

		#region View.IOnLayoutChangeListener

		public void OnLayoutChange(View v, int left, int top, int right, int bottom, int oldLeft, int oldTop, int oldRight, int oldBottom)
		{
			UpdateVideoStretch(_currentStretch);
		}

		#endregion

		#region AndroidMediaPlayer.IOnVideoSizeChangedListener

		public void OnVideoSizeChanged(AndroidMediaPlayer mp, int width, int height)
		{

		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			TryDisposePlayer();
			Application.Context.UnregisterReceiver(_noisyAudioStreamReceiver);
			base.Dispose(disposing);
		}

		// This is necessary for the Android-Skia target, specifically for code that
		// compiles against the reference API (which has IsLoopingAllEnabled) such as
		// MediaTransportControls.UpdateRepeatStates().
		[NotImplemented]
		public bool IsLoopingAllEnabled { get; set; }
	}
}
