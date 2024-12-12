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
	private MediaPlayer? _player;
	private nint _nativePlayer;

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

	public MediaPlayerState CurrentState => MediaPlayerState.Closed;

	public TimeSpan NaturalDuration => TimeSpan.Zero;

	public bool IsProtected => false; // TODO

	public double BufferingProgress => 0.0d; // TODO

	public bool CanPause => false; // TODO

	public bool CanSeek => false; // TODO

	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }
	public MediaPlayerAudioCategory AudioCategory { get; set; }
	public TimeSpan TimelineControllerPositionOffset { get; set; }
	public bool RealTimePlayback { get; set; }
	public double AudioBalance { get; set; }
	public TimeSpan Position { get; set; }

	public bool? IsVideo => false; // TODO

	public void Dispose() => NotImplemented(); // TODO
	public void Initialize() => NotImplemented(); // TODO
	public void InitializeSource() => NotImplemented(); // TODO
	public void NextTrack() => NotImplemented(); // TODO
	public void OnOptionChanged(string name, object value) => NotImplemented(); // TODO
	public void OnVolumeChanged() => NotImplemented(); // TODO
	public void Pause() => NativeUno.uno_mediaplayer_pause(_nativePlayer);
	public void Play() => NativeUno.uno_mediaplayer_play(_nativePlayer);
	public void PreviousTrack() => NotImplemented(); // TODO
	public void SetFileSource(IStorageFile file) => NotImplemented(); // TODO
	public void SetMediaSource(IMediaSource source) => NotImplemented(); // TODO
	public void SetStreamSource(IRandomAccessStream stream) => NotImplemented(); // TODO
	public void SetSurfaceSize(Size size) => NotImplemented(); // TODO
	public void SetTransportControlsBounds(Rect bounds) => NotImplemented(); // TODO
	public void SetUriSource(Uri value) => NotImplemented(); // TODO
	public void StepBackwardOneFrame() => NotImplemented(); // TODO
	public void StepForwardOneFrame() => NotImplemented(); // TODO
	public void Stop() => NativeUno.uno_mediaplayer_stop(_nativePlayer);

	public void ToggleMute() => NotImplemented(); // TODO

	public void NotImplemented([CallerMemberName] string name = "unknown")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Member {name} is not implemented");
		}
	}
}
