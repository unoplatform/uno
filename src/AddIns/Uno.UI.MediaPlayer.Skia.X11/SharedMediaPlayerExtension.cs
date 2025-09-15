using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Helpers;
using Uno.Media.Playback;
using Uno.UI.Dispatching;
using MediaPlayer = Windows.Media.Playback.MediaPlayer;
using Windows.Web.Http.Headers;
using Uno.Logging;
using System.Timers;

#if IS_MPE_WIN32
namespace Uno.UI.MediaPlayer.Skia.Win32;
#else
namespace Uno.UI.MediaPlayer.Skia.X11;
#endif

public class SharedMediaPlayerExtension : IMediaPlayerExtension
{
	private static int _vlcInitialized;
	private static LibVLC _vlc = null!;

	private const string MsAppXScheme = "ms-appx";
	private static readonly ConditionalWeakTable<Windows.Media.Playback.MediaPlayer, SharedMediaPlayerExtension> _mediaPlayerToExtension = new();

	private readonly IDisposable _timerDisposable;
	private int _playlistIndex = -1; // -1 if no playlist or empty playlist, otherwise the 0-based index of the current track in the playlist
	private MediaPlaybackList? _playlist; // only set and used if the current _player.Source is a playlist

	private int _vlcPlayerVolume = 100;

	// the current effective url (e.g. current video in playlist) that is set natively
	// DO NOT READ OR WRITE THIS. It's only used to RaiseSourceChanged.
	private Uri? _uri;

	private Uri? Uri
	{
		set
		{
			if (_uri != value)
			{
				IsVideo = null;
				_uri = value;
				Events?.RaiseSourceChanged();
				// We don't return here since setting the uri to itself should reload (e.g. looping playlist of a single element)
			}

			VlcPlayer.Media?.Dispose();

			if (value is null)
			{
				VlcPlayer.Media = null;
				return;
			}

			Player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;

			var uri = value;
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + value.OriginalString.TrimStart('/'));
			}

			if (uri.IsLocalResource())
			{
				var filePath = uri.PathAndQuery;

				if (uri.Host is { Length: > 0 } host)
				{
					filePath = host + "/" + filePath.TrimStart('/');
				}

				VlcPlayer.Media = new LibVLCSharp.Shared.Media(_vlc, new Uri(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, filePath.TrimStart('/'))));
			}
			else if (uri.IsAppData())
			{
				VlcPlayer.Media = new LibVLCSharp.Shared.Media(_vlc, new Uri(AppDataUriEvaluator.ToPath(uri)));
			}
			else if (uri.IsFile)
			{
				VlcPlayer.Media = new LibVLCSharp.Shared.Media(_vlc, uri);
			}
			else
			{
				VlcPlayer.Media = new LibVLCSharp.Shared.Media(_vlc, uri);
			}

			if (VlcPlayer.Media is { } m)
			{
				var weakRef = new WeakReference<SharedMediaPlayerExtension>(this);
				m.ParsedChanged += (_, a) => weakRef.GetTarget()?.OnLoadedMetadata(a.ParsedStatus);
			}

			// This doesn't start the playback. It just force-loads the media. This is the behaviour only when --start-paused
			VlcPlayer.Play();
		}
	}

	private bool? _isVideo;
	private bool _updatingPositionFromNative;
	internal event EventHandler<bool?>? IsVideoChanged;
	public bool? IsVideo
	{
		get => _isVideo;
		private set
		{
			if (_isVideo != value)
			{
				IsVideoChanged?.Invoke(this, value);
			}
			_isVideo = value;
		}
	}

	internal Windows.Media.Playback.MediaPlayer Player { get; }

	// On Win32, EnableMouseInput needs to be false, or else libvlc will capture the pointer and we won't receive
	// any mouse events. Attempting to do this later below doesn't work for some reason. It needs to be
	// right after constructing the LibVLCSharp.Shared.MediaPlayer
	internal LibVLCSharp.Shared.MediaPlayer VlcPlayer { get; }

	public static void PreloadVlc()
	{
		Task.Run(() =>
		{
			if (Volatile.Read(ref _vlcInitialized) == 0)
			{
				var vlc = new LibVLC("--start-paused");
				try
				{
					var mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(vlc);
					var stream = typeof(SharedMediaPlayerExtension).Assembly.GetManifestResourceStream($"{typeof(SharedMediaPlayerExtension).Assembly.GetName().Name}.Assets.libvlc_init_sample.mp4");
					var media = new LibVLCSharp.Shared.Media(vlc, new StreamMediaInput(stream!));
					EventHandler<MediaParsedChangedEventArgs>? mediaOnParsedChanged = default;
					mediaOnParsedChanged = (_, a) =>
					{
						if (Interlocked.CompareExchange(ref _vlcInitialized, 1, 0) == 0)
						{
							_vlc = vlc;
						}
						else
						{
							vlc.Dispose();
						}
						media.ParsedChanged -= mediaOnParsedChanged;
					};
					media.ParsedChanged += mediaOnParsedChanged;
					mediaPlayer.Media = media;
					mediaPlayer.Play();
				}
				catch (Exception e)
				{
					typeof(SharedMediaPlayerExtension).Log().Error(e.Message);
					_vlc = vlc;
				}
			}
		});
	}

	public SharedMediaPlayerExtension(Windows.Media.Playback.MediaPlayer player)
	{
		if (Interlocked.CompareExchange(ref _vlcInitialized, 1, 0) == 0)
		{
			_vlc = new LibVLC("--start-paused");
		}

		VlcPlayer = new LibVLCSharp.Shared.MediaPlayer(_vlc) { EnableMouseInput = false, EnableKeyInput = false };
		Player = player;
		_mediaPlayerToExtension.TryAdd(player, this);

		// It's important not to let libVLC's media player grab a strong reference to this object,
		// otherwise neither will ever get collected. It seems like libVLC's media player is never
		// collected until explicitly disposed. The lifetime of X11mediaPlayerExtension is similar
		// to that of its owning MediaPlayer, which is part of the public API and  has an indefinite
		// lifetime. We rely on the GC to determine when it's time to end this object's lifetime,
		// and it turn, dispose of libVLC's media player handle as well.
		var weakRef = new WeakReference<SharedMediaPlayerExtension>(this);

		VlcPlayer.LengthChanged += (o, a) => weakRef.GetTarget()?.OnLengthChange(o, a);
		VlcPlayer.EndReached += (o, a) => weakRef.GetTarget()?.OnEndReached(o, a);
		VlcPlayer.EncounteredError += (o, a) => weakRef.GetTarget()?.OnEncounteredError(o, a);
		VlcPlayer.Playing += (o, a) => weakRef.GetTarget()?.OnPlaying(o, a);
		VlcPlayer.Buffering += (o, a) => weakRef.GetTarget()?.OnBuffering(o, a);
		VlcPlayer.Paused += (o, a) => weakRef.GetTarget()?.OnPaused(o, a);
		VlcPlayer.TimeChanged += (o, a) => weakRef.GetTarget()?.OnTimeChanged(o, a); // PositionChanged fires way too frequently (probably every frame). We use TimeChanged instead.

		//VlcPlayer.VolumeChanged += (o, a) => weakRef.GetTarget()?.OnVlcVolumeChanged(o, a);
		// We need to start a timer to update the playback state, since libVLC doesn't
		// provide a way to get the end of buffering without polling.
		// Also, attaching to VolumeChanged with debugger attached causes crashes on some systems,
		// so we poll the volume on the timer as well.
		var timer = new System.Timers.Timer(16);
		ElapsedEventHandler timerOnTick = (_, _) => weakRef.GetTarget()?.OnTick();
		timer.Elapsed += timerOnTick;
		_timerDisposable = timer;
		timer.Start();
	}


	~SharedMediaPlayerExtension()
	{
		Dispose();
	}

	internal static SharedMediaPlayerExtension? GetByMediaPlayer(Windows.Media.Playback.MediaPlayer player) => _mediaPlayerToExtension.TryGetValue(player, out var ext) ? ext : null;

	public IMediaPlayerEventsExtension? Events { get; set; }

	public double PlaybackRate
	{
		get => VlcPlayer.Rate;
		set => VlcPlayer.SetRate((float)value);
	}

	public bool IsLoopingEnabled { get; set; }

	public bool IsLoopingAllEnabled { get; set; }

	// Deprecated.
	public MediaPlayerState CurrentState => MediaPlayerState.Closed;

	public TimeSpan NaturalDuration => VlcPlayer.Media?.Duration is { } d ? TimeSpan.FromMilliseconds(d) : TimeSpan.Zero;

	public bool IsProtected => false;

	public double BufferingProgress => 0.0;

	public bool CanPause => VlcPlayer.CanPause;

	public bool CanSeek => VlcPlayer.IsSeekable;

	public MediaPlayerAudioDeviceType AudioDeviceType { get; set; }

	public MediaPlayerAudioCategory AudioCategory { get; set; }

	public TimeSpan TimelineControllerPositionOffset
	{
		get => Position;
		set => Position = value;
	}

	public bool RealTimePlayback { get; set; }

	// TODO
	public double AudioBalance { get; set; }

	public TimeSpan Position
	{
		get => TimeSpan.FromMilliseconds(Math.Max(0, VlcPlayer.Position * NaturalDuration.TotalMilliseconds));
		set
		{
			if (!_updatingPositionFromNative && NaturalDuration.TotalMilliseconds > 0)
			{
				VlcPlayer.Position = (float)(value.TotalMilliseconds / (float)NaturalDuration.TotalMilliseconds);
			}
		}
	}

	// not applicable, we use the managed uno MTC
	public void SetTransportControlsBounds(Rect bounds) { }

	public void Initialize() { }

	public void InitializeSource()
	{
		_playlistIndex = -1;
		_playlist = null;
		switch (Player.Source)
		{
			case MediaPlaybackItem item:
				Uri = item.Source.Uri;
				break;
			case MediaSource source:
				Uri = source.Uri;
				break;
			case MediaPlaybackList playlist:
				_playlist = playlist;
				_playlistIndex = _playlist.Items.Count > 0 ? 0 : -1;
				Uri = _playlist.Items.FirstOrDefault()?.Source.Uri;
				break;
			default:
				Uri = null;
				break;
		}
	}

	// Deprecated. Use MediaPlayer.Source instead
	public void SetUriSource(Uri uri) => throw new NotImplementedException();
	public void SetFileSource(IStorageFile file) => throw new NotImplementedException();
	public void SetStreamSource(IRandomAccessStream stream) => throw new NotImplementedException();
	public void SetMediaSource(IMediaSource source) => throw new NotImplementedException();

	public void StepForwardOneFrame() => VlcPlayer.NextFrame();
	// VLC only supports forward frame stepping.
	public void StepBackwardOneFrame() => throw new NotImplementedException();

	public void SetSurfaceSize(Size size) => throw new NotImplementedException();

	public void Play() => VlcPlayer.Play();

	public void Pause() => VlcPlayer.Pause();

	public void Stop()
	{
		if (OperatingSystem.IsWindows())
		{
			// On Win32, Stop() deadlocks for some reason. The best guess is that Stop does something like SendMessage
			// and needs the window message queue to continue pumping before returning, so calling it on the UI
			// thread (which also pumps the queue) deadlocks. This is not a problem on X11 because we run the X11
			// message queue on a separate thread.
			Task.Run(() =>
			{
				VlcPlayer.Stop();
			});
		}
		else
		{
			VlcPlayer.Stop();
		}
	}

	public void ToggleMute() => VlcPlayer.Mute = Player.IsMuted;

	public void OnVolumeChanged()
	{
		var volume = (int)Math.Round(Player.Volume * 100);
		if (volume != _vlcPlayerVolume)
		{
			_vlcPlayerVolume = volume;
			VlcPlayer.Volume = volume;
		}
	}

	public void OnOptionChanged(string name, object value) { }

	public void PreviousTrack()
	{
		if (_playlist != null && _playlistIndex > 0)
		{
			Uri = _playlist.Items[--_playlistIndex].Source.Uri;
			Play();
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

	public void Dispose()
	{
		try
		{
			_timerDisposable?.Dispose();
			_mediaPlayerToExtension.Remove(Player);
			VlcPlayer.Dispose();
		}
		catch (Exception)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
			{
				this.Log().Warn("Unable to dispose MediaPlayerExtension");
			}
		}
	}

	private void OnPlaying(object? _, EventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Playing);

	private void OnLoadedMetadata(MediaParsedStatus status)
	{
		if (status == MediaParsedStatus.Done)
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				Events?.RaiseNaturalVideoDimensionChanged();
				Events?.NaturalDurationChanged();
				Events?.RaiseMediaOpened();
				IsVideo = VlcPlayer.Media?.Tracks.Any(track => track.TrackType is TrackType.Video);
				VlcPlayer.Time = 1; // this shows the first frame of the video after loading instead of a black frame
			});
		}
	}

	private void OnTimeChanged(object? _, MediaPlayerTimeChangedEventArgs _1)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			var oldValue = _updatingPositionFromNative;
			_updatingPositionFromNative = true; // RaisePositionChanged will set Position, so we need a way to flag this so we can ignore it
			Events?.RaisePositionChanged();
			_updatingPositionFromNative = oldValue;
		});
	}

	private void OnBuffering(object? _, MediaPlayerBufferingEventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Buffering);

	private void OnLengthChange(object? _, MediaPlayerLengthChangedEventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() =>
		{
			if (Player.PlaybackSession.NaturalDuration != NaturalDuration)
			{
				Events?.NaturalDurationChanged();
			}
		});

	private void OnEndReached(object? _, EventArgs _1)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (VlcPlayer.Media is { Mrl: { } url } media)
			{
				// without recreating the media object, any attempt at
				// rewinding and replaying the video fails.
				// cf. https://github.com/unoplatform/uno-private/issues/1230
				VlcPlayer.Media.Dispose();
				url = url.TrimStart("file:///").Replace('/', '\\');
				VlcPlayer.Media = new LibVLCSharp.Shared.Media(_vlc, url);
				// This doesn't start the playback. It just force-loads the media. This is the behaviour only when --start-paused
				VlcPlayer.Play();
			}
			Events?.RaiseMediaEnded();
			Player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
			if (this is { IsLoopingEnabled: false, IsLoopingAllEnabled: false })
			{
				NextTrack();
			}
			if (this is { IsLoopingEnabled: true, IsLoopingAllEnabled: false })
			{
				Stop();
				Play();
			}
			else // IsLoopingAllEnabled
			{
				if (_playlist is not null && _playlist.Items.Count > 0)
				{
					_playlistIndex = (_playlistIndex + 1) % _playlist.Items.Count;
					Uri = _playlist.Items[_playlistIndex]?.Source.Uri;
					Play();
				}
			}
		});
	}

	private void OnEncounteredError(object? _, EventArgs _1)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			Events?.RaiseMediaFailed(MediaPlayerError.Unknown, null, null);
			Player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		});
	}

	private void OnPaused(object? _, EventArgs _1)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Paused);

	private void OnTick()
	{
		// This is primarily to update the Buffering status, since libVLC doesn't
		// expose a BufferingEnded event.
		MediaPlaybackState? state = null;
		switch (VlcPlayer.State)
		{
			case VLCState.Opening:
				state = MediaPlaybackState.Opening;
				break;
			case VLCState.Buffering:
				state = MediaPlaybackState.Buffering;
				break;
			case VLCState.Playing:
				state = MediaPlaybackState.Playing;
				break;
			case VLCState.Paused:
				state = MediaPlaybackState.Paused;
				break;
			case VLCState.Stopped:
			case VLCState.Ended:
			case VLCState.Error:
			case VLCState.NothingSpecial:
				break;
		}

		if (state != null && state != Player.PlaybackSession.PlaybackState)
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				Player.PlaybackSession.PlaybackState = state.Value;
			});
		}

		// We also update the volume, in case it has has been changed externally.
		var volume = VlcPlayer.Volume;
		if (_vlcPlayerVolume != volume && volume != -1)
		{
			_vlcPlayerVolume = volume;
			NativeDispatcher.Main.Enqueue(() =>
			{
				double newVolume = volume / 100.0; // VlcPlayer.Volume is in [0, 100]
				Events?.RaiseVolumeChanged(newVolume);
			});
		}
	}
}
