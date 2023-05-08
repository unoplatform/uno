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
using System.Globalization;
using System.Collections.Immutable;
using System.IO;
using Uno.Extensions;

namespace Uno.UI.Media;

public partial class GTKMediaPlayer : Button
{
	private LibVLC? _libvlc;
	private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
	private VideoView? _videoView;
	private double _ratio;
	public double Duration { get; set; }
	private readonly ImmutableArray<string> audioTagAllowedFormats = ImmutableArray.Create(new string[] { ".MP3", ".WAV" });
	private readonly ImmutableArray<string> videoTagAllowedFormats = ImmutableArray.Create(new string[] { ".MP4", ".WEBM", ".OGG" });
	public VideoView? VideoView
	{
		get => _videoView;
		set
		{
			_videoView = value;
		}
	}
	public double Ratio
	{
		get => _ratio;
		set
		{
			_ratio = value;
		}
	}
	public bool IsVideo
	{
		get => videoTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	}
	public bool IsAudio
	{
		get => audioTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	}
	public GTKMediaPlayer()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug("Adding media elements");
		//}

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		//RaiseLoaded();
		Initialized();
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

	public void SetVolume(int volume)
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			_mediaPlayer.Volume = volume;
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

	internal void UpdateVideoStretch()
	{
		Console.WriteLine("UpdateVideoStretch");
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			if (_videoView != null && _mediaPlayer != null && _mediaPlayer.Media != null)
			{
				try
				{
					_mediaPlayer.Media.Parse(MediaParseOptions.ParseNetwork);
					var width = _videoView.AllocatedWidth;
					var height = _videoView.AllocatedHeight;
					// var parentRatio = (double)width / global::System.Math.Max(1, height);

					Console.WriteLine($"MediaPlayer {width} x {width}");
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
						if (videoWidth == 0 || videoHeight == 0)
						{
							return;
						}
						if (width == 1)
						{
							width = (int)videoWidth;
						}

						Ratio = (double)videoWidth / global::System.Math.Max(1, videoHeight);
						height = (int)(videoWidth / Ratio);

						Console.WriteLine($"Video Ratio {Ratio}");
						Console.WriteLine($"VideoView SizeAllocate after Video Ratio {width} x {width}");
						_videoView?.SizeAllocate(new(100, 100, width, height));

						if (_videoView != null)
						{
							_videoView.Visible = true;
						}
						Console.WriteLine($"After VideoView SizeAllocate: {width},  Altura: {height}");
					}
				}
				finally
				{
				}
			}
		});
	}
	private double _playbackRate;
	public double PlaybackRate
	{
		get => _playbackRate;
		set
		{
			_playbackRate = value;
		}
	}
	public double CurrentPosition
	{
		get
		{
			if (_videoView == null || _mediaPlayer == null)
			{
				return 0;
			}
			return _mediaPlayer.Time;
		}
		set
		{
			if (_videoView != null && _mediaPlayer != null)
			{
				_mediaPlayer.Time = long.Parse(value.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
			}
		}
	}
	private bool _isLoopingEnabled;
	public void SetIsLoopingEnabled(bool value)
	{
		_isLoopingEnabled = value;
		if (_videoView != null && _mediaPlayer != null)
		{
			//TODO: version 3
			//_mediaPlayer.PositionChanged += 
			//public void MediaEndReached(object sender, EventArgs args)
			//{
			//	ThreadPool.QueueUserWorkItem(() => this.MediaPlayer.Stop());
			//}
			//version 4
			//new LibVLC("--input-repeat=2");
		}
	}
	internal void UpdateVideoStretch(Windows.UI.Xaml.Media.Stretch stretch)
	{

		switch (stretch)
		{
			case Windows.UI.Xaml.Media.Stretch.None:
				//_htmlVideo.SetCssStyle("object-fit", "none");
				break;
			case Windows.UI.Xaml.Media.Stretch.Fill:
				//_htmlVideo.SetCssStyle("object-fit", "fill");
				break;
			case Windows.UI.Xaml.Media.Stretch.Uniform:
				//_htmlVideo.SetCssStyle("object-fit", "cover");
				break;
			case Windows.UI.Xaml.Media.Stretch.UniformToFill:
				//_htmlVideo.SetCssStyle("object-fit", "contain");
				break;
		}
	}

}
