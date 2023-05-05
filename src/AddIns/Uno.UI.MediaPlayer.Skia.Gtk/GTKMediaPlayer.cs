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

namespace Uno.UI.Media;

public partial class GTKMediaPlayer : Border
{
	private LibVLC? _libvlc;
	private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
	private VideoView? _videoView;

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
	public GTKMediaPlayer()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug("Adding media elements");
		//}

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}
	private void OnUnloaded(object sender, object args)
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"HtmlMediaPlayer Unloaded");
		//}

		SourceLoaded -= OnSourceVideoLoaded;
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

	private void OnLoaded(object sender, object args)
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"GTKMediaPlayer Loaded");
		//}

#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
		SourceLoaded += OnSourceVideoLoaded;
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
		Console.WriteLine("Creating libvlc");
		_libvlc = new LibVLC(enableDebugLogs: false);

		Console.WriteLine("Creating player");
		_mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libvlc);

		Console.WriteLine("Creating VideoView");
		_videoView = new LibVLCSharp.GTK.VideoView();

		_videoView.Visible = false;
		_videoView.MediaPlayer = _mediaPlayer;
		_mediaPlayer.Stopped += (sender, e) =>
		{
			_videoView.Visible = false;
		};

		//ContentView.Content = _videoView;
		//myNativeContent.Content = _videoView;

	}

	public void Play()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			_mediaPlayer.Play();
			_videoView.Visible = true;
		}
	}

	public void Stop()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			_mediaPlayer.Stop();
			_videoView.Visible = false;
		}
	}

	public void Pause()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			_mediaPlayer.Pause();
			_videoView.Visible = true;
		}
	}

	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set
		{
			SetValue(SourceProperty, value);
		}
	}
	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		"Source", typeof(string), typeof(GTKMediaPlayer), new PropertyMetadata(default(string),
			OnSourceChanged));

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
				options[0] = "video-on-top";

				var media = new LibVLCSharp.Shared.Media(player._libvlc, new Uri(encodedSource), options);
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

	internal void UpdateVideoStretch()
	{
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			if (_videoView != null && _mediaPlayer != null && _mediaPlayer.Media != null)
			{
				try
				{
					var width = _videoView.AllocatedWidth;
					var height = _videoView.AllocatedHeight;
					// var parentRatio = (double)width / global::System.Math.Max(1, height);

					_mediaPlayer.Media.Parse(MediaParseOptions.ParseNetwork);
					while (_mediaPlayer.Media.Tracks != null && !_mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
					{
						Thread.Sleep(100);
					}


					if (_mediaPlayer.Media.Tracks != null && _mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
					{
						var videoTrack = _mediaPlayer.Media.Tracks.FirstOrDefault(track => track.TrackType == TrackType.Video);
						var videoSettings = videoTrack.Data.Video;
						var videoWidth = videoSettings.Width;
						var videoHeight = videoSettings.Height;

						if (videoWidth == 0 || videoHeight == 0)
						{
							return;
						}
						var ratio = (double)videoWidth / global::System.Math.Max(1, videoHeight);
						height = (int)(width / ratio);
						_videoView?.SizeAllocate(new(0, 0, width, height));
						Console.WriteLine($"Largura: {width},  Altura: {height}");
					}
				}
				finally
				{
				}
			}
		});
	}
}
