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

using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.Media.Playback;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSMediaPlayerExtension : IMediaPlayerExtension
{
	private const string MsAppXScheme = "ms-appx";
	private static readonly ConditionalWeakTable<MediaPlayer, MacOSMediaPlayerExtension> _instances = new();
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
			// `_nativePlayer` is now in `_natives` and ready to dispatch notifications
			NativeUno.uno_mediaplayer_set_notifications(_nativePlayer);
		}
		else
		{
			throw new InvalidOperationException($"MacOSMediaPlayerExtension must be initialized with a MediaPlayer instance");
		}

		lock (_instances)
		{
			_instances.TryAdd(_player, this);
		}
	}

	~MacOSMediaPlayerExtension()
	{
		Dispose(false);
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
			onBufferingProgressChanged: &OnBufferingProgressChanged,
			onMediaOpened: &OnMediaOpened,
			onMediaEnded: &OnMediaEnded,
			onMediaFailed: &OnMediaFailed,
			onMediaStalled: &OnMediaStalled
			);

		ApiExtensibility.Register(typeof(IMediaPlayerExtension), o => new MacOSMediaPlayerExtension(o));
	}

	public IMediaPlayerEventsExtension? Events { get; set; }

	public double PlaybackRate
	{
		get => NativeUno.uno_mediaplayer_get_rate(_nativePlayer);
		set => NativeUno.uno_mediaplayer_set_rate(_nativePlayer, (float)value);
	}

	public bool IsLoopingEnabled { get; set; }
	public bool IsLoopingAllEnabled { get; set; }

	// Deprecated. Use PlaybackSession.PlaybackState
	// ref: https://learn.microsoft.com/en-us/uwp/api/windows.media.playback.mediaplayer.currentstate?view=winrt-26100
	public MediaPlayerState CurrentState { get; }

	public TimeSpan NaturalDuration { get; private set; }

	public bool IsProtected => false;

	public double BufferingProgress => 0.0d;

	// Deprecated. Use PlaybackSession.CanPause
	// ref: https://learn.microsoft.com/en-us/uwp/api/windows.media.playback.mediaplayer.canpause?view=winrt-26100
	// Always true, no mapping to AVPlayer
	public bool CanPause => true;

	// Deprecated. Use PlaybackSession.CanSeek
	// ref: https://learn.microsoft.com/en-us/uwp/api/windows.media.playback.mediaplayer.canseek?view=winrt-26100
	// Always true, no mapping to AVPlayer
	public bool CanSeek => true;

	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }

	public MediaPlayerAudioCategory AudioCategory { get; set; }

	public TimeSpan TimelineControllerPositionOffset { get; set; }

	public bool RealTimePlayback { get; set; }

	public double AudioBalance { get; set; }

	public TimeSpan Position
	{
		get { return TimeSpan.FromSeconds(NativeUno.uno_mediaplayer_get_current_time(_nativePlayer)); }
		set { NativeUno.uno_mediaplayer_set_current_time(_nativePlayer, value.TotalSeconds); }
	}

	public bool? IsVideo => NativeUno.uno_mediaplayer_is_video(_nativePlayer);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_player is null)
		{
			return;
		}
		if (disposing)
		{
			lock (_instances)
			{
				_instances.Remove(_player);
			}
			_player = null!;
		}
	}

	public void Initialize()
	{
	}

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
			if (_uri != value)
			{
				_uri = value;
				Events?.RaiseSourceChanged();
			}

			if (_uri is null)
			{
				_player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
				return;
			}

			var uri = _uri;
			if (!uri.IsAbsoluteUri || uri.Scheme.Length == 0)
			{
				uri = new Uri(MsAppXScheme + ":///" + _uri.OriginalString.TrimStart('/'));
			}

			if (uri.IsLocalResource())
			{
				var filePath = uri.PathAndQuery;

				if (uri.Host is { Length: > 0 } host)
				{
					filePath = host + "/" + filePath.TrimStart('/');
				}

				// location differs for app bundles
				var baseUrl = NativeUno.uno_application_is_bundled() ? "[ResourcePath]" : Windows.ApplicationModel.Package.Current.InstalledPath;
				NativeUno.uno_mediaplayer_set_source_path(_nativePlayer, Path.Combine(baseUrl, filePath.TrimStart('/')));
			}
			else if (uri.IsAppData())
			{
				NativeUno.uno_mediaplayer_set_source_path(_nativePlayer, AppDataUriEvaluator.ToPath(uri));
			}
			else
			{
				NativeUno.uno_mediaplayer_set_source_uri(_nativePlayer, _uri.ToString());
			}

			_player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
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
		NativeUno.uno_mediaplayer_set_volume(_nativePlayer, (float)_player.Volume);
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
				// It's AVPlayer default behavior to clear currentItem when no next item exists
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
			Events?.RaiseMediaFailed(MediaPlayerError.Unknown, ex?.Message, ex);
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

	// Not applicable, we use the managed Uno's MTC
	public void SetTransportControlsBounds(Rect bounds)
	{
	}

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
			var ts = TimeSpan.FromSeconds(duration);
			player.NaturalDuration = ts;
			player._player.PlaybackSession.NaturalDuration = ts;
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

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMediaOpened(nint handle)
	{
		if (typeof(MacOSMediaPlayerExtension).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(MacOSMediaPlayerExtension).Log().Debug("OnMediaOpened");
		}

		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			player.Events?.RaiseMediaOpened();
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMediaEnded(nint handle)
	{
		if (typeof(MacOSMediaPlayerExtension).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(MacOSMediaPlayerExtension).Log().Debug("OnMediaEnded");
		}

		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			var media_player = player._player;
			NativeDispatcher.Main.Enqueue(() =>
			{
				player.Events?.RaiseMediaEnded();
				media_player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
				if (media_player is { IsLoopingEnabled: false, IsLoopingAllEnabled: false })
				{
					media_player.NextTrack();
				}
				if (media_player is { IsLoopingEnabled: true, IsLoopingAllEnabled: false })
				{
					media_player.Stop();
					media_player.Play();
				}
				else // IsLoopingAllEnabled
				{
					if (player._playlist is not null && player._playlist.Items.Count > 0)
					{
						player._playlistIndex = (player._playlistIndex + 1) % player._playlist.Items.Count;
						player.Uri = player._playlist.Items[player._playlistIndex]?.Source.Uri;
						media_player.Play();
					}
				}
			});
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMediaFailed(nint handle)
	{
		if (typeof(MacOSMediaPlayerExtension).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(MacOSMediaPlayerExtension).Log().Debug("OnMediaFailed");
		}

		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			player.Events?.RaiseMediaFailed(MediaPlayerError.Unknown, null, null);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMediaStalled(nint handle)
	{
		if (typeof(MacOSMediaPlayerExtension).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(MacOSMediaPlayerExtension).Log().Debug("OnMediaStalled");
		}

		var player = GetMediaPlayer(handle);
		if (player is not null)
		{
			player._player.PlaybackSession.PlaybackState = MediaPlaybackState.Buffering;
		}
	}
}
