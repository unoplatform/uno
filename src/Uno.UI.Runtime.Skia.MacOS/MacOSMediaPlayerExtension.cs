#nullable enable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSMediaPlayerExtension : IMediaPlayerExtension
{
	private static Dictionary<MediaPlayer, MacOSMediaPlayerExtension> _instances = new();
	private MediaPlayer _player;
	internal nint _nativePlayer;
	internal MacOSMediaPlayerPresenterExtension? _presenter;
	private static readonly Dictionary<nint, WeakReference<MacOSMediaPlayerExtension>> _natives = [];
	private int _playlistIndex;
	private MediaPlaybackList? _playlist;

	private MacOSMediaPlayerExtension(object owner)
	{
		if (owner is MediaPlayer player)
		{
			_player = player;
			_nativePlayer = NativeUno.uno_mediaplayer_create();
			_natives.Add(_nativePlayer, new WeakReference<MacOSMediaPlayerExtension>(this));
		}
		else
		{
			throw new InvalidOperationException($"MacOSMediaPlayerExtension must be initialized with a MediaPlayer instance");
		}

		lock (_instances)
		{
			_instances[_player] = this;
		}
	}

	~MacOSMediaPlayerExtension()
	{
		lock (_instances)
		{
			_instances.Remove(_player!);
		}
	}

	internal static MacOSMediaPlayerExtension? GetByMediaPlayer(MediaPlayer mediaPlayer)
	{
		lock (_instances)
		{
			return _instances.TryGetValue(mediaPlayer, out var instance) ? instance : null;
		}
	}

	public static unsafe void Register()
	{
		NativeUno.uno_mediaplayer_set_callbacks(
			periodicPositionUpdate: &OnPeriodicPositionUpdate,
			onRateChanged: &OnRateChanged,
			onVideoDimensionChanged: &OnVideoDimensionChanged,
			onDurationChanged: &OnDurationChanged,
			onReadyToPlay: &OnReadyToPlay,
			onBufferingProgressChanged: &OnBufferingProgressChanged
			);

		ApiExtensibility.Register(typeof(IMediaPlayerExtension), o => new MacOSMediaPlayerExtension(o));
	}

	public IMediaPlayerEventsExtension? Events { get; set; }
	public double PlaybackRate { get; set; }
	public bool IsLoopingEnabled { get; set; }
	public bool IsLoopingAllEnabled { get; set; }

	public MediaPlayerState CurrentState { get; }

	public TimeSpan NaturalDuration => TimeSpan.Zero;

	public bool IsProtected => false;

	public double BufferingProgress => 0.0d;

	// deprecated
	public bool CanPause => true;

	// deprecated
	public bool CanSeek => true;

	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }
	public MediaPlayerAudioCategory AudioCategory { get; set; }
	public TimeSpan TimelineControllerPositionOffset { get; set; }
	public bool RealTimePlayback { get; set; }
	public double AudioBalance { get; set; }
	public TimeSpan Position { get; set; }

	public bool? IsVideo => NativeUno.uno_mediaplayer_is_video(_nativePlayer);

	public void Dispose() => NotImplemented(); // TODO
	public void Initialize() => NotImplemented(); // TODO
	public void InitializeSource()
	{
		_player.PlaybackSession.NaturalDuration = TimeSpan.Zero;
		_player.PlaybackSession.PositionFromPlayer = TimeSpan.Zero;

		_playlistIndex = -1;
		_playlist = null;

		switch (_player.Source)
		{
			case MediaPlaybackList playlist:
				_playlist = playlist;
				_playlistIndex = playlist.Items.Count > 0 ? 0 : -1;
				Uri = playlist.Items.FirstOrDefault()?.Source.Uri;
				break;
			case MediaPlaybackItem item:
				Uri = item.Source.Uri;
				break;
			case MediaSource source:
				Uri = source.Uri;
				break;
			case null:
				Uri = null;
				break;
			default:
				Uri = null;
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Unsupported media source type {_player.Source.GetType()}");
				}
				break;
		}
	}

	private Uri? _uri;

	private Uri? Uri
	{
		get => _uri;
		set
		{
			if (_uri is not null)
			{

			}

			_uri = value;
			if (_uri is null)
			{
				_player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
				return;
			}

			_player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
			NativeUno.uno_mediaplayer_set_source(_nativePlayer, _uri.ToString());
		}
	}

	public void NextTrack()
	{
		if (_playlist != null && _playlist.Items.Count > 0 && _playlistIndex + 1 < _playlist.Items.Count)
		{
			Uri = _playlist.Items[++_playlistIndex].Source.Uri;
			Play();
		}
	}

	public void OnOptionChanged(string name, object value) => NotImplemented(); // TODO

	public void OnVolumeChanged()
	{
		NativeUno.uno_mediaplayer_set_volume(_nativePlayer, (float)(_player.Volume / 100));
	}

	public void Pause()
	{
		if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
		{
			NativeUno.uno_mediaplayer_pause(_nativePlayer);
			_player.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
		}
	}

	public void Play()
	{
		if (_player.Source == null)
		{
			return;
		}

		try
		{
			if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.None)
			{
				// It's AVPlayer default behavior to clear CurrentItem when no next item exists
				// Solution to this is to reinitialize the source if video was: Ended, Failed or Manually stopped (not paused)
				// This will also reinitialize all videos in case of source list, but only in one of 3 listed scenarios above
				_player.InitializeSource();
			}

			_player.PlaybackSession.PlaybackState = MediaPlaybackState.Buffering;
			NativeUno.uno_mediaplayer_play(_nativePlayer);
			_player.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug(ex.ToString());
			}
			// OnMediaFailed(ex);
		}
	}

	public void PreviousTrack()
	{
		if (_playlist != null && _playlistIndex > 0)
		{
			Uri = _playlist.Items[--_playlistIndex].Source.Uri;
			Play();
		}
	}

	// Deprecated. Use MediaPlayer.Source instead
	public void SetUriSource(Uri uri) => throw new NotImplementedException();
	public void SetFileSource(IStorageFile file) => throw new NotImplementedException();
	public void SetStreamSource(IRandomAccessStream stream) => throw new NotImplementedException();
	public void SetMediaSource(IMediaSource source) => throw new NotImplementedException();

	public void SetSurfaceSize(Size size) => NotImplemented(); // TODO
	public void SetTransportControlsBounds(Rect bounds) => NotImplemented(); // TODO
	public void StepBackwardOneFrame() => NativeUno.uno_mediaplayer_step_by(_nativePlayer, -1);
	public void StepForwardOneFrame() => NativeUno.uno_mediaplayer_step_by(_nativePlayer, 1);
	public void Stop()
	{
		NativeUno.uno_mediaplayer_stop(_nativePlayer);
		_player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
	}

	public void ToggleMute() => NativeUno.uno_mediaplayer_toggle_mute(_nativePlayer);

	public void NotImplemented([CallerMemberName] string name = "unknown")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Member {name} is not implemented");
		}
	}

	private static MacOSMediaPlayerExtension? GetMediaPlayer(nint handle)
	{
		if (_natives.TryGetValue(handle, out var weak))
		{
			weak.TryGetTarget(out var player);
			return player;
		}

		if (typeof(MacOSMediaPlayerExtension).Log().IsEnabled(LogLevel.Error))
		{
			typeof(MacOSMediaPlayerExtension).Log().Error($"Could not map handle 0x{handle:X} to a managed MacOSMediaPlayerExtension");
		}
		return null;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]

	private static void OnPeriodicPositionUpdate(nint handle, double position)
	{
		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			var session = player._player.PlaybackSession;
			if (session.PlaybackState == MediaPlaybackState.Playing)
			{
				session.PositionFromPlayer = TimeSpan.FromSeconds(position);
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnRateChanged(nint handle, double rate)
	{
		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			var session = player._player.PlaybackSession;

			if (rate == 0.0d && session.PlaybackState == MediaPlaybackState.Playing)
			{
				// Update the status because the system changed the rate.
				session.PlaybackState = MediaPlaybackState.Paused;
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnVideoDimensionChanged(nint handle, double width, double height)
	{
		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			var presenter = player._presenter;
			if (presenter is not null)
			{
				presenter.NaturalVideoWidth = (uint)width;
				presenter.NaturalVideoHeight = (uint)height;
			}
			player.Events?.RaiseNaturalVideoDimensionChanged();
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDurationChanged(nint handle, double duration)
	{
		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			player._player.PlaybackSession.NaturalDuration = TimeSpan.FromSeconds(duration);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnReadyToPlay(nint handle, double rate)
	{
		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			var session = player._player.PlaybackSession;
			if (session.PlaybackState is MediaPlaybackState.Opening or MediaPlaybackState.Buffering)
			{
				if (rate == 0.0d)
				{
					session.PlaybackState = MediaPlaybackState.Paused;
				}
				else
				{
					session.PlaybackState = MediaPlaybackState.Playing;
					NativeUno.uno_mediaplayer_play(player._nativePlayer);
				}
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnBufferingProgressChanged(nint handle, double progress)
	{
		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			player._player.PlaybackSession.BufferingProgress = progress;
		}
	}
}
