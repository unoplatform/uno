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
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Point = Windows.Foundation.Point;

namespace Uno.UI.Media;

public partial class GTKMediaPlayer : Border
{
	private LibVLC? _libvlc;
	private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
	private ContentControl? _videoContainer;
	private VideoView? _videoView;
	private double _ratio;
	//public int VideoHeight;
	//public int VideoWidth;
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

		//Loaded += OnLoaded;
		//Unloaded += OnUnloaded;
		//RaiseLoaded();
		Initialized();

		//SizeChanged += GTKMediaPlayer_SizeChanged;
	}

	//private void GTKMediaPlayer_SizeChanged(object sender, SizeChangedEventArgs args)
	//{
	//	UpdateVideoStretch();
	//}

	public void Play()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			Console.WriteLine($"Play");
			_mediaPlayer.Play();
			_videoView.Visible = true;
			//_videoView?.SizeAllocate(new(0, 0, (int)416, (int)240));
		}
		//UpdateVideoStretch();
	}

	public void Stop()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			Console.WriteLine($"Stop");
			_mediaPlayer.Stop();
			_videoView.Visible = false;
		}
	}

	public void Pause()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			Console.WriteLine($"Pause");
			_mediaPlayer.Pause();
			_videoView.Visible = true;
		}
	}

	public void SetVolume(int volume)
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			Console.WriteLine($"SetVolume ({volume})");
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

	private int _isUpdating;
	private bool _updatedRequested;
	private void UpdateVideoStretch()
	{
		if (Interlocked.CompareExchange(ref _isUpdating, 1, 0) == 1)
		{
			_updatedRequested = true;
			return;
		}

		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			try
			{
				if (_videoView != null &&
			_mediaPlayer != null &&
			_mediaPlayer.Media != null &&
			_videoContainer is not null)
				{
					if (this.ActualHeight <= 0 || this.ActualWidth <= 0)
					{
						return;
					}


					var width = _videoView.AllocatedWidth;
					var height = _videoView.AllocatedHeight;
					var parentRatio = (double)width / global::System.Math.Max(1, height);

					var playerRatio = (double)this.ActualHeight / (double)this.ActualWidth;


					_mediaPlayer.Media.Parse(MediaParseOptions.ParseNetwork);
					while (!_mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
					{
						Thread.Sleep(100);
					}
					// Obtém a faixa de vídeo
					var videoTrack = _mediaPlayer.Media.Tracks.FirstOrDefault(track => track.TrackType == TrackType.Video);

					if (_mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
					{

						var videoSettings = videoTrack.Data.Video;
						var videoWidth = videoSettings.Width;
						var videoHeight = videoSettings.Height;

						if (videoWidth == 0 || videoHeight == 0)
						{
							return;
						}
						var videoRatio = (double)videoHeight / (double)videoWidth;

						var newHeight = (videoRatio > playerRatio) ? this.ActualHeight : this.ActualWidth * videoRatio;
						var newWidth = (videoRatio > playerRatio) ? this.ActualHeight / videoRatio : this.ActualWidth;


						var root = (_videoContainer.XamlRoot?.Content as UIElement)!;

						var topInset = (this.ActualHeight - newHeight) / 2;
						var leftInset = (this.ActualWidth - newWidth) / 2;
						Point pagePosition = this.TransformToVisual(root).TransformPoint(new Point(leftInset, topInset));

						_videoView?.SizeAllocate(new((int)pagePosition.X, (int)pagePosition.Y, (int)newWidth, (int)newHeight));
						_videoView?.SetNaturalSize((int)newHeight, (int)newWidth);

						Console.WriteLine($"Largura: {width},  Altura: {height}");
					}
				}
			}
			finally
			{
				Interlocked.Exchange(ref _isUpdating, 0);
				if (_updatedRequested)
				{
					_updatedRequested = false;
					UpdateVideoStretch();
				}
			}
		});
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var result = base.ArrangeOverride(finalSize);
		UpdateVideoStretch();

		return result;
	}

	//internal void UpdateVideoStretch()
	//{
	//	Console.WriteLine("UpdateVideoStretch");
	//	//_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
	//	//{
	//	if (_videoView != null && _mediaPlayer != null && _mediaPlayer.Media != null)
	//	{
	//		try
	//		{
	//			_mediaPlayer.Media.Parse(MediaParseOptions.ParseNetwork);
	//			var width = _videoView.AllocatedWidth;
	//			var height = _videoView.AllocatedHeight;
	//			// var parentRatio = (double)width / global::System.Math.Max(1, height);

	//			Console.WriteLine($"MediaPlayer {width} x {width}");
	//			while (_mediaPlayer.Media.Tracks != null && !_mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
	//			{
	//				Thread.Sleep(100);
	//			}


	//			if (_mediaPlayer.Media.Tracks != null && _mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
	//			{
	//				var videoTrack = _mediaPlayer.Media.Tracks.FirstOrDefault(track => track.TrackType == TrackType.Video);
	//				var videoSettings = videoTrack.Data.Video;
	//				var videoWidth = videoSettings.Width;
	//				var videoHeight = videoSettings.Height;

	//				if (videoWidth == 0 || videoHeight == 0)
	//				{
	//					return;
	//				}
	//				if (videoWidth == 0 || videoHeight == 0)
	//				{
	//					return;
	//				}
	//				if (width == 1)
	//				{
	//					width = (int)videoWidth;
	//				}

	//				Ratio = (double)videoWidth / global::System.Math.Max(1, videoHeight);
	//				height = (int)(videoWidth / Ratio);

	//				Console.WriteLine($"Video Ratio {Ratio}");
	//				Console.WriteLine($"VideoView SizeAllocate after Video Ratio {width} x {width}");
	//				_videoView?.SizeAllocate(new(100, 100, (int)width, (int)height));

	//				Console.WriteLine($"After VideoView SizeAllocate: {width},  Altura: {height}");

	//				if (_videoView != null)
	//				{
	//					_videoView.Visible = true;
	//				}
	//			}
	//		}
	//		finally
	//		{
	//		}
	//	}
	//	//});
	//}


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
