#nullable enable

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
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;
using Windows.ApplicationModel.Background;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml.Controls.Maps;
using System.Numerics;
using Uno.Logging;
using Windows.UI.Xaml;

[assembly: ApiExtension(typeof(IMediaPlayerExtension), typeof(Uno.UI.Media.MediaPlayerExtension))]

namespace Uno.UI.Media;

public partial class MediaPlayerExtension : IMediaPlayerExtension
{
	private static Dictionary<Windows.Media.Playback.MediaPlayer, MediaPlayerExtension> _instances = new();

	private Uri? _uri;
	private List<Uri>? _playlistItems;
	private readonly Windows.Media.Playback.MediaPlayer _owner;
	private GTKMediaPlayer? _player;



	private bool _updatingPosition;
	private bool _isPlayRequested;
	private bool _isPlayerPrepared;
	private int _playlistIndex;
	private TimeSpan _naturalDuration;

	public MediaPlayerExtension(object owner)
	{
		if (owner is Windows.Media.Playback.MediaPlayer player)
		{
			_owner = player;
		}
		else
		{
			throw new InvalidOperationException($"MediaPlayerPresenterExtension must be initialized with a MediaPlayer instance");
		}

		lock (_instances)
		{
			_instances[_owner] = this;
		}
	}

	~MediaPlayerExtension()
	{
		lock (_instances)
		{
			_instances.Remove(_owner);
		}
	}

	internal static MediaPlayerExtension? GetByMediaPlayer(Windows.Media.Playback.MediaPlayer mediaPlayer)
	{
		lock (_instances)
		{
			return _instances.TryGetValue(mediaPlayer, out var instance) ? instance : null;
		}
	}

	internal GTKMediaPlayer? GTKMediaPlayer
	{
		get => _player;
		set
		{
			if (value != null)
			{
				_player = value;
				InitializePlayer();
			}
		}
	}
	public IMediaPlayerEventsExtension? Events { get; set; }

	private void InitializePlayer()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"MediaPlayerExtension.InitializePlayer ({_player})");
		//}

		if (_player is null)
		{
			return;
		}

		_player.OnSourceFailed -= OnError;
		_player.OnSourceLoaded -= OnPrepared;
		_player.OnSourceEnded -= OnCompletion;
		_player.OnTimeUpdate -= OnTimeUpdate;
		_player.OnSourceFailed += OnError;
		_player.OnSourceLoaded += OnPrepared;
		_player.OnSourceEnded += OnCompletion;
		_player.OnTimeUpdate += OnTimeUpdate;

		_owner.PlaybackSession.PlaybackStateChanged -= OnStatusChanged;
		_owner.PlaybackSession.PlaybackStateChanged += OnStatusChanged;

		ApplyVideoSource();
	}
	public TimeSpan Position
	{
		get => TimeSpan.FromSeconds(_player?.CurrentPosition ?? 0);
		set
		{
			if (!_updatingPosition)
			{
				_updatingPosition = true;

				try
				{
					if (_owner.PlaybackSession.PlaybackState != MediaPlaybackState.None && _player is not null && _player.Source is not null)
					{
						_player.CurrentPosition = (int)value.TotalSeconds;
						OnSeekComplete();
					}
				}
				finally
				{
					_updatingPosition = false;
				}
			}
		}
	}

	private void ApplyVideoSource()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"MediaPlayerElementExtension.SetVideoSource({_uri})");
		//}

		if (_player is not null && _uri is not null)
		{
			_player.Source = _uri.OriginalString;
		}
		//else
		//{
		//	if (this.Log().IsEnabled(LogLevel.Debug))
		//	{
		//		this.Log().Debug($"MediaPlayerElementExtension.SetVideoSource: failed (Player is not available)");
		//	}
		//}
	}

	public void Pause()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"MediaPlayerExtension.Pause()");
		//}

		if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
		{
			_player?.Pause();
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
		}
	}

	public void InitializeSource()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().LogDebug("Enter MediaPlayerExtension.InitializeSource().");
		//}

		if (_owner.Source == null)
		{
			return;
		}

		try
		{
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
			InitializePlayer();

			switch (_owner.Source)
			{
				case MediaPlaybackList playlist when playlist.Items.Count > 0 && _playlistItems is not null:
					SetPlaylistItems(playlist);
					_uri = _playlistItems[0];
					break;

				case MediaPlaybackItem item:
					_uri = item.Source.Uri;
					break;

				case MediaSource source:
					_uri = source.Uri;
					break;

				default:
					throw new InvalidOperationException("Unsupported media source type");
			}

			ApplyVideoSource();
			Events?.RaiseMediaOpened();
			Events?.RaiseSourceChanged();
		}
		catch (global::System.Exception)
		{

			//this.Log().Debug($"MediaPlayerElementExtension.InitializeSource({ex.Message})");
			//OnMediaFailed(ex);
		}
	}
	private void SetPlaylistItems(MediaPlaybackList playlist)
	{
		_playlistItems = playlist.Items
			.Select(i => i.Source.Uri)
			.ToList();
	}
	public void Stop()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"MediaPlayerExtension.Stop()");
		//}

		if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
		{
			_player?.Stop();
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
		}
	}
	public void Play()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"MediaPlayerExtension.Play()");
		//}

		if (_owner.Source == null || _player == null)
		{
			//if (this.Log().IsEnabled(LogLevel.Debug))
			//{
			//	this.Log().Debug($"MediaPlayerExtension.Play(): Failed {_owner.Source} / {_player}");
			//}
			return;
		}

		try
		{
			// If we reached the end of media, we need to reset position to 0
			if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.None)
			{
				_owner.PlaybackSession.Position = TimeSpan.Zero;
			}

			_isPlayRequested = true;

			if (_isPlayerPrepared)
			{
				if (_player != null)
				{
					_player.PlaybackRate = 1;
					_player.Play();
				}
				_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}
			else
			{
				//if (this.Log().IsEnabled(LogLevel.Debug))
				//{
				//	this.Log().Debug($"MediaPlayerExtension.Play(): Player was not prepared");
				//}
			}
		}
		catch (global::System.Exception ex)
		{
			//if (this.Log().IsEnabled(LogLevel.Debug))
			//{
			//	this.Log().Debug($"MediaPlayerExtension.Play(): Failed {ex}");
			//}
			OnMediaFailed(ex);
		}
	}

	public void Initialize()
		=> InitializePlayer();

	private double _playbackRate;
	public double PlaybackRate
	{
		get => _playbackRate;
		set
		{
			_playbackRate = value;
			if (_player is not null)
			{
				_player.PlaybackRate = value;
			}
		}
	}

	private bool _isLoopingEnabled;
	public bool IsLoopingEnabled
	{
		get => _isLoopingEnabled;
		set
		{
			_isLoopingEnabled = value;
			if (_player is not null)
			{
				_player.SetIsLoopingEnabled(value);
			}
		}
	}


	public MediaPlayerState CurrentState => throw new NotImplementedException();

	public TimeSpan NaturalDuration
	{
		get => _naturalDuration;
		internal set
		{
			_naturalDuration = value;

			Events?.NaturalDurationChanged();
		}
	}

	public bool IsProtected
			=> false;

	public double BufferingProgress
		=> 0.0;

	public bool CanPause
		=> true;

	public bool CanSeek
		=> true;

	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }

	public MediaPlayerAudioCategory AudioCategory { get; set; }

	public TimeSpan TimelineControllerPositionOffset
	{
		get => Position;
		set => Position = value;
	}

	public bool RealTimePlayback { get; set; }

	public double AudioBalance { get; set; }

	public void SetUriSource(Uri uri)
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"MediaPlayerExtension.SetUriSource({uri})");
		//}

		if (_player is not null)
		{
			_player.Source = uri.OriginalString;
		}
	}

	public void SetFileSource(IStorageFile file) => throw new NotImplementedException();
	public void SetStreamSource(IRandomAccessStream stream) => throw new NotImplementedException();
	public void SetMediaSource(IMediaSource source) => throw new NotImplementedException();
	public void StepForwardOneFrame() => throw new NotImplementedException();
	public void StepBackwardOneFrame() => throw new NotImplementedException();
	public void SetSurfaceSize(Size size) => throw new NotImplementedException();
	public void ToggleMute()
	{
		if (_owner.IsMuted)
		{
			_player?.SetVolume(0);
		}
		else
		{
			var volume = (int)(_owner.Volume / 100);
			_player?.SetVolume(volume);
		}
	}



	public void OnOptionChanged(string name, object value) => throw new NotImplementedException();

	public void Dispose()
	{
		_instances.Remove(_owner);

		TryDisposePlayer();
	}
	private void TryDisposePlayer()
	{
		if (_player != null)
		{
			_isPlayRequested = false;
			_isPlayerPrepared = false;
		}
	}
}
