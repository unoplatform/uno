#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.Media.Playback;
using Uno.UI.DataBinding;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserMediaPlayerExtension : IMediaPlayerExtension
{
	private const string MsAppXScheme = "ms-appx";
	private static readonly ConditionalWeakTable<MediaPlayer, ManagedWeakReference> _mediaPlayerToExtension = new();
	private static readonly ConcurrentDictionary<string, ManagedWeakReference> _elementIdToMediaPlayer = new();

	private readonly MediaPlayer _player;
	private readonly DispatcherTimer _onTimeUpdateTimer;

	private bool _updatingPositionFromNative;

	private int _playlistIndex = -1; // -1 if no playlist or empty playlist, otherwise the 0-based index of the current track in the playlist
	private MediaPlaybackList? _playlist; // only set and used if the current _player.Source is a playlist

	public BrowserHtmlElement HtmlElement { get; }

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

			_player.PlaybackSession.PlaybackState = MediaPlaybackState.Opening;

			if (value is null)
			{
				NativeMethods.SetSource(HtmlElement.ElementId, "");
				return;
			}

			var uri = value;
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + value.OriginalString.TrimStart('/'));
			}

			if (uri.IsLocalResource())
			{
				if (AssetsPathBuilder.BuildAssetUri(uri.PathAndQuery) is { } source)
				{
					NativeMethods.SetSource(HtmlElement.ElementId, source);
				}
				else
				{
					NativeMethods.SetSource(HtmlElement.ElementId, "");
				}
			}
			else if (uri.IsAppData())
			{
				NativeMethods.SetSource(HtmlElement.ElementId, AppDataUriEvaluator.ToPath(uri));
			}
			else if (uri.IsFile)
			{
				NativeMethods.SetSource(HtmlElement.ElementId, uri.OriginalString);
			}
			else
			{
				NativeMethods.SetSource(HtmlElement.ElementId, uri.OriginalString);
			}
		}
	}

	private bool? _isVideo;
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

	public BrowserMediaPlayerExtension(MediaPlayer player)
	{
		NativeMethods.BuildImports();
		_player = player;
		var weakThis = WeakReferencePool.RentWeakReference(this, this);
		_mediaPlayerToExtension.TryAdd(player, weakThis);

		HtmlElement = BrowserHtmlElement.CreateHtmlElement("video");
		_elementIdToMediaPlayer.TryAdd(HtmlElement.ElementId, weakThis);

		NativeMethods.SetupEvents(HtmlElement.ElementId);

		// using the native timeupdate fires way too frequently (probably every frame) and chokes
		// the event loop, so we limit this to 60 times a second.
		_onTimeUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
		_onTimeUpdateTimer.Tick += (_, _) =>
		{
			if (weakThis.Target is BrowserMediaPlayerExtension @this)
			{
				OnTimeUpdate(@this.HtmlElement.ElementId);
			}
		};
		OnTimeUpdate(HtmlElement.ElementId);
		_onTimeUpdateTimer.Start();
	}

	internal static BrowserMediaPlayerExtension? GetByMediaPlayer(MediaPlayer player)
		=> _mediaPlayerToExtension.TryGetValue(player, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension extension ? extension : null;

	public IMediaPlayerEventsExtension? Events { get; set; }

	public double PlaybackRate
	{
		get => NativeMethods.GetVideoPlaybackRate(HtmlElement.ElementId);
		set => NativeMethods.SetVideoPlaybackRate(HtmlElement.ElementId, value);
	}

	public bool IsLoopingEnabled
	{
		get => NativeMethods.GetIsVideoLooped(HtmlElement.ElementId);
		set => NativeMethods.SetIsVideoLooped(HtmlElement.ElementId, value);
	}

	public bool IsLoopingAllEnabled { get; set; }

	// Deprecated.
	public MediaPlayerState CurrentState { get; }

	public TimeSpan NaturalDuration => TimeSpan.FromSeconds(NativeMethods.GetDuration(HtmlElement.ElementId));

	public bool IsProtected => false;

	public double BufferingProgress => 0.0;

	public bool CanPause => true;

	public bool CanSeek => true;

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
		get => TimeSpan.FromSeconds(NativeMethods.GetPosition(HtmlElement.ElementId));
		set
		{
			if (!_updatingPositionFromNative)
			{
				NativeMethods.SetPosition(HtmlElement.ElementId, value.TotalSeconds);
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
		switch (_player.Source)
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

	// Web APIs don't support this.
	public void StepForwardOneFrame() => throw new NotImplementedException();
	public void StepBackwardOneFrame() => throw new NotImplementedException();

	// Not applicable on WASM
	public void SetSurfaceSize(Size size) => throw new NotImplementedException();

	public void Play() => NativeMethods.Play(HtmlElement.ElementId);

	public void Pause() => NativeMethods.Pause(HtmlElement.ElementId);

	public void Stop() => NativeMethods.Stop(HtmlElement.ElementId);

	public void ToggleMute() => NativeMethods.SetMuted(HtmlElement.ElementId, _player.IsMuted);

	public void OnVolumeChanged() => NativeMethods.SetVolume(HtmlElement.ElementId, _player.Volume / 100);

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

	public void Dispose() => Dispose(true);
	~BrowserMediaPlayerExtension() => Dispose(false);

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			GC.SuppressFinalize(this);
		}
		_onTimeUpdateTimer.Stop();
		_mediaPlayerToExtension.Remove(_player);
		_elementIdToMediaPlayer.TryRemove(HtmlElement.ElementId, out var weakRef);
		WeakReferencePool.ReturnWeakReference(this, weakRef);
		HtmlElement.Dispose();
	}

	[JSExport]
	private static void OnPlaying(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this._player.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
		}
	}

	[JSExport]
	private static void OnLoadedMetadata(string id, bool isVideo)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this.Events?.RaiseNaturalVideoDimensionChanged();
			@this.Events?.RaiseMediaOpened();
			@this._player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
			@this.IsVideo = isVideo;
		}
	}

	[JSExport]
	private static void OnStalled(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this._player.PlaybackSession.PlaybackState = MediaPlaybackState.Buffering;
		}
	}

	[JSExport]
	private static void OnRateChange(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this.Events?.RaiseMediaPlayerRateChanged(@this.PlaybackRate);
		}
	}

	[JSExport]
	private static void OnDurationChange(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this.Events?.NaturalDurationChanged();
		}
	}

	[JSExport]
	private static void OnEnded(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this.Events?.RaiseMediaEnded();
			@this._player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
			if (@this is { IsLoopingEnabled: false, IsLoopingAllEnabled: false })
			{
				@this.NextTrack();
			}
			if (@this is { IsLoopingEnabled: true, IsLoopingAllEnabled: false })
			{
				@this.Stop();
				@this.Play();
			}
			else // IsLoopingAllEnabled
			{
				if (@this._playlist is not null && @this._playlist.Items.Count > 0)
				{
					@this._playlistIndex = (@this._playlistIndex + 1) % @this._playlist.Items.Count;
					@this.Uri = @this._playlist.Items[@this._playlistIndex]?.Source.Uri;
					@this.Play();
				}
			}
		}
	}

	[JSExport]
	private static void OnError(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this.Events?.RaiseMediaFailed(MediaPlayerError.Unknown, null, null);
			@this._player.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}
	}

	[JSExport]
	private static void OnPause(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this._player.PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
		}
	}

	[JSExport]
	private static void OnSeeked(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this.Events?.RaiseSeekCompleted();
		}
	}

	[JSExport]
	private static void OnVolumeChange(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this.Events?.RaiseVolumeChanged();
		}
	}

	[JSExport]
	private static void OnTimeUpdate(string id)
	{
		if (_elementIdToMediaPlayer.TryGetValue(id, out var weakRef) && weakRef.Target is BrowserMediaPlayerExtension @this)
		{
			@this._updatingPositionFromNative = true; // RaisePositionChanged will set Position, so we need a way to flag this so we can ignore it
			@this.Events?.RaisePositionChanged();
			@this._updatingPositionFromNative = false;
		}
	}

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.buildImports")]
		public static partial void BuildImports();
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.getVideoPlaybackRate")]
		public static partial double GetVideoPlaybackRate(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.setVideoPlaybackRate")]
		public static partial void SetVideoPlaybackRate(string elementId, double playbackRate);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.getIsVideoLooped")]
		public static partial bool GetIsVideoLooped(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.setIsVideoLooped")]
		public static partial void SetIsVideoLooped(string elementId, bool isLooped);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.getDuration")]
		public static partial double GetDuration(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.play")]
		public static partial void Play(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.pause")]
		public static partial void Pause(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.stop")]
		public static partial void Stop(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.getPosition")]
		public static partial double GetPosition(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.setPosition")]
		public static partial void SetPosition(string elementId, double position);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.setSource")]
		private static partial void _setSource(string elementId, string uri);
		public static void SetSource(string elementId, string uri)
		{
			Console.WriteLine($"Setting native source to {uri}");
			_setSource(elementId, uri);
		}

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.toggleMute")]
		public static partial void SetMuted(string elementId, bool muted);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.setVolume")]
		public static partial void SetVolume(string elementId, double volume);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerExtension)}.setupEvents")]
		public static partial void SetupEvents(string elementId);
	}
}
