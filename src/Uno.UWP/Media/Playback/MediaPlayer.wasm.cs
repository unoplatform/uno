using System;
using Uno.Media.Playback;
using Windows.Media.Core;
using Uno.Extensions;
using System.IO;
using Uno.Foundation.Logging;
using System.Collections.Generic;
using Uno;
using Uno.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer
	{
		private bool _isPlayRequested;
		private bool _isPlayerPrepared;
		private List<Uri> _playlistItems;
		private int _playlistIndex;

		private IHtmlMediaPlayer _player;
		internal IHtmlMediaPlayer Player
		{
			get => _player;
			set
			{
				if (value != null)
				{
					_player = value;
					Initialize();
				}
			}
		}

		public void Initialize()
		{
			if (_player is null) return;
			InitializePlayer();
		}

		private void TryDisposePlayer()
		{
			if (_player != null)
			{
				_isPlayRequested = false;
				_isPlayerPrepared = false;
			}
		}

		#region Player Initialization
		private void InitializePlayer()
		{
			_player.OnSourceFailed -= OnError;
			_player.OnSourceLoaded -= OnPrepared;
			_player.OnSourceEnded -= OnCompletion;
			_player.OnTimeUpdate -= OnTimeUpdate;
			_player.OnSourceFailed += OnError;
			_player.OnSourceLoaded += OnPrepared;
			_player.OnSourceEnded += OnCompletion;
			_player.OnTimeUpdate += OnTimeUpdate;

			PlaybackSession.PlaybackStateChanged -= OnStatusChanged;
			PlaybackSession.PlaybackStateChanged += OnStatusChanged;
		}

		private void SetPlaylistItems(MediaPlaybackList playlist)
		{
			_playlistItems = playlist.Items
				.Select(i => i.Source.Uri)
				.ToList();
		}

		protected virtual void InitializeSource()
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
				MediaOpened?.Invoke(this, null);
			}
			catch (global::System.Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		private void OnTimeUpdate(object sender, object what)
		{
			PlaybackSession.PositionFromPlayer = Position;
		}

		private void SetVideoSource(Uri uri)
		{
			_player.Source = uri.ToString();
		}

		#endregion

		private void OnStatusChanged(MediaPlaybackSession sender, object args)
		{
			if ((MediaPlaybackState)args == MediaPlaybackState.Playing)
			{
				_player?.Play();
			}
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
					_player.Play();
					PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				}
			}
			catch (global::System.Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		public void OnPrepared(object sender, object what)
		{
			var mp = (IHtmlMediaPlayer)sender;
			PlaybackSession.NaturalDuration = TimeSpan.FromSeconds(_player.Duration);

			if (mp.IsVideo)
			{
				VideoRatioChanged?.Invoke(this, (double)mp.VideoWidth / global::System.Math.Max(mp.VideoHeight, 1));
			}

			if (PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
			{
				if (_isPlayRequested)
				{
					_player?.Play();
					PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				}
				else
				{
					// To display first image of media when setting a new source. Otherwise, last image of previous source remains visible
					_player?.Play();
					_player?.Stop();
				}
			}

			_isPlayerPrepared = true;
		}

		void OnError(object sender, object what)
		{
			if (PlaybackSession.PlaybackState != MediaPlaybackState.None)
			{
				_player?.Stop();
				PlaybackSession.PlaybackState = MediaPlaybackState.None;
			}

			OnMediaFailed(message: $"MediaPlayer Error: {(string)what}");
		}

		public void OnCompletion(object sender, object what)
		{
			MediaEnded?.Invoke(this, null);
			PlaybackSession.PlaybackState = MediaPlaybackState.None;

			// Play next item in playlist, if any
			if (_playlistItems != null && _playlistIndex < _playlistItems.Count - 1)
			{
				SetVideoSource(_playlistItems[++_playlistIndex]);
			}
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
				_player?.Pause();
				PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}

		public virtual void Stop()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing || PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
			{
				_player?.Pause(); // Do not call stop, otherwise player will need to be prepared again
				PlaybackSession.PlaybackState = MediaPlaybackState.None;
			}
		}

		public void OnSeekComplete()
		{
			SeekCompleted?.Invoke(this, null);
		}

		private void ToggleMute()
		{
			if (IsMuted)
			{
				_player?.SetVolume(0);
			}
			else
			{
				var volume = (float)Volume / 100;
				_player?.SetVolume(volume);
			}
		}

		private void OnVolumeChanged()
		{
			var volume = (float)Volume / 100;
			_player?.SetVolume(volume);
		}

		public virtual TimeSpan Position
		{
			get
			{
				return TimeSpan.FromSeconds(_player.CurrentPosition);
			}
			set
			{
				if (PlaybackSession.PlaybackState != MediaPlaybackState.None)
				{
					_player.CurrentPosition = (int)value.TotalSeconds;
					OnSeekComplete();
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

		public void Dispose()
		{
			TryDisposePlayer();
		}
	}
}
