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
using GLib;

[assembly: ApiExtension(typeof(IMediaPlayerExtension), typeof(Uno.UI.Media.MediaPlayerExtension))]

namespace Uno.UI.Media;

public partial class MediaPlayerExtension : IMediaPlayerExtension
{
	public void OnPrepared(object? sender, object what)
	{
		if (sender is GtkMediaPlayer mp)
		{
			if (_player is not null)
			{
				IsVideo = _player.IsVideo;

				if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
				{
					if (_isPlayRequested)
					{
						_player.Play();
						_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
					}
				}

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"The GtkMediaPlayer is prepared");
				}

				_isPlayerPrepared = true;
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					this.Log().Error($"The media player is not available yet");
				}
			}
		}

		if (Events is not null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Raising MediaOpened");
			}

			Events?.RaiseMediaOpened();
		}
	}

	public void OnError(object? sender, object what)
	{
		if (_owner.PlaybackSession.PlaybackState != MediaPlaybackState.None)
		{
			_player?.Stop();
			_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}
		OnMediaFailed(message: $"MediaPlayer Error: {(string)what}");
	}

	public void OnCompletion(object? sender, object what)
	{
		Events?.RaiseMediaEnded();
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
		if (IsLoopingEnabled && !IsLoopingAllEnabled)
		{
			ReInitializeSource();
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
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayerElementExtension.OnMediaFailed({message})");
		}
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
	}

	public void OnVolumeChanged()
	{
		var volume = (int)_owner.Volume;
		_player?.SetVolume(volume);
	}

	private void OnNaturalVideoDimensionChanged()
	{
		if (_player is not null
			&& _player.IsVideo
			&& Events is not null)
		{
			IsVideo = _player.IsVideo;
			Events?.RaiseNaturalVideoDimensionChanged();
		}
	}

	private void OnTimeUpdate(object? sender, object o)
	{
		try
		{
			if (_player is not null)
			{
				NaturalDuration = TimeSpan.FromSeconds(_player.Duration);
			}

			_updatingPosition = true;
			Events?.RaisePositionChanged();
		}
		finally
		{
			_updatingPosition = false;
		}
	}

	public void OnSeekComplete()
	{
		Events?.RaiseSeekCompleted();
	}
	private void OnStatusChanged(MediaPlaybackSession? sender, object args)
	{
		if (_player != null && args is MediaPlaybackState state)
		{
			switch (state)
			{
				case MediaPlaybackState.Playing:
					if (_player.CurrentState != MediaPlayerState.Playing)
					{
						_player.Play();
					}
					break;
				case MediaPlaybackState.Paused:
					if (_player.CurrentState != MediaPlayerState.Paused)
					{
						_player.Pause();
					}
					break;
				case MediaPlaybackState.None:
				case MediaPlaybackState.Opening:
				case MediaPlaybackState.Buffering:
					break;
			}
		}
	}
}
