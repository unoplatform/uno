#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Helpers;

[assembly: ApiExtension(typeof(IMediaPlayerExtension), typeof(Uno.UI.Media.MediaPlayerExtension))]

namespace Uno.UI.Media;

internal partial class MediaPlayerExtension : IMediaPlayerExtension
{
	private static Dictionary<MediaPlayer, MediaPlayerExtension> _instances = new();

	private readonly MediaPlayer _owner;
	private HtmlMediaPlayer? _player;

	private bool _updatingPosition;
	private bool _isPlayRequested;
	private bool _isPlayerPrepared;
	private bool _isLoopingEnabled;
	private bool _isLoopingAllEnabled;
	private List<Uri>? _playlistItems;
	private int _playlistIndex;
	private TimeSpan _naturalDuration;
	private Uri? _uri;
	private bool _anonymousCors = FeatureConfiguration.AnonymousCorsDefault;

	const string MsAppXScheme = "ms-appx";

	public MediaPlayerExtension(object owner)
	{
		if (owner is MediaPlayer player)
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

	internal static MediaPlayerExtension? GetByMediaPlayer(MediaPlayer mediaPlayer)
	{
		lock (_instances)
		{
			return _instances.TryGetValue(mediaPlayer, out var instance) ? instance : null;
		}
	}

	internal HtmlMediaPlayer? HtmlPlayer
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

	void IMediaPlayerExtension.OnOptionChanged(string name, object value)
	{
		switch (name)
		{
			case "AnonymousCORS" when value is bool enabled:
				_anonymousCors = enabled;
				ApplyAnonymousCors();
				break;
		}
	}

	private void ApplyAnonymousCors()
		=> _player?.SetAnonymousCORS(_anonymousCors);

	public IMediaPlayerEventsExtension? Events { get; set; }

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

	public bool IsLoopingEnabled
	{
		get => _isLoopingEnabled;
		set
		{
			_isLoopingEnabled = value;
		}
	}

	public bool IsLoopingAllEnabled
	{
		get => _isLoopingAllEnabled;
		set
		{
			_isLoopingAllEnabled = value;
		}
	}

	public MediaPlayerState CurrentState { get; private set; }

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

	public bool? IsVideo { get; set; }

	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }

	public MediaPlayerAudioCategory AudioCategory { get; set; }

	public TimeSpan TimelineControllerPositionOffset
	{
		get => Position;
		set => Position = value;
	}

	public bool RealTimePlayback { get; set; }

	public double AudioBalance { get; set; }

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
						if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"Position {value.TotalSeconds}");
						}
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

	public void Dispose()
	{
		_instances.Remove(_owner);

		TryDisposePlayer();
	}

	public void Initialize()
		=> InitializePlayer();

	private void InitializePlayer()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.InitializePlayer ({_player})");
		}

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

		_player.OnStatusChanged -= OnStatusMediaChanged;
		_player.OnStatusChanged += OnStatusMediaChanged;

		_owner.PlaybackSession.PlaybackStateChanged -= OnStatusChanged;
		_owner.PlaybackSession.PlaybackStateChanged += OnStatusChanged;

		ApplyAnonymousCors();
		ApplyVideoSource();
	}

	private void OnStatusMediaChanged(object? sender, object e)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.OnStatusMediaChanged to state {_player?.PlayerState.ToString()}");
			this.Log().Debug($"MediaPlayerExtension owner PlaybackSession PlaybackState {_owner?.PlaybackSession?.PlaybackState.ToString()}");
		}

		if (_player?.PlayerState == HtmlMediaPlayerState.Paused && _owner?.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
		{
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
		}
		else if (_player?.PlayerState == HtmlMediaPlayerState.Playing && _owner?.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
		{
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
		}
	}

	private void SetPlaylistItems(MediaPlaybackList playlist)
	{
		_playlistItems = playlist.Items
			.Select(i => i.Source.Uri)
			.ToList();
	}

	public void InitializeSource()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug("Enter MediaPlayerExtension.InitializeSource().");
		}

		NaturalDuration = TimeSpan.Zero;
		if (Position != TimeSpan.Zero)
		{
			Position = TimeSpan.Zero;
		}

		// Reset player
		TryDisposePlayer();

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
				case MediaPlaybackList playlist when playlist.Items.Count > 0:
					SetPlaylistItems(playlist);
					if (_playlistItems is not null && _playlistItems.Any())
					{
						_uri = _playlistItems[0];
					}
					else
					{
						throw new InvalidOperationException("Playlist Items could not be set");
					}
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
			Events?.RaiseSourceChanged();
		}
		catch (global::System.Exception ex)
		{

			this.Log().Debug($"MediaPlayerElementExtension.InitializeSource({ex.Message})");
			OnMediaFailed(ex);
		}
	}

	public void ReInitializeSource()
	{
		NaturalDuration = TimeSpan.Zero;
		if (Position != TimeSpan.Zero)
		{
			Position = TimeSpan.Zero;
		}

		if (_owner.Source == null)
		{
			return;
		}
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
		InitializePlayer();
		ApplyVideoSource();
		Events?.RaiseSourceChanged();
	}

	private void ApplyVideoSource()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerElementExtension.SetVideoSource({_uri})");
		}

		if (_player is not null && _uri is not null)
		{
			if (!_uri.IsAbsoluteUri || _uri.Scheme == "")
			{
				_uri = new Uri(MsAppXScheme + ":///" + _uri.OriginalString.TrimStart('/'));
			}

			if (_uri.IsLocalResource())
			{
				_player.Source = AssetsPathBuilder.BuildAssetUri(_uri?.PathAndQuery);
				return;
			}

			if (_uri.IsAppData())
			{
				var filePath = AppDataUriEvaluator.ToPath(_uri);
				_player.Source = filePath;
				return;
			}

			if (_uri.IsFile)
			{
				_player.Source = _uri.OriginalString;
				return;
			}

			_player.Source = _uri.OriginalString;
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"MediaPlayerElementExtension.SetVideoSource: failed (Player is not available)");
			}
		}
	}

	public void Pause()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.Pause()");
		}

		if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
		{
			_player?.Pause();
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
		}
	}

	public void Play()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.Play()");
		}

		if (_owner.Source == null || _player == null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"MediaPlayerExtension.Play(): Failed {_owner.Source} / {_player}");
			}
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
				_player.PlaybackRate = 1;
				_player.Play();
				_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"MediaPlayerExtension.Play(): Player was not prepared");
				}
			}
		}
		catch (global::System.Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"MediaPlayerExtension.Play(): Failed {ex}");
			}
			OnMediaFailed(ex);
		}
	}

	public void SetFileSource(IStorageFile file)
		=> throw new NotSupportedException($"IStorageFile is not supported");

	public void SetMediaSource(IMediaSource source)
		=> throw new NotSupportedException($"IMediaSource is not supported");

	public void SetStreamSource(IRandomAccessStream stream)
		=> throw new NotSupportedException($"IRandomAccessStream is not supported");

	public void SetSurfaceSize(Size size)
		=> throw new NotSupportedException($"SetSurfaceSize is not supported");

	public void SetUriSource(Uri uri)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.SetUriSource({uri})");
		}

		if (_player is not null)
		{
			_player.Source = uri.OriginalString;
		}
	}

	public void StepBackwardOneFrame()
		=> throw new NotSupportedException($"StepBackwardOneFrame is not supported");

	public void StepForwardOneFrame()
		=> throw new NotSupportedException($"StepForwardOneFrame is not supported");

	public void Stop()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerExtension.Stop()");
		}

		if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
			|| _owner.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
		{
			_player?.Pause(); // Do not call stop, otherwise player will need to be prepared again
			_owner.PlaybackSession.Position = TimeSpan.Zero;
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}
	}

	public void ToggleMute()
	{
		if (_owner.IsMuted)
		{
			_player?.SetVolume(0);
		}
		else
		{
			var volume = (float)_owner.Volume / 100;
			_player?.SetVolume(volume);
		}
	}

	private void OnStatusChanged(MediaPlaybackSession? sender, object args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnStatusChanged: {args}");
		}

		if ((MediaPlaybackState)args == MediaPlaybackState.Playing)
		{
			_player?.Play();
		}
	}

	private void SetPrepared(HtmlMediaPlayer _player)
	{
		if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
		{
			if (_isPlayRequested)
			{
				_player.Play();
				_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}
			else
			{
				_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}
		_isPlayerPrepared = true;
	}

	public void OnPrepared(object? sender, object what)
	{
		if (sender is HtmlMediaPlayer mp && _player is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"OnPrepared: {_player.Duration}");
			}

			NaturalDuration = TimeSpan.FromSeconds(_player.Duration);

			IsVideo = !_player.IsAudio;

			if (!mp.IsAudio && Events is not null)
			{
				try
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"OnPrepared: {mp.VideoWidth}x{mp.VideoHeight}");
					}

					Events.RaiseNaturalVideoDimensionChanged();
				}
				catch { }
			}
			if (NaturalDuration > TimeSpan.Zero)
			{
				SetPrepared(_player);
			}
		}

		if (Events is not null)
		{
			Events?.RaiseMediaOpened();
		}
	}

	void OnError(object? sender, object what)
	{
		if (_owner.PlaybackSession.PlaybackState != MediaPlaybackState.None)
		{
			_player?.Stop();
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnError: {what}");
		}

		OnMediaFailed(message: $"MediaPlayer Error: {(string)what}");
	}


	public void OnCompletion(object? sender, object what)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnCompletion: {_owner.Position}");
		}

		Events?.RaiseMediaEnded();
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		if (IsLoopingEnabled && !IsLoopingAllEnabled)
		{
			Play();
		}
		else
		{
			// Play first item in playlist, if any and repeat all
			if (_playlistItems != null && _playlistIndex >= _playlistItems.Count - 1 && IsLoopingAllEnabled)
			{
				_playlistIndex = 0;
				_uri = _playlistItems[_playlistIndex];
				ReInitializeSource();
				Play();
			}
			else
			{
				// Play next item in playlist, if any
				if (_playlistItems != null && _playlistIndex < _playlistItems.Count - 1)
				{
					_uri = _playlistItems[++_playlistIndex];
					ReInitializeSource();
					Play();
				}
			}
		}
	}


	private void OnMediaFailed(global::System.Exception? ex = null, string? message = null)
	{
		Events?.RaiseMediaFailed(MediaPlayerError.Unknown, message ?? ex?.Message, ex);

		this.Log().Debug($"MediaPlayerElementExtension.OnMediaFailed({message})");
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
	}

	public void OnVolumeChanged()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnVolumeChanged: {_owner.Volume}");
		}

		var volume = (float)_owner.Volume / 100;
		_player?.SetVolume(volume);
	}

	private void OnTimeUpdate(object? sender, object what)
	{
		try
		{
			_updatingPosition = true;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnTimeUpdate: {Position}");
			}

			Events?.RaisePositionChanged();
		}
		finally
		{
			_updatingPosition = false;
		}
	}

	public void OnSeekComplete()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnSeekComplete: {Position}");
		}

		Events?.RaiseSeekCompleted();
	}

	private void TryDisposePlayer()
	{
		if (_player != null)
		{
			_isPlayRequested = false;
			_isPlayerPrepared = false;
		}
	}

	public void SetTransportControlsBounds(Rect bounds)
	{
		// No effect on WebAssembly.
	}

	public void PreviousTrack()
	{
		// Play prev item in playlist, if any
		if (_playlistItems != null && _playlistIndex > 0)
		{
			Pause();
			_uri = _playlistItems[--_playlistIndex];
			ReInitializeSource();
			Play();
		}
	}

	public void NextTrack()
	{
		// Play next item in playlist, if any
		if (_playlistItems != null && _playlistIndex < _playlistItems.Count - 1)
		{
			Pause();
			_uri = _playlistItems[++_playlistIndex];
			ReInitializeSource();
			Play();
		}
	}
}
