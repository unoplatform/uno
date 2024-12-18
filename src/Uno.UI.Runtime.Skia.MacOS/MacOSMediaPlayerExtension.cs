#nullable enable

using System.Runtime.CompilerServices;

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

	private MacOSMediaPlayerExtension(object owner)
	{
		if (owner is MediaPlayer player)
		{
			_player = player;
			_nativePlayer = NativeUno.uno_mediaplayer_create();
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

	public static void Register() => ApiExtensibility.Register(typeof(IMediaPlayerExtension), o => new MacOSMediaPlayerExtension(o));


	public IMediaPlayerEventsExtension? Events { get; set; }
	public double PlaybackRate { get; set; }
	public bool IsLoopingEnabled { get; set; }
	public bool IsLoopingAllEnabled { get; set; }

	public MediaPlayerState CurrentState { get; }

	public TimeSpan NaturalDuration => TimeSpan.Zero;

	public bool IsProtected => false;

	public double BufferingProgress => 0.0d;

	public bool CanPause => true;

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

		// Reset player
		// TryDisposePlayer();

		if (_player.Source == null)
		{
			return;
		}

		try
		{
			// InitializePlayer();

			_player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;

			switch (_player.Source)
			{
				case MediaPlaybackList playlist:
					// Play(playlist);
					break;
				case MediaPlaybackItem item:
					// Play(item.Source.Uri);
					NativeUno.uno_mediaplayer_set_source(_nativePlayer, item.Source.Uri.ToString());
					break;
				case MediaSource source:
					// Play(source.Uri);
					NativeUno.uno_mediaplayer_set_source(_nativePlayer, source.Uri.ToString());
					break;
				default:
					throw new InvalidOperationException("Unsupported media source type");
			}
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

	public void NextTrack() => NotImplemented(); // TODO
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

	public void PreviousTrack() => NotImplemented(); // TODO

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
}
