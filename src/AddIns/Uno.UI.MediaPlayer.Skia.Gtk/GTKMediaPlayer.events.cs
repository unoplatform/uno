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

namespace Uno.UI.Media;

public partial class GTKMediaPlayer : Button
{

	public event EventHandler<object>? OnSourceFailed;
	public event EventHandler<object>? OnSourceEnded;
	public event EventHandler<object>? OnMetadataLoaded;
	public event EventHandler<object>? OnTimeUpdate;
	public event EventHandler<object>? OnSourceLoaded;
	public event EventHandler SourceLoaded
	{
		add
		{
			//_htmlVideo.RegisterHtmlEventHandler("loadeddata", value);
			//_htmlAudio.RegisterHtmlEventHandler("loadeddata", value);
		}
		remove
		{
			//_htmlVideo.UnregisterHtmlEventHandler("loadeddata", value);
			//_htmlAudio.UnregisterHtmlEventHandler("loadeddata", value);
		}
	}
	public event EventHandler? SourceFailed
	{
		add
		{
		}
		remove
		{
		}
	}
	public event EventHandler? SourceEnded
	{
		add
		{
		}
		remove
		{
		}
	}
	public event EventHandler? MetadataLoaded
	{
		add
		{
		}
		remove
		{
		}
	}
	public event EventHandler? TimeUpdated
	{
		add
		{
		}
		remove
		{
		}
	}
	private void OnLoaded(object sender, object args)
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"GTKMediaPlayer Loaded");
		//}

		Console.WriteLine("GTKMediaPlayer OnLoaded");
		SourceLoaded += OnSourceVideoLoaded;
		Console.WriteLine("Creating libvlc");
		_libvlc = new LibVLC(enableDebugLogs: false);

		Console.WriteLine("Creating player");
		_mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libvlc);

		Console.WriteLine("Creating VideoView");
		_videoView = new LibVLCSharp.GTK.VideoView();


		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			Console.WriteLine("Set MediaPlayer");
			_videoView.MediaPlayer = _mediaPlayer;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{

				_videoView.Visible = true;
				//_videoView.MediaPlayer = _mediaPlayer;
				_mediaPlayer.Stopped += (sender, e) =>
				{
					_videoView.Visible = false;
				};
				Console.WriteLine("Content _videoView on Dispatcher");
				Content = _videoView;

				//_videoView?.SizeAllocate(new(0, 0, 800, 640));
				UpdateVideoStretch();
				Console.WriteLine("Created player");
			});
		});




		SourceLoaded += OnHtmlSourceLoaded;
		SourceFailed += OnHtmlSourceFailed;
		SourceEnded += OnHtmlSourceEnded;
		MetadataLoaded += OnHtmlMetadataLoaded;
		TimeUpdated += OnHtmlTimeUpdated;
	}
	private void Initialized()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"GTKMediaPlayer Loaded");
		//}

		Console.WriteLine("GTKMediaPlayer Initialized");
		SourceLoaded += OnSourceVideoLoaded;
		Console.WriteLine("Creating libvlc");
		_libvlc = new LibVLC(enableDebugLogs: false);

		Console.WriteLine("Creating player");
		_mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libvlc);

		Console.WriteLine("Creating VideoView");
		_videoView = new LibVLCSharp.GTK.VideoView();


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
					_videoView.Visible = false;
				};
				//Starts playing
				var media = new LibVLCSharp.Shared.Media(
					_libvlc,
					new Uri("https://ia800201.us.archive.org/12/items/BigBuckBunny_328/BigBuckBunny_512kb.mp4")
				);

				media.Parse(MediaParseOptions.ParseNetwork);
				_videoView.MediaPlayer.Media = media;

				Console.WriteLine("Content _videoView on Dispatcher");
				Content = _videoView;

				_videoView?.SizeAllocate(new(0, 0, 800, 640));
				UpdateVideoStretch();
				Console.WriteLine("Created player");
			});
		});




		SourceLoaded += OnHtmlSourceLoaded;
		SourceFailed += OnHtmlSourceFailed;
		SourceEnded += OnHtmlSourceEnded;
		MetadataLoaded += OnHtmlMetadataLoaded;
		TimeUpdated += OnHtmlTimeUpdated;
	}

	private void OnUnloaded(object sender, object args)
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"HtmlMediaPlayer Unloaded");
		//}

		SourceLoaded -= OnHtmlSourceLoaded;
		SourceFailed -= OnHtmlSourceFailed;
		SourceEnded -= OnHtmlSourceEnded;
		MetadataLoaded -= OnHtmlMetadataLoaded;
		TimeUpdated -= OnHtmlTimeUpdated;
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
				//options[0] = "video-on-top";
				var media = new LibVLCSharp.Shared.Media(player._libvlc, new Uri(encodedSource), options);
				//media.par parseWithOptions(VLC::Media::ParseFlags::Network, -1);

				media.Parse(MediaParseOptions.ParseNetwork);
				player._mediaPlayer.Media = media;
				//UpdateVideoStretch();

			}
			//if (player.Log().IsEnabled(LogLevel.Debug))
			//{
			//	player.Log().Debug($"MediaPlayer source changed: [{player.Source}]");
			//}

			player.OnSourceLoaded?.Invoke(player, EventArgs.Empty);
		}
	}


	private void OnHtmlTimeUpdated(object? sender, EventArgs e)
	{
		//if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		//{
		//	this.Log().Debug($"Time updated [{Source}]");
		//}

		OnTimeUpdate?.Invoke(this, EventArgs.Empty);
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
		if (_videoView != null && _mediaPlayer != null)
		{
			Duration = _mediaPlayer.Time;
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
		}
		//if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		//{
		//	this.Log().Debug($"{ActiveElementName} source loaded: [{Source}]");
		//}

		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
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
