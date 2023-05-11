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
using Pango;
using Uno.UI.Extensions;
using Windows.Media.Playback;

namespace Uno.UI.Media;

public partial class GTKMediaPlayer : Border
{
	private LibVLC? _libvlc;
	private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
	private ContentControl? _videoContainer;
	private VideoView? _videoView;
	private Uri? _mediaPath;
	private double _ratio;
	private bool _isEnding;
	Windows.UI.Xaml.Media.Stretch _stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
	private Rect _transportControlsBounds;
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
		}
	}

	public void IsCompactChange()
	{
		UpdateVideoStretch();
	}

	public void ExitFullScreen()
	{
		if (_mediaPlayer != null)
		{
			Console.WriteLine($"ExitFullScreen");
			//_mediaPlayer.Fullscreen = false;
		}
	}

	public void RequestFullScreen()
	{
		if (_mediaPlayer != null)
		{
			Console.WriteLine($"RequestFullScreen");
			//_mediaPlayer.Fullscreen = true;
		}
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
	public void Mute(bool IsMuted)
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			Console.WriteLine($"Mute {IsMuted}");
			_mediaPlayer.Mute = IsMuted;
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
	//private Gtk.Window? _fullScreenWindow;
	private void UpdateVideoStretch()
	{
		//if (_mediaPlayer?.Fullscreen == true && _fullScreenWindow == null)
		//{
		//	_fullScreenWindow = new Gtk.Window(Gtk.WindowType.Popup)
		//	{
		//		Decorated = false,
		//		Resizable = false,
		//		WindowPosition = Gtk.WindowPosition.Center,
		//		TypeHint = Gdk.WindowTypeHint.Dialog
		//	};

		//	_fullScreenWindow.Add(_videoView);
		//	_fullScreenWindow.ShowAll();

		//	return;
		//}
		//if (_mediaPlayer?.Fullscreen != true && _fullScreenWindow != null)
		//{
		//	_fullScreenWindow = null;
		//}
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
						_mediaPlayer.Media.Mrl != null &&
						_videoContainer is not null)
				{
					if (this.ActualHeight <= 0 || this.ActualWidth <= 0)
					{
						return;
					}

					//UpdateTransportControlHeight();
					var playerHeight = (double)this.ActualHeight - _transportControlsBounds.Height;
					var playerWidth = (double)this.ActualWidth;
					var playerRatio = playerHeight / playerWidth;

					_mediaPlayer.Media.Parse(MediaParseOptions.ParseNetwork);
					while (!_mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
					{
						Thread.Sleep(100);
					}

					var videoTrack = _mediaPlayer.Media.Tracks.FirstOrDefault(track => track.TrackType == TrackType.Video);

					if (_mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
					{

						var root = (_videoContainer.XamlRoot?.Content as UIElement)!;
						var videoSettings = videoTrack.Data.Video;
						var videoWidth = videoSettings.Width;
						var videoHeight = videoSettings.Height;

						// From: https://github.com/videolan/libvlcsharp/blob/bca0a53fe921e6f1f745e4e3ac83a7bd3b2e4a9d/src/LibVLCSharp/Shared/MediaPlayerElement/AspectRatioManager.cs#L188
						videoWidth = videoWidth * videoSettings.SarNum / videoSettings.SarDen;

						if (videoWidth == 0 || videoHeight == 0)
						{
							return;
						}
						var videoRatio = (double)videoHeight / (double)videoWidth;

						var newHeight = (int)((videoRatio > playerRatio) ? playerHeight : playerWidth * Math.Round(videoRatio, 4));
						var newWidth = (int)((videoRatio > playerRatio) ? (playerHeight / videoRatio) : playerWidth);


						if (_videoView != null)
						{
							switch (_stretch)
							{
								case Windows.UI.Xaml.Media.Stretch.None:
									break;
								case Windows.UI.Xaml.Media.Stretch.Uniform:

									var topInsetUniform = (playerHeight - newHeight) / 2;
									var leftInsetUniform = (playerWidth - newWidth) / 2;

									_mediaPlayer.CropGeometry = null;

									Point pagePosition = this.TransformToVisual(root).TransformPoint(new Point(leftInsetUniform, topInsetUniform));
									_videoView?.SizeAllocate(new((int)pagePosition.X, (int)pagePosition.Y, newWidth, newHeight));
									Console.WriteLine($"Uniform Stretch Width: {newWidth},  Height: {newHeight}");

									break;
								case Windows.UI.Xaml.Media.Stretch.UniformToFill:

									//newHeight = (int)(newWidth * 9.0 / 16.0);
									//var topInsetUniformToFill = (playerHeight - newHeight) / 2;
									//var leftInsetUniformToFill = (playerWidth - newWidth) / 2;

									//Point pagePositionUniformToFill = this.TransformToVisual(root).TransformPoint(new Point(leftInsetUniformToFill, topInsetUniformToFill));
									//_videoView?.SizeAllocate(new((int)pagePositionUniformToFill.X, (int)pagePositionUniformToFill.Y, newWidth, newHeight));
									//Console.WriteLine($"UniformToFill Stretch Width: {newWidth},  Height: {newHeight}");

									//	break;
									//case Windows.UI.Xaml.Media.Stretch.Fill:

									var topInsetFill = (playerHeight - newHeight) / 2;
									var leftInsetFill = 0;

									var newHeightFill = (int)(videoHeight * playerRatio);
									var newWidthFill = (int)(videoWidth * playerRatio);
									double correctVideoRate = (videoRatio / playerRatio);

									Point pagePositionFill = this.TransformToVisual(root).TransformPoint(new Point(leftInsetFill, topInsetFill));
									_videoView?.SizeAllocate(new((int)pagePositionFill.X, (int)pagePositionFill.Y, (int)playerWidth, (int)playerHeight));
									_mediaPlayer.CropGeometry = $"{(int)(newWidthFill)}:{(int)(newHeightFill / correctVideoRate)}";

									break;
							}
						}
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
	//protected void UpdateTransportControlHeight()
	//{
	//	UIElement? container = VisualTreeHelper.GetParent(_videoContainer) as UIElement;
	//	ContentPresenter? ContentPresenter = VisualTreeHelper.GetParent(container) as ContentPresenter;
	//	ContentControl? ContentControl = VisualTreeHelper.GetParent(ContentPresenter) as ContentControl;
	//	MediaPlayerPresenter? MediaPlayerPresenter = VisualTreeHelper.GetParent(ContentControl) as MediaPlayerPresenter;
	//	UIElement? MediaPlayerPresenter2 = VisualTreeHelper.GetParent(MediaPlayerPresenter) as UIElement;

	//	if (MediaPlayerPresenter2 != null)
	//	{
	//		foreach (var child in MediaPlayerPresenter2.GetChildren())
	//		{
	//			if (child is ContentPresenter)
	//			{
	//				_transportControlAdjust = (int)child.DesiredSize.Height;
	//			}
	//		}
	//	}
	//}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var result = base.ArrangeOverride(finalSize);
		UpdateVideoStretch();

		return result;
	}

	private double _playbackRate;
	public double PlaybackRate
	{
		get => _playbackRate;
		set
		{
			_mediaPlayer?.SetRate((float)value);
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
			return _mediaPlayer.Time / 1000;
		}
		set
		{
			if (_videoView != null
					&& _mediaPlayer != null)
			{
				_mediaPlayer.Time = (long)value;
			}
		}
	}
	private bool _isLoopingEnabled;
	public void SetIsLoopingEnabled(bool value)
	{
		_isLoopingEnabled = value;
	}
	internal void UpdateVideoStretch(Windows.UI.Xaml.Media.Stretch stretch)
	{
		if (_videoView != null)
		{
			_stretch = stretch;
			UpdateVideoStretch();
		}
	}

	internal void SetTransportControlsBounds(Rect bounds)
	{
		if (!_transportControlsBounds.Equals(bounds))
		{
			_transportControlsBounds = bounds;
			UpdateVideoStretch();
		}
	}

}
