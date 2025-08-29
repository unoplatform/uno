using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Media.Playback;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.MediaPlayer.Skia.Win32;

public class Win32MediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private const string WindowClassName = "UnoPlatformVLCWindow";

	// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private static readonly WNDCLASSEXW _windowClass;

	// This is necessary to be able to direct the very first WndProc call of a window to the correct window wrapper.
	// That first call is inside CreateWindow, so we don't have a HWND yet. The alternative would be to create a new
	// window class (and a new WndProc) per window, but that sounds excessive.
	private static WeakReference<Win32MediaPlayerPresenterExtension>? _extForNextCreateWindow;
	private static readonly Dictionary<HWND, WeakReference<Win32MediaPlayerPresenterExtension>> _hwndToExtension = new();

	private readonly MediaPlayerPresenter _presenter;
	private SharedMediaPlayerExtension? _playerExtension;
	private readonly HWND _hwnd;

	static unsafe Win32MediaPlayerPresenterExtension()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = &WndProc,
			hInstance = Win32Helper.GetHInstance(),
			lpszClassName = lpClassName,
			style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-class-styles
		};

		var classAtom = PInvoke.RegisterClassEx(_windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	public unsafe Win32MediaPlayerPresenterExtension(MediaPlayerPresenter presenter)
	{
		_presenter = presenter;

		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_extForNextCreateWindow = new WeakReference<Win32MediaPlayerPresenterExtension>(this);
		_hwnd = PInvoke.CreateWindowEx(
			0,
			lpClassName,
			new PCWSTR(),
			0,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			PInvoke.CW_USEDEFAULT,
			HWND.Null,
			HMENU.Null,
			Win32Helper.GetHInstance(),
			null);
		_extForNextCreateWindow = null;

		if (_hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
		{
			this.Log().Trace("Created media player window.");
		}

		var success = PInvoke.RegisterTouchWindow(_hwnd, 0);
		if (!success && this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
		{
			this.Log().Error($"{nameof(PInvoke.RegisterTouchWindow)} failed: {Win32Helper.GetErrorMessage()}");
		}

		var cp = new ContentPresenter { Content = new Win32NativeWindow(_hwnd) };
		_presenter.Child = cp;
		cp.SizeChanged += (_, _) => StretchChanged();
	}

	~Win32MediaPlayerPresenterExtension()
	{
		_presenter.DispatcherQueue.TryEnqueue(() =>
		{
			var success = PInvoke.DestroyWindow(_hwnd);
			if (!success && this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.DestroyWindow)} failed: {Win32Helper.GetErrorMessage()}");
			}

			lock (_hwndToExtension)
			{
				_hwndToExtension.Remove(_hwnd);
			}
		});
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		lock (_hwndToExtension)
		{
			if (_extForNextCreateWindow is { } ext)
			{
				_hwndToExtension[hwnd] = _extForNextCreateWindow;
			}
			else if (!_hwndToExtension.TryGetValue(hwnd, out ext))
			{
				throw new Exception($"{nameof(WndProc)} was fired on a {nameof(HWND)} before it was added to, or after it was removed from, {nameof(_hwndToExtension)}.");
			}

			if (ext.TryGetTarget(out var target))
			{
				return target.WndProcInner(hwnd, msg, wParam, lParam);
			}
			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}
	}

	private LRESULT WndProcInner(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		Debug.Assert(_hwnd == HWND.Null || hwnd == _hwnd); // the null check is for when this method gets called inside CreateWindow before setting _hwnd

		switch (msg)
		{
			case PInvoke.WM_POINTERDOWN or PInvoke.WM_POINTERUP or PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL
				or PInvoke.WM_POINTERENTER or PInvoke.WM_POINTERLEAVE or PInvoke.WM_POINTERUPDATE:
				((Win32WindowWrapper)XamlRootMap.GetHostForRoot(_presenter.XamlRoot!)!).OnPointer(msg, wParam, _hwnd);
				return new LRESULT(0);
		}

		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
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
			_playerExtension.VlcPlayer.Hwnd = _hwnd;
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

	public void RequestFullScreen() { /* No need to do anything. */ }
	public void ExitFullScreen() { /* No need to do anything. */ }

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
