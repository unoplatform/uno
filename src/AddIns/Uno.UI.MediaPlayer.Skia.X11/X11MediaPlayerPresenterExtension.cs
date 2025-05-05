using System;
using System.Linq;
using Windows.Media.Playback;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Logging;
using Uno.UI.NativeElementHosting;
using Uno.UI.Runtime.Skia;
using Uno.WinUI.Runtime.Skia.X11;

[assembly: ApiExtension(
	typeof(IMediaPlayerPresenterExtension),
	typeof(Uno.UI.MediaPlayer.Skia.X11.X11MediaPlayerPresenterExtension),
	ownerType: typeof(MediaPlayerPresenter),
	operatingSystemCondition: "linux")]

namespace Uno.UI.MediaPlayer.Skia.X11;

public class X11MediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private readonly MediaPlayerPresenter _presenter;
	private SharedMediaPlayerExtension? _playerExtension;
	private readonly X11Window _x11Window;

	public X11MediaPlayerPresenterExtension(MediaPlayerPresenter presenter)
	{
		_presenter = presenter;

		IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);
		using var lockDisposable = X11Helper.XLock(display);

		if (display == IntPtr.Zero)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				this.Log().Error("XLIB ERROR: Cannot connect to X server");
			}
			throw new InvalidOperationException("XLIB ERROR: Cannot connect to X server");
		}

		int screen = XLib.XDefaultScreen(display);

		_ = XLib.XFlush(display); // unnecessary on most Xlib implementations

		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
		{
			this.Log().Trace("Created media player window.");
		}

		_x11Window = new X11Window(display, XLib.XCreateSimpleWindow(
			display,
			XLib.XRootWindow(display, screen),
			0,
			0,
			1,
			1,
			0,
			XLib.XBlackPixel(display, screen),
			XLib.XWhitePixel(display, screen)));

		// this is important because this display is different from the display used on the main thread,
		// so we need to make sure the window is actually created (not just queued to be created)
		// when we manipulate it using other display handles.
		_ = XLib.XFlush(display);
		XLib.XSync(display, true);

		var cp = new ContentPresenter() { Content = new X11NativeWindow(_x11Window.Window) };
		_presenter.Child = cp;
		cp.SizeChanged += (_, _) => StretchChanged();
	}

	~X11MediaPlayerPresenterExtension()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		_ = XLib.XDestroyWindow(_x11Window.Display, _x11Window.Window);
	}

	public void MediaPlayerChanged()
	{
		if (SharedMediaPlayerExtension.GetByMediaPlayer(_presenter.MediaPlayer) is { } extension)
		{
			if (_playerExtension is { })
			{
				_playerExtension.VlcPlayer.XWindow = 0;
				_playerExtension.IsVideoChanged -= OnExtensionOnIsVideoChanged;
				_playerExtension.Player.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
			}
			_playerExtension = extension;
			_playerExtension.VlcPlayer.XWindow = (uint)(_x11Window.Window);
			_playerExtension.IsVideoChanged += OnExtensionOnIsVideoChanged;
			_playerExtension.Player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
		}
	}

	private void OnExtensionOnIsVideoChanged(object? sender, bool? args)
	{
		_presenter.Child.Visibility = args ?? false ? Visibility.Visible : Visibility.Collapsed;
		if (args ?? false)
		{
			StretchChanged();
		}
	}

	private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		// this is needed because if you set the AspectRatio too early it won't take effect.
		StretchChanged();
	}

	// Native support for this was added in libvlc 4, the release of which has been getting delayed for half a decade at this point
	// https://code.videolan.org/videolan/vlc/-/merge_requests/5239
	public void StretchChanged()
	{
		if (_playerExtension?.VlcPlayer is { } vlcPlayer)
		{
			switch (_presenter.Stretch)
			{
				case Stretch.None:
					vlcPlayer.Scale = 1;
					vlcPlayer.AspectRatio = null;
					break;
				case Stretch.Fill:
					vlcPlayer.Scale = 0;
					vlcPlayer.AspectRatio = $"{_presenter.ActualWidth}:{_presenter.ActualHeight}";
					break;
				case Stretch.UniformToFill:
					vlcPlayer.Scale = 0;
					vlcPlayer.AspectRatio = null;
					break;
				case Stretch.Uniform:
					// https://code.videolan.org/videolan/LibVLCSharp/-/blob/3.x/src/LibVLCSharp/Shared/MediaPlayerElement/AspectRatioManager.cs
					{
						var videoTrack = _playerExtension?.VlcPlayer.Media?.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video;
						if (videoTrack == null)
						{
							break;
						}
						var track = (VideoTrack)videoTrack;
						var videoWidth = track.Width;
						var videoHeight = track.Height;
						if (videoWidth == 0 || videoHeight == 0)
						{
							vlcPlayer.Scale = 0;
						}
						else
						{
							if (track.SarNum != track.SarDen)
							{
								videoWidth = videoWidth * track.SarNum / track.SarDen;
							}

							var ar = videoWidth / (double)videoHeight;
							var videoViewWidth = _presenter.ActualWidth;
							var videoViewHeight = _presenter.ActualHeight;
							var dar = videoViewWidth / videoViewHeight;

							var rawPixelsPerViewPixel = XamlRoot.GetDisplayInformation(_presenter.XamlRoot).RawPixelsPerViewPixel;
							var displayWidth = videoViewWidth * rawPixelsPerViewPixel;
							var displayHeight = videoViewHeight * rawPixelsPerViewPixel;
							vlcPlayer.Scale = (float)(dar >= ar ? (displayWidth / videoWidth) : (displayHeight / videoHeight));
						}
						vlcPlayer.AspectRatio = null;
					}
					break;
			}
		}
	}

	public void RequestFullScreen()
	{
		// TODO
	}

	public void ExitFullScreen()
	{
		// TODO
	}

	public void RequestCompactOverlay()
	{
		// TODO
	}

	public void ExitCompactOverlay()
	{
		// TODO
	}

	public uint NaturalVideoHeight
		=> _playerExtension?.VlcPlayer.Media?.Tracks
			.FirstOrDefault(t => t.TrackType == TrackType.Video)
			.Data.Video.Height ?? 0;

	public uint NaturalVideoWidth
			=> _playerExtension?.VlcPlayer.Media?.Tracks
				.FirstOrDefault(t => t.TrackType == TrackType.Video)
				.Data.Video.Width ?? 0;
}
