using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Helpers;
using Uno.Media.Playback;
using Uno.UI.Dispatching;
using MediaPlayer = Windows.Media.Playback.MediaPlayer;

[assembly: ApiExtension(typeof(IMediaPlayerExtension), typeof(Uno.UI.MediaPlayer.Skia.X11.X11MediaPlayerExtension), typeof(MediaPlayer))]

namespace Uno.UI.MediaPlayer.Skia.X11;

internal class X11MediaPlayerExtension : IMediaPlayerExtension
{
	private static readonly LibVLC _vlc = new LibVLC(":start-paused");

	private const string MsAppXScheme = "ms-appx";
	private static readonly ConcurrentDictionary<Windows.Media.Playback.MediaPlayer, X11MediaPlayerExtension> _mediaPlayerToExtension = new();

	private readonly DispatcherTimer _timer;

	private int _playlistIndex = -1; // -1 if no playlist or empty playlist, otherwise the 0-based index of the current track in the playlist
	private MediaPlaybackList? _playlist; // only set and used if the current _player.Source is a playlist

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
				m.ParsedChanged += (_, args) => OnLoadedMetadata(args.ParsedStatus);
			}
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

	internal LibVLCSharp.Shared.MediaPlayer VlcPlayer { get; } = new LibVLCSharp.Shared.MediaPlayer(_vlc);

	public X11MediaPlayerExtension(Windows.Media.Playback.MediaPlayer player)
	{
		Player = player;
		_mediaPlayerToExtension.TryAdd(player, this);
		VlcPlayer.LengthChanged += OnLengthChange;
		VlcPlayer.EndReached += OnEndReached;
		VlcPlayer.EncounteredError += OnEncounteredError;
		VlcPlayer.Playing += OnPlaying;
		VlcPlayer.Buffering += OnBuffering;
		VlcPlayer.Paused += OnPaused;
		VlcPlayer.VolumeChanged += OnVolumeChanged;

		// using the native PositionChanged fires way too frequently (probably every frame) and chokes
		// the event loop, so we limit this to 60 times a second.
		// VlcPlayer.PositionChanged += OnTimeUpdate;
		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
		_timer.Tick += (_, _) => OnTick();
		_timer.Start();
	}

	internal static X11MediaPlayerExtension? GetByMediaPlayer(Windows.Media.Playback.MediaPlayer player) => _mediaPlayerToExtension.GetValueOrDefault(player);

	public IMediaPlayerEventsExtension? Events { get; set; }

	public double PlaybackRate
	{
		get => VlcPlayer.Rate;
		set => VlcPlayer.SetRate((float)value);
	}

	public bool IsLoopingEnabled { get; set; }

	public bool IsLoopingAllEnabled { get; set; }

	// Deprecated.
	public MediaPlayerState CurrentState { get; }

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
			if (!_updatingPositionFromNative)
			{
				VlcPlayer.Position = NaturalDuration.TotalMilliseconds > 0 ? value.Milliseconds * 1.0f / (float)NaturalDuration.TotalMilliseconds : 0;
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

	public void Stop() => VlcPlayer.Stop();

	public void ToggleMute() => VlcPlayer.Mute = Player.IsMuted;

	public void OnVolumeChanged() => VlcPlayer.Volume = (int)Math.Round(Player.Volume);

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
		_mediaPlayerToExtension.TryRemove(Player, out _);
		VlcPlayer.Dispose();
	}

	private void OnPlaying(object? sender, EventArgs eventArgs)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Playing);

	private void OnLoadedMetadata(MediaParsedStatus status)
	{
		if (status == MediaParsedStatus.Done)
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				Events?.RaiseNaturalVideoDimensionChanged();
				Events?.RaiseMediaOpened();
				IsVideo = VlcPlayer.Media?.Tracks.Any(track => track.TrackType is TrackType.Video);
			});
		}
	}

	private void OnBuffering(object? sender, MediaPlayerBufferingEventArgs mediaPlayerBufferingEventArgs)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Buffering);

	private void OnLengthChange(object? sender, MediaPlayerLengthChangedEventArgs mediaPlayerLengthChangedEventArgs)
		=> NativeDispatcher.Main.Enqueue(() => Events?.NaturalDurationChanged());

	private void OnEndReached(object? sender, EventArgs eventArgs)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
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

	private void OnEncounteredError(object? sender, EventArgs eventArgs)
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			Events?.RaiseMediaFailed(MediaPlayerError.Unknown, null, null);
			Player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		});
	}

	private void OnPaused(object? sender, EventArgs eventArgs)
		=> NativeDispatcher.Main.Enqueue(() => Player.PlaybackSession.PlaybackState = MediaPlaybackState.Paused);

	private void OnVolumeChanged(object? sender, MediaPlayerVolumeChangedEventArgs mediaPlayerVolumeChangedEventArgs)
		=> NativeDispatcher.Main.Enqueue(() => Events?.RaiseVolumeChanged());

	private void OnTick()
	{
		_updatingPositionFromNative = true; // RaisePositionChanged will set Position, so we need a way to flag this so we can ignore it
		Events?.RaisePositionChanged();
		_updatingPositionFromNative = false;

		// This is primarily to update the Buffering status, since libVLC doesn't
		// expose a BufferingEnded event.
		switch (VlcPlayer.State)
		{
			case VLCState.Opening:
				Player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;
				break;
			case VLCState.Buffering:
				Player.PlaybackSession.PlaybackState = MediaPlaybackState.Buffering;
				break;
			case VLCState.Playing:
				Player.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				break;
			case VLCState.Paused:
				Player.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
				break;
			case VLCState.Stopped:
			case VLCState.Ended:
			case VLCState.Error:
			case VLCState.NothingSpecial:
				break;
		}
	}
}
