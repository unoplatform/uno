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

[assembly: ApiExtension(typeof(IMediaPlayerExtension), typeof(Uno.UI.Media.MediaPlayerExtension))]

namespace Uno.UI.Media;

public partial class MediaPlayerExtension : IMediaPlayerExtension
{
	private static Dictionary<Windows.Media.Playback.MediaPlayer, MediaPlayerExtension> _instances = new();

	private Uri? _uri;
	private List<Uri>? _playlistItems;
	private readonly Windows.Media.Playback.MediaPlayer _owner;
	public GTKMediaPlayer? _player;

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

		// _player.OnSourceFailed -= OnError;
		// _player.OnSourceLoaded -= OnPrepared;


		ApplyVideoSource();
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
			_player.Play();
		}
		catch (global::System.Exception)
		{
			//if (this.Log().IsEnabled(LogLevel.Debug))
			//{
			//	this.Log().Debug($"MediaPlayerExtension.Play(): Failed {ex}");
			//}
			//OnMediaFailed(ex);
		}
	}

	public IMediaPlayerEventsExtension? Events { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public double PlaybackRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public bool IsLoopingEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public MediaPlayerState CurrentState => throw new NotImplementedException();

	public TimeSpan NaturalDuration => throw new NotImplementedException();

	public bool IsProtected => throw new NotImplementedException();

	public double BufferingProgress => throw new NotImplementedException();

	public bool CanPause => throw new NotImplementedException();

	public bool CanSeek => throw new NotImplementedException();

	public MediaPlayerAudioDeviceType AudioDeviceType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public MediaPlayerAudioCategory AudioCategory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public TimeSpan TimelineControllerPositionOffset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public bool RealTimePlayback { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public double AudioBalance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public TimeSpan Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public void SetUriSource(Uri value) => throw new NotImplementedException();
	public void SetFileSource(IStorageFile file) => throw new NotImplementedException();
	public void SetStreamSource(IRandomAccessStream stream) => throw new NotImplementedException();
	public void SetMediaSource(IMediaSource source) => throw new NotImplementedException();
	public void StepForwardOneFrame() => throw new NotImplementedException();
	public void StepBackwardOneFrame() => throw new NotImplementedException();
	public void SetSurfaceSize(Size size) => throw new NotImplementedException();
	public void ToggleMute() => throw new NotImplementedException();
	public void OnVolumeChanged() => throw new NotImplementedException();
	public void Initialize() => throw new NotImplementedException();
	public void OnOptionChanged(string name, object value) => throw new NotImplementedException();
	public void Dispose() => throw new NotImplementedException();
}
