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
using Microsoft.Extensions.Logging;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Point = Windows.Foundation.Point;
using Pango;
using Uno.UI.Extensions;
using Windows.Media.Playback;
using Uno.Logging;
using Windows.UI.ViewManagement;
using Windows.Media.Core;

namespace Uno.UI.Media;

public partial class GtkMediaPlayer : Border
{
	private LibVLC? _libvlc;
	private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
	private ContentControl? _videoContainer;
	private VideoView? _videoView;
	private Uri? _mediaPath;
	private bool _isEnding;
	private bool _isPlaying;
	private int _isUpdating;
	private bool _updatedRequested;
	private double _playbackRate;
	private bool _isLoopingEnabled;
	private long _currentPositionBeforeFullscreenChange;
	private Rect _transportControlsBounds;
	private readonly ImmutableArray<string> audioTagAllowedFormats = ImmutableArray.Create(".MP3", ".WAV");
	private readonly ImmutableArray<string> videoTagAllowedFormats = ImmutableArray.Create(".MP4", ".WEBM", ".OGG");
	public double Duration { get; set; }
	public double VideoRatio { get; set; }
	public bool IsVideo => videoTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	public bool IsAudio => audioTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	Windows.UI.Xaml.Media.Stretch _stretch = Windows.UI.Xaml.Media.Stretch.Uniform;

	public GtkMediaPlayer()
	{
		_ = Initialized();
	}

	public void Play()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Play");
			}
			_mediaPlayer.Play();
			_videoView.Visible = true;
		}
	}

	public MediaPlayerState CurrentState
	{
		get
		{
			if (_mediaPlayer != null)
			{
				switch (_mediaPlayer.State)
				{
					case VLCState.Opening:
						return MediaPlayerState.Opening;
					case VLCState.Buffering:
						return MediaPlayerState.Buffering;
					case VLCState.Playing:
						return MediaPlayerState.Playing;
					case VLCState.Paused:
						return MediaPlayerState.Paused;
					case VLCState.Stopped:
						return MediaPlayerState.Stopped;
					case VLCState.Ended:
					case VLCState.Error:
						return MediaPlayerState.Closed;
					case VLCState.NothingSpecial:
					default:
						return default(MediaPlayerState);
				};
			}
			return default(MediaPlayerState);
		}
		internal set
		{
			if (_mediaPlayer != null)
			{
				switch (value)
				{
					case MediaPlayerState.Closed:
						_mediaPlayer.Stop();
						break;
					case MediaPlayerState.Playing:
						_mediaPlayer.Play();
						break;
					case MediaPlayerState.Paused:
						_mediaPlayer.Pause();
						break;
					case MediaPlayerState.Stopped:
						_mediaPlayer.Stop();
						break;
					case MediaPlayerState.Opening:
						break;
					case MediaPlayerState.Buffering:
						break;
				};
			}
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
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("ExitFullScreen");
			}
			_isPlaying = _mediaPlayer.State == VLCState.Playing;
			_currentPositionBeforeFullscreenChange = _mediaPlayer.Time;
			if (_videoView != null && _mediaPlayer != null)
			{
				Stop();
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					await Initialized();
					UpdateMedia();
					_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						UpdateVideoStretch();
						Play();
						Pause();
						CurrentPosition = (double)_currentPositionBeforeFullscreenChange;
						if (_isPlaying)
						{
							Play();
						}
					});
				});
			}
		}
	}

	public void RequestFullScreen()
	{
		if (_mediaPlayer != null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("RequestFullScreen");
			}
			_isPlaying = _mediaPlayer.State == VLCState.Playing;
			_currentPositionBeforeFullscreenChange = _mediaPlayer.Time;

			if (_videoView != null && _mediaPlayer != null)
			{
				Stop();
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					await Initialized();
					UpdateMedia();
					_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						UpdateVideoStretch();
						Play();
						Pause();
						CurrentPosition = (double)_currentPositionBeforeFullscreenChange;
						if (_isPlaying)
						{
							Play();
						}
					});
				});
			}
		}
	}

	public void Stop()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Stop");
			}
			_mediaPlayer.Stop();
			_videoView.Visible = false;
		}
	}

	public void Pause()
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Pause");
			}
			_mediaPlayer.Pause();
			_videoView.Visible = true;
		}
	}

	public void SetVolume(int volume)
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"SetVolume ({volume})");
			}
			_mediaPlayer.Volume = volume;
			_videoView.Visible = true;
		}
	}

	public void Mute(bool IsMuted)
	{
		if (_videoView != null && _mediaPlayer != null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Mute {IsMuted}");
			}
			_mediaPlayer.Mute = IsMuted;
		}
	}

	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		"Source", typeof(string), typeof(GtkMediaPlayer), new PropertyMetadata(default(string),
			OnSourceChanged));

	private void UpdateVideoStretch()
	{
		if (Interlocked.CompareExchange(ref _isUpdating, 1, 0) == 1)
		{
			_updatedRequested = true;
			return;
		}
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			if (_videoView != null &&
					_mediaPlayer != null &&
					_mediaPlayer.Media != null &&
					_mediaPlayer.Media.Mrl != null &&
					_videoContainer is not null)
			{
				if (this.ActualHeight <= 0 || this.ActualWidth <= 0)
				{
					CheckUpdatedRequested();
					return;
				}

				var playerHeight = (double)this.ActualHeight - _transportControlsBounds.Height;
				var playerWidth = (double)this.ActualWidth;

				_mediaPlayer.Media.Parse(MediaParseOptions.ParseNetwork);

				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					if (_mediaPlayer != null && _mediaPlayer.Media != null)
					{
						var videoTrack = _mediaPlayer.Media.Tracks.FirstOrDefault(track => track.TrackType == TrackType.Video);
						if (_mediaPlayer.Media.Tracks.Any(track => track.TrackType == TrackType.Video))
						{
							var videoSettings = videoTrack.Data.Video;
							var videoWidth = videoSettings.Width;
							var videoHeight = videoSettings.Height;

							// From: https://github.com/videolan/libvlcsharp/blob/bca0a53fe921e6f1f745e4e3ac83a7bd3b2e4a9d/src/LibVLCSharp/Shared/MediaPlayerElement/AspectRatioManager.cs#L188
							videoWidth = videoWidth * videoSettings.SarNum / videoSettings.SarDen;

							UpdateVideoSizeAllocate(playerHeight, playerWidth, videoHeight, videoWidth);
						}
						else
						{
							CheckUpdatedRequested();
						}
					}
					else
					{
						CheckUpdatedRequested();
					}
				});
			}
			else
			{
				CheckUpdatedRequested();
			}
		});
	}
	private void CheckUpdatedRequested()
	{
		Interlocked.Exchange(ref _isUpdating, 0);
		if (_updatedRequested)
		{
			_updatedRequested = false;
			UpdateVideoStretch();
		}
	}
	private void UpdateVideoSizeAllocate(double playerHeight, double playerWidth, uint videoHeight, uint videoWidth)
	{
		try
		{
			if (_videoView != null && _mediaPlayer != null && _videoContainer != null)
			{
				if (videoWidth == 0 || videoHeight == 0)
				{
					return;
				}
				VideoRatio = (double)videoHeight / (double)videoWidth;
				var playerRatio = playerHeight / playerWidth;

				if (playerRatio == 0)
				{
					return;
				}
				var newHeight = (int)((VideoRatio > playerRatio) ? playerHeight : playerWidth * VideoRatio);
				var newWidth = (int)((VideoRatio > playerRatio) ? (playerHeight / VideoRatio) : playerWidth);
				var root = (_videoContainer.XamlRoot?.Content as UIElement)!;

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
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"Uniform Stretch Width: {newWidth},  Height: {newHeight}");
						}
						break;
					case Windows.UI.Xaml.Media.Stretch.UniformToFill:

						//newHeight = (int)(newWidth * 9.0 / 16.0);
						//var topInsetUniformToFill = (playerHeight - newHeight) / 2;
						//var leftInsetUniformToFill = (playerWidth - newWidth) / 2;

						//Point pagePositionUniformToFill = this.TransformToVisual(root).TransformPoint(new Point(leftInsetUniformToFill, topInsetUniformToFill));
						//_videoView?.SizeAllocate(new((int)pagePositionUniformToFill.X, (int)pagePositionUniformToFill.Y, newWidth, newHeight));

						//	break;
						//case Windows.UI.Xaml.Media.Stretch.Fill:

						var topInsetFill = (playerHeight - newHeight) / 2;
						var leftInsetFill = 0;

						var newHeightFill = (int)(videoHeight * playerRatio);
						var newWidthFill = (int)(videoWidth * playerRatio);
						double correctVideoRate = (VideoRatio / playerRatio);

						Point pagePositionFill = this.TransformToVisual(root).TransformPoint(new Point(leftInsetFill, topInsetFill));
						_videoView?.SizeAllocate(new((int)pagePositionFill.X, (int)pagePositionFill.Y, (int)playerWidth, (int)playerHeight));
						_mediaPlayer.CropGeometry = $"{(int)(newWidthFill)}:{(int)(newHeightFill / correctVideoRate)}";

						break;
				}
			}
		}
		finally
		{
			CheckUpdatedRequested();
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var result = base.ArrangeOverride(finalSize);
		UpdateVideoStretch();

		return result;
	}

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
