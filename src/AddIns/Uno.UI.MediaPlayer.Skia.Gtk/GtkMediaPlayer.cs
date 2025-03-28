#nullable enable

using Windows.UI.Core;
using LibVLCSharp.Shared;
using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml;
using System.Threading;
using System.Linq;
using System.Collections.Immutable;
using System.IO;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Windows.Foundation;
using Point = Windows.Foundation.Point;
using Windows.Media.Playback;
using Uno.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Media;

public partial class GtkMediaPlayer : FrameworkElement
{
	private LibVLC? _libvlc;
	private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
	private ContentPresenter? _videoContainer;
	private VideoView? _videoView;
	private Uri? _mediaPath;
	private bool _isEnding;
	private double _playbackRate;
	private Rect _transportControlsBounds;
	private Windows.UI.Xaml.Media.Stretch _stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
	private readonly MediaPlayerPresenter _owner;
	private (double playerHeight, double playerWidth, uint videoHeight, uint videoWidth) _lastSetDimensions;

	public GtkMediaPlayer(MediaPlayerPresenter owner)
	{
		_owner = owner;

		Loaded += GtkMediaPlayer_Loaded;
		Unloaded += GtkMediaPlayer_Unloaded;
		LayoutUpdated += GtkMediaPlayer_LayoutUpdated;

		_ = Initialize();
	}

	~GtkMediaPlayer()
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug("Collecting GtkMediaPlayer");
		}

		GLib.Idle.Add(() =>
		{
			_videoView?.Dispose();
			return false;
		});

		// Freeing those resources currently crash the app with an access violation exception.
		//_mediaPlayer?.Dispose();
		//_libvlc?.Dispose();
	}

	internal uint NaturalVideoHeight => _lastSetDimensions.videoHeight;
	internal uint NaturalVideoWidth => _lastSetDimensions.videoWidth;

	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		"Source", typeof(string), typeof(GtkMediaPlayer), new PropertyMetadata(default(string),
			OnSourceChanged));

	public double Duration { get; set; }

	public double VideoRatio { get; set; }

	public bool IsVideo
		=> _mediaPlayer?.Media?.Tracks?.Any(x => x.TrackType == TrackType.Video) == true;

	public bool IsAudio
		=> _mediaPlayer?.Media?.Tracks?.Any(x => x.TrackType == TrackType.Video) == true;

	public void Play()
	{
		if (EnsureMediaPlayerAndVideoView())
		{
			if (PlatformHelper.IsMac)
			{
				VideoView.ReportMacOSNotSupported();
				return;
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Play");
			}
			_mediaPlayer.Play();

			_videoView.SetVisible(true);
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to Play, the player is not ready yet");
			}
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
				}
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
				}
			}
		}
	}

	public void IsCompactChange()
	{
		UpdateVideoStretch();
	}

	public void ExitFullScreen()
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug("ExitFullScreen");
		}
	}

	public void RequestFullScreen()
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug("RequestFullScreen");
		}

	}

	public void Stop()
	{
		if (EnsureMediaPlayerAndVideoView())
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Stop");
			}

			_mediaPlayer.Stop();
			_videoView.SetVisible(false);
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to stop, the player is not ready yet");
			}
		}
	}

	public void Pause()
	{
		if (EnsureMediaPlayerAndVideoView())
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Pause");
			}

			_mediaPlayer.Pause();
			_videoView.SetVisible(true);
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to pause, the player is not ready yet");
			}
		}
	}

	public void SetVolume(int volume)
	{
		if (EnsureMediaPlayerAndVideoView())
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"SetVolume ({volume})");
			}

			_mediaPlayer.Volume = volume;
			_videoView.SetVisible(true);
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to set the volume, the player is not ready yet");
			}
		}
	}

	public void Mute(bool IsMuted)
	{
		if (EnsureMediaPlayerAndVideoView())
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Mute {IsMuted}");
			}
			_mediaPlayer.Mute = IsMuted;
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to mute, the player is not ready yet");
			}
		}
	}

	private void GtkMediaPlayer_LayoutUpdated(object? sender, object e)
		=> UpdateVideoStretch();

	private bool TryGetVideoDetails(
		[NotNullWhen(true)] out uint? videoWidth,
		[NotNullWhen(true)] out uint? videoHeight,
		[NotNullWhen(true)] out VideoTrack? videoTrack)
	{
		if (_mediaPlayer is not null
			&& _mediaPlayer.Media is not null)
		{
			var mediaTrack = _mediaPlayer
				.Media
				.Tracks
				.FirstOrDefault(track => track.TrackType == TrackType.Video);

			if (mediaTrack is { })
			{
				videoTrack = mediaTrack.Data.Video;
				videoWidth = videoTrack.Value.Width;
				videoHeight = videoTrack.Value.Height;

				if (videoTrack.Value.SarDen != 0)
				{
					return true;
				}
			}
		}

		videoWidth = null;
		videoHeight = null;
		videoTrack = null;

		return false;
	}

	private void UpdateVideoStretch(bool forceVideoViewVisibility = false)
	{
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
		{
			if (_videoView != null
				&& _mediaPlayer?.Media?.Mrl is not null
				&& _videoContainer is not null)
			{

				await _mediaPlayer.Media.Parse(MediaParseOptions.ParseNetwork);

				if (TryGetVideoDetails(out var videoWidth, out var videoHeight, out var videoSettings))
				{
					// From: https://github.com/videolan/libvlcsharp/blob/bca0a53fe921e6f1f745e4e3ac83a7bd3b2e4a9d/src/LibVLCSharp/Shared/MediaPlayerElement/AspectRatioManager.cs#L188
					videoWidth = videoWidth * videoSettings.Value.SarNum / videoSettings.Value.SarDen;

					// Update video ratio first, so that the cover can be
					// removed properly without the mediaplayerpresenter to be
					// visible.
					if (videoWidth == 0 || videoHeight == 0)
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"Source video size is not valid ({videoWidth}x{videoHeight})");
						}

						return;
					}

					VideoRatio = (double)videoHeight / (double)videoWidth;
					if (videoHeight != _lastSetDimensions.videoHeight || videoWidth != _lastSetDimensions.videoWidth)
					{
						OnNaturalVideoDimensionChanged?.Invoke();
					}

					var currentSize = new Size(ActualWidth, ActualHeight);

					// If the control is not visible, or not sized properly
					// we cannot display the video window.
					if (currentSize.Height <= 0 || currentSize.Width <= 0)
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"Skipping layout update for empty layout slot");
						}

						return;
					}

					var playerHeight = (double)currentSize.Height - _transportControlsBounds.Height;
					var playerWidth = (double)currentSize.Width;

					UpdateVideoSizeAllocate(playerHeight, playerWidth, videoHeight.Value, videoWidth.Value);

					if (forceVideoViewVisibility)
					{
						_videoView?.SetVisible(true);
					}
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Skipping layout update because video details are not available");
					}
				}
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Skipping layout update because the player is not available");
				}
			}
		});
	}

	private void GtkMediaPlayer_Loaded(object sender, RoutedEventArgs e)
	{
		if (_videoContainer is not null)
		{
			_videoContainer.Content = _videoView;
		}
	}

	private void GtkMediaPlayer_Unloaded(object sender, RoutedEventArgs e)
	{
		if (_videoContainer is not null)
		{
			_videoContainer.Content = null;
		}
	}

	internal void UpdateVideoLayout() => UpdateVideoSizeAllocate(_lastSetDimensions.playerHeight, _lastSetDimensions.playerWidth, _lastSetDimensions.videoHeight, _lastSetDimensions.videoWidth);

	private void UpdateVideoSizeAllocate(double playerHeight, double playerWidth, uint videoHeight, uint videoWidth)
	{
		_lastSetDimensions = (playerHeight, playerWidth, videoHeight, videoWidth);

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

					if (_videoView is not null)
					{
						_videoView.Arrange(new((int)pagePosition.X, (int)pagePosition.Y, newWidth, newHeight));
					}

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"UpdateVideoSizeAllocate Uniform ({pagePosition}, {newWidth}x{newHeight})");
					}
					break;

				case Windows.UI.Xaml.Media.Stretch.UniformToFill:

					var topInsetFill = (playerHeight - newHeight) / 2;
					var leftInsetFill = 0;

					if (_videoView is not null)
					{
						Point pagePositionFill = this.TransformToVisual(root).TransformPoint(new Point(leftInsetFill, topInsetFill));

						if (_videoView is not null)
						{
							_videoView.Arrange(new((int)pagePositionFill.X, (int)pagePositionFill.Y, (int)playerWidth, (int)playerHeight));
						}

						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"UpdateVideoSizeAllocate UniformToFill ({pagePositionFill}, {newWidth}x{newHeight})");
						}
					}

					break;
			}
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var result = ArrangeFirstChild(finalSize);
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
		get => _mediaPlayer?.Time / 1000 ?? 0;
		set
		{
			if (_mediaPlayer is not null)
			{
				_mediaPlayer.Time = (long)value;
			}
		}
	}

	internal void UpdateVideoStretch(Windows.UI.Xaml.Media.Stretch stretch)
	{
		if (_videoView != null)
		{
			_stretch = stretch;
			UpdateVideoStretch();
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to set stretch, the player is not ready yet");
			}
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

	[MemberNotNullWhen(true, nameof(_videoView), nameof(_mediaPlayer))]
	private bool EnsureMediaPlayerAndVideoView()
		=> _videoView is not null && _mediaPlayer is not null;

	public void RequestCompactOverlay()
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"RequestPictureInPicture()");
		}
	}

	public void ExitCompactOverlay()
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"ExitPictureInPicture()");
		}
	}
}
