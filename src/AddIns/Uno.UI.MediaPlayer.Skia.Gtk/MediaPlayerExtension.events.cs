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
		if (sender is GtkMediaPlayer mp && _player is not null)
		{
			NaturalDuration = TimeSpan.FromSeconds(_player.Duration);
			if (mp.IsVideo && Events is not null)
			{
				Events?.RaiseVideoRatioChanged(global::System.Math.Max(1, (double)mp.VideoRatio));
			}
			if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
			{
				if (_isPlayRequested)
				{
					_player.Play();
					_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				}
			}
			_isPlayerPrepared = true;
		}
	}

	void OnError(object? sender, object what)
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

		// Play next item in playlist, if any
		if (_playlistItems != null && _playlistIndex < _playlistItems.Count - 1)
		{
			_uri = _playlistItems[++_playlistIndex];
			ApplyVideoSource();
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

	private void OnTimeUpdate(object? sender, object o)
	{
		try
		{
			var time = o is TimeSpan e ? e : TimeSpan.Zero;
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
		//if ((MediaPlaybackState)args == MediaPlaybackState.Playing)
		//{
		//	_player?.Play();
		//}
	}
}
