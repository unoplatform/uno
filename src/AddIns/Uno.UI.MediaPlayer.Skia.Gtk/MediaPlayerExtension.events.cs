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

[assembly: ApiExtension(typeof(IMediaPlayerExtension), typeof(Uno.UI.Media.MediaPlayerExtension))]

namespace Uno.UI.Media;

public partial class MediaPlayerExtension : IMediaPlayerExtension
{

	private void OnStatusChanged(MediaPlaybackSession? sender, object args)
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"OnStatusChanged: {args}");
		//}

		//if ((MediaPlaybackState)args == MediaPlaybackState.Playing)
		//{
		//	_player?.Play();
		//}
		//if ((MediaPlaybackState)args == MediaPlaybackState.None)
		//{
		//	_player?.Stop();
		//}
		//if ((MediaPlaybackState)args == MediaPlaybackState.Paused)
		//{
		//	_player?.Pause();
		//}
	}
	public void OnPrepared(object? sender, object what)
	{
		if (sender is GTKMediaPlayer mp && _player is not null)
		{
			//if (this.Log().IsEnabled(LogLevel.Debug))
			//{
			//	this.Log().Debug($"OnPrepared: {_player.Duration}");
			//}

			NaturalDuration = TimeSpan.FromSeconds(_player.Duration);

			if (mp.IsVideo && Events is not null)
			{
				try
				{
					//if (this.Log().IsEnabled(LogLevel.Debug))
					//{
					//	this.Log().Debug($"OnPrepared: {mp.VideoWidth}x{mp.VideoHeight}");
					//}
					//mp.UpdateVideoStretch();
					Events?.RaiseVideoRatioChanged(global::System.Math.Max(1, (double)mp.Ratio));
				}
				catch { }
			}

			if (_owner.PlaybackSession.PlaybackState == MediaPlaybackState.Opening)
			{
				if (_isPlayRequested)
				{
					_player.Play();
					_owner.PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
				}
				else
				{
					// To display first image of media when setting a new source. Otherwise, last image of previous source remains visible
					_player.Play();
					_player.Stop();
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

		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"OnError: {what}");
		//}

		OnMediaFailed(message: $"MediaPlayer Error: {(string)what}");
	}

	public void OnCompletion(object? sender, object what)
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"OnCompletion: {_owner.Position}");
		//}

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

		this.Log().Debug($"MediaPlayerElementExtension.OnMediaFailed({message})");
		_owner.PlaybackSession.PlaybackState = MediaPlaybackState.None;
	}
	public void OnVolumeChanged()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"OnVolumeChanged: {_owner.Volume}");
		//}

		var volume = (int)_owner.Volume;
		_player?.SetVolume(volume);
	}

	private void OnTimeUpdate(object? sender, object what)
	{
		try
		{
			_updatingPosition = true;

			//if (this.Log().IsEnabled(LogLevel.Trace))
			//{
			//	this.Log().Trace($"OnTimeUpdate: {Position}");
			//}

			Events?.RaisePositionChanged();
		}
		finally
		{
			_updatingPosition = false;
		}
	}

	public void OnSeekComplete()
	{
		//if (this.Log().IsEnabled(LogLevel.Trace))
		//{
		//	this.Log().Trace($"OnSeekComplete: {Position}");
		//}

		Events?.RaiseSeekCompleted();
	}



}
