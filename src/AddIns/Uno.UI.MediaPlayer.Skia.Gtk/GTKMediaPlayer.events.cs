#nullable enable

using Windows.UI.Core;
using LibVLCSharp.GTK;
using LibVLCSharp.Shared;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using Windows.UI.Xaml;
using System.Threading;
using System.Linq;
using Windows.UI.Notifications;
using Uno.Extensions;
using Uno.Logging;
using Pango;
using Windows.UI.Xaml.Media;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Globalization;
using Windows.Media.Playback;

namespace Uno.UI.Media;

public partial class GTKMediaPlayer
{
	public event EventHandler<object>? OnSourceFailed;
	public event EventHandler<object>? OnSourceEnded;
	public event EventHandler<object>? OnMetadataLoaded;
	public event EventHandler<object>? OnTimeUpdate;
	public event EventHandler<object>? OnSourceLoaded;

	private void Initialized()
	{

		Console.WriteLine("GTKMediaPlayer Initialized");
		Console.WriteLine("Creating libvlc");
		_libvlc = new LibVLC(enableDebugLogs: false);

		Console.WriteLine("Creating player");
		_mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libvlc);

		Console.WriteLine("Creating VideoView");
		_videoView = new LibVLCSharp.GTK.VideoView();

		_videoContainer = new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch,
		};

		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			Console.WriteLine("Set MediaPlayer");
			_videoView.MediaPlayer = _mediaPlayer;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				Console.WriteLine("Content _videoView on Dispatcher");

				_videoView.Visible = true;
				_videoView.MediaPlayer = _mediaPlayer;

				_mediaPlayer.TimeChanged += OnMediaPlayerTimeChange;
				_mediaPlayer.MediaChanged += MediaPlayerMediaChanged;
				_mediaPlayer.Stopped += OnMediaPlayerStopped;

				_videoContainer.Content = _videoView;
				Child = _videoContainer;
				UpdateVideoStretch();
				Console.WriteLine("Created player");
			});
		});
	}

	private void OnSourceVideoLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = true;
		}
		UpdateVideoStretch();
		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
	}

	private static void OnSourceChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is GTKMediaPlayer player)
		{
			string encodedSource = (string)args.NewValue;

			player._mediaPath = new Uri(encodedSource);
			player.UpdateMedia();
		}
	}

	private void UpdateMedia()
	{
		if (_mediaPlayer != null && _libvlc != null && _mediaPath != null)
		{
			string[] options = new string[1];
			var media = new LibVLCSharp.Shared.Media(_libvlc, _mediaPath, options);

			media.Parse(MediaParseOptions.ParseNetwork);
			_mediaPlayer.Media = media;
			while (!_mediaPlayer.Media.IsParsed)
			{
				Thread.Sleep(10);
			}
			if (_mediaPlayer != null && _mediaPlayer.Media != null)
			{
				_mediaPlayer.Media.DurationChanged += DurationChanged;
				_mediaPlayer.Media.MetaChanged += MetaChanged;
				_mediaPlayer.Media.StateChanged += StateChanged;
				_mediaPlayer.Media.ParsedChanged += ParsedChanged;
			}
			OnSourceLoaded?.Invoke(this, EventArgs.Empty);
		}
	}

	private void ParsedChanged(object? sender, EventArgs el)
	{
		Console.WriteLine("ParsedChanged");
		OnGtkSourceLoaded(sender, el);
	}

	private void DurationChanged(object? sender, EventArgs el)
	{
		Console.WriteLine("DurationChanged");
		Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
	}

	private void StateChanged(object? sender, MediaStateChangedEventArgs el)
	{
		Console.WriteLine("StateChanged");
		if (el.State == VLCState.Error)
		{
			Console.WriteLine("Error");
			OnGtkSourceFailed(sender, el);
		}
		if (el.State == VLCState.Ended)
		{
			Console.WriteLine("Ended");
			if (!_isEnding)
			{
				OnEndReached();
			}
		}
		if (el.State == VLCState.Opening)
		{
			Console.WriteLine("Opening");
			OnGtkSourceLoaded(sender, el);
		}
	}

	private void MetaChanged(object? sender, EventArgs el)
	{
		Console.WriteLine("MetaChanged");
		OnGtkMetadataLoaded(sender, el);
	}

	private void OnMediaPlayerStopped(object? sender, EventArgs el)
	{
		Console.WriteLine("MediaPlayer Stopped");
		if (_videoView != null)
		{
			if (!_isEnding)
			{
				_videoView.Visible = false;
			}
		}
	}

	private void MediaPlayerMediaChanged(object? sender, MediaPlayerMediaChangedEventArgs el)
	{
		Console.WriteLine("MediaChanged");
		OnGtkSourceLoaded(sender, el);
	}

	private void OnMediaPlayerTimeChange(object? sender, MediaPlayerTimeChangedEventArgs el)
	{
		var time = el is LibVLCSharp.Shared.MediaPlayerTimeChangedEventArgs e ? TimeSpan.FromMilliseconds(e.Time) : TimeSpan.Zero;
		OnTimeUpdate?.Invoke(this, time);
	}

	private void OnEndReached()
	{
		Console.WriteLine("OnEndReached");

		if (_libvlc != null && _videoView != null && _mediaPlayer != null && _mediaPath != null)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_isEnding = true;
				UpdateMedia();

				OnSourceEnded?.Invoke(this, EventArgs.Empty);
				if (_isLoopingEnabled)
				{
					_mediaPlayer.Play();
				}
				_videoView.Visible = true;
				_isEnding = false;
			});
		}
	}

	private void OnGtkMetadataLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null && _mediaPlayer != null && _mediaPlayer.Media != null)
		{
			Duration = (double)_mediaPlayer.Media.Duration / 1000;
		}
		OnMetadataLoaded?.Invoke(this, Duration);
	}

	private void OnGtkSourceLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = true;

			Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
			if (Duration > 0)
			{
				OnSourceLoaded?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	private void OnGtkSourceFailed(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = false;
		}
		OnSourceFailed?.Invoke(this, EventArgs.Empty);
	}

}
