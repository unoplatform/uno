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
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"GTKMediaPlayer Loaded");
		//}

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
				_videoView.Visible = true;
				_videoView.MediaPlayer = _mediaPlayer;
				_mediaPlayer.Stopped += (sender, e) =>
				{
					Console.WriteLine("MediaPlayer Stopped");
					_videoView.Visible = false;
				};
				_mediaPlayer.TimeChanged += (sender, el) =>
				{
					var time = el is LibVLCSharp.Shared.MediaPlayerTimeChangedEventArgs e ? TimeSpan.FromMilliseconds(e.Time) : TimeSpan.Zero;

					OnTimeUpdate?.Invoke(this, time);
				};
				_mediaPlayer.EndReached += (sender, el) =>
				{
					Console.WriteLine("EndReached");
					OnHtmlSourceEnded(sender, el);
				};
				_mediaPlayer.MediaChanged += (sender, el) =>
				{
					Console.WriteLine("MediaChanged");
					OnHtmlSourceLoaded(sender, el);
				};

				Console.WriteLine("Content _videoView on Dispatcher");
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

			//if (player.Log().IsEnabled(LogLevel.Debug))
			//{
			//	player.Log().Debug($"GTKMediaPlayer.OnSourceChanged: {args.NewValue} isVideo:{player.IsVideo} isAudio:{player.IsAudio}");
			//}
			if (player._mediaPlayer != null && player._libvlc != null)
			{
				string[] options = new string[1];
				var media = new LibVLCSharp.Shared.Media(player._libvlc, new Uri(encodedSource), options);

				media.Parse(MediaParseOptions.ParseNetwork);
				player._mediaPlayer.Media = media;
				while (!player._mediaPlayer.Media.IsParsed)
				{
					Thread.Sleep(10);
				}

				if (player._mediaPlayer != null && player._mediaPlayer.Media != null)
				{
					player._mediaPlayer.Media.DurationChanged += (sender, el) =>
					{
						Console.WriteLine("DurationChanged");
						player.Duration = (double)(player._videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
					};
					player._mediaPlayer.Media.MetaChanged += (sender, el) =>
					{
						Console.WriteLine("MetaChanged");
						player.OnHtmlMetadataLoaded(sender, el);
					};
					player._mediaPlayer.Media.StateChanged += (sender, el) =>
					{
						Console.WriteLine("StateChanged");
						if (el.State == VLCState.Error)
						{
							Console.WriteLine("Error");
							player.OnHtmlSourceFailed(sender, el);
						}
						if (el.State == VLCState.Ended)
						{
							Console.WriteLine("Ended");
							player.OnHtmlSourceEnded(sender, el);
						}
						if (el.State == VLCState.Opening)
						{
							Console.WriteLine("Opening");
							player.OnHtmlSourceLoaded(sender, el);
						}
					};

					player._mediaPlayer.Media.ParsedChanged += (sender, el) =>
					{
						Console.WriteLine("ParsedChanged");
						player.OnHtmlSourceLoaded(sender, el);
					};

				}
				//if (player.Log().IsEnabled(LogLevel.Debug))
				//{
				//	player.Log().Debug($"MediaPlayer source changed: [{player.Source}]");
				//}

				//player.OnSourceLoaded?.Invoke(player, EventArgs.Empty);
			}
		}
	}

	private void OnHtmlSourceEnded(object? sender, EventArgs e)
	{
		//if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		//{
		//	this.Log().Debug($"Media ended [{Source}]");
		//}

		OnSourceEnded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlMetadataLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null && _mediaPlayer != null && _mediaPlayer.Media != null)
		{
			Duration = (double)_mediaPlayer.Media.Duration / 1000;
		}
		OnMetadataLoaded?.Invoke(this, Duration);
	}

	private void OnHtmlSourceLoaded(object? sender, EventArgs e)
	{
		//if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		//{
		//	this.Log().Debug($"Media opened [{Source}]");
		//}
		if (_videoView != null)
		{
			_videoView.Visible = true;

			Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
			if (Duration > 0)
			{
				OnSourceLoaded?.Invoke(this, EventArgs.Empty);
			}
		}
		//if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		//{
		//	this.Log().Debug($"{ActiveElementName} source loaded: [{Source}]");
		//}

	}

	private void OnHtmlSourceFailed(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = false;
		}
		OnSourceFailed?.Invoke(this, EventArgs.Empty);
	}

}
