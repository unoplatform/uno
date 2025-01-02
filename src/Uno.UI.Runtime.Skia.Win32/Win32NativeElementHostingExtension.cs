using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Runtime.Skia.Win32;

public class Win32NativeElementHostingExtension(ContentPresenter presenter) : ContentPresenter.INativeElementHostingExtension
{
	private static readonly string SampleVideoLink = Path.Combine(Package.Current.InstalledPath, @"Assets\Videos\Getting_Started_with_Uno_Platform_for_Figma.mp4");
	private static readonly SKPath _emptyPath = new();
	private static readonly byte[] _emptyRegionBytes;

	private Rect _lastArrangeRect;
	private (byte[] bytes, SKPath path) _lastClip = (_emptyRegionBytes, _emptyPath);

	static unsafe Win32NativeElementHostingExtension()
	{
		var emptyRegion = PInvoke.CreateRectRgn(0, 0, 0, 0);
		var neededSize = PInvoke.GetRegionData(emptyRegion, 0);
		_emptyRegionBytes = new byte[neededSize];
		fixed (byte* ptr = _emptyRegionBytes)
		{
			_ = PInvoke.GetRegionData(emptyRegion, neededSize, (RGNDATA*)ptr);
		}
	}

	private HWND Hwnd
	{
		get
		{
			if (presenter.XamlRoot is null)
			{
				throw new InvalidOperationException($"{nameof(XamlRoot)} is null.");
			}

			if (presenter.XamlRoot.HostWindow is not { } window)
			{
				throw new InvalidOperationException($"{nameof(presenter)}.{nameof(XamlRoot)}.{nameof(XamlRoot.HostWindow)} is null.");
			}

			if (window.NativeWindow is not Win32NativeWindow nativeWindow)
			{
				throw new InvalidOperationException($"{nameof(window.NativeWindow)} is not a {nameof(Win32NativeWindow)} instance.");
			}

			return (HWND)nativeWindow.Hwnd;
		}
	}

	public bool IsNativeElement(object content) => content is Win32NativeWindow;

	public void AttachNativeElement(object content)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		// We don't need to remove the WS_EX_LAYERED flag on detaching since it only makes compositing order more restrictive
		// so if things were working without it, they will continue to work with it.
		var oldExStyleVal = PInvoke.GetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
		PInvoke.SetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, oldExStyleVal | (int)WINDOW_EX_STYLE.WS_EX_LAYERED);

		var res1 = PInvoke.SetParent((HWND)window.Hwnd, Hwnd);
		if (res1 == HWND.Null)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetParent)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		Win32WindowWrapper.XamlRootMap.GetHostForRoot(presenter.XamlRoot!)!.RenderingNegativePathChanged += OnRenderingNegativePathChanged;
	}

	private unsafe void OnRenderingNegativePathChanged(object? sender, SKPath path)
	{
		using var _1 = SkiaHelper.GetTempSKPath(out var arrangePath);
		using var _2 = SkiaHelper.GetTempSKPath(out var intersectionPath);

		arrangePath.AddRect(_lastArrangeRect.ToSKRect());
		path.Op(arrangePath, SKPathOp.Intersect, intersectionPath);
		intersectionPath.Transform(SKMatrix.CreateTranslation((float)-_lastArrangeRect.X, (float)-_lastArrangeRect.Y));
		intersectionPath.Simplify();

		if (intersectionPath.IsEmpty)
		{
			if (_lastClip.bytes != _emptyRegionBytes)
			{
				ArrayPool<byte>.Shared.Return(_lastClip.bytes);
			}
			_lastClip = (_emptyRegionBytes, _emptyPath);
			CloneAndSetWindowRgn();
			return;
		}

		using var _3 = SkiaHelper.GetTempSKPath(out var xorPath);
		if (_lastClip.path is { } lastPath && lastPath.Op(intersectionPath, SKPathOp.Xor, xorPath) && xorPath.IsEmpty)
		{
			CloneAndSetWindowRgn();
			return;
		}

		var rects = new List<SKRectI>();
		var region = new SKRegion(intersectionPath);
		for (var iter = region.CreateRectIterator(); iter.Next(out var rect);)
		{
			rects.Add(rect);
		}

		var bounds = region.Bounds;
		var bufferSize = Marshal.SizeOf<RGNDATAHEADER>() + rects.Count * Marshal.SizeOf<RECT>();
		var bytes = ArrayPool<byte>.Shared.Rent(bufferSize);
		fixed (byte* buffer = bytes)
		{
			var data = (RGNDATA*)buffer;
			data->rdh = new RGNDATAHEADER
			{
				dwSize = (uint)Marshal.SizeOf<RGNDATAHEADER>(),
				nCount = (uint)rects.Count,
				iType = PInvoke.RDH_RECTANGLES,
				nRgnSize = 0,
				rcBound = new RECT(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)
			};
			var rectSpan = new Span<RECT>(&data->Buffer, rects.Count);
			for (var i = 0; i < rects.Count; i++)
			{
				rectSpan[i] = new RECT(rects[i].Left, rects[i].Top, rects[i].Right, rects[i].Bottom);
			}

			var hrgn = PInvoke.ExtCreateRegion((XFORM*)null, (uint)bufferSize, data);
			if (hrgn == HRGN.Null)
			{
				this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.ExtCreateRegion)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}

			var pathCopy = new SKPath(); // Careful! new SKPath(intersectionPath) doesn't seem to be properly cloning
			pathCopy.AddPath(intersectionPath);
			if (_lastClip.bytes != _emptyRegionBytes)
			{
				ArrayPool<byte>.Shared.Return(_lastClip.bytes);
			}
			_lastClip = (bytes, pathCopy);
			CloneAndSetWindowRgn();
		}

		void CloneAndSetWindowRgn()
		{
			// "After a successful call to SetWindowRgn, the system owns the region specified by the region handle hRgn. The system does not make a copy of the region. Thus, you should not make any further function calls with this region handle. In particular, do not delete this region handle. The system deletes the region handle when it no longer needed."
			fixed (byte* ptr = _lastClip.bytes)
			{
				var cpy = PInvoke.ExtCreateRegion((XFORM*)null, (uint)_lastClip.bytes.Length, (RGNDATA*)ptr);
				_ = PInvoke.SetWindowRgn((HWND)((Win32NativeWindow)presenter.Content).Hwnd, cpy, true) != 0
					|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowRgn)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}

	public void DetachNativeElement(object content)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		_ = PInvoke.SetParent((HWND)window.Hwnd, HWND.Null) != HWND.Null
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetParent)} failed: {Win32Helper.GetErrorMessage()}");

		Win32WindowWrapper.XamlRootMap.GetHostForRoot(presenter.XamlRoot!)!.RenderingNegativePathChanged -= OnRenderingNegativePathChanged;

		if (_lastClip.bytes != _emptyRegionBytes)
		{
			ArrayPool<byte>.Shared.Return(_lastClip.bytes);
		}
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		_lastArrangeRect = arrangeRect;

		_ = PInvoke.SetWindowPos((HWND)window.Hwnd, HWND.Null, (int)arrangeRect.X, (int)arrangeRect.Y, (int)arrangeRect.Width, (int)arrangeRect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize) => availableSize;

	public object CreateSampleComponent(string text)
	{
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = @"C:\Program Files\VideoLAN\VLC\vlc.exe",
				UseShellExecute = false
			}
		};

		string title;
		title = $"Sample Video {Random.Shared.Next()} {text}"; // used to maintain unique titles
		process.StartInfo.ArgumentList.Add(SampleVideoLink);
		process.StartInfo.ArgumentList.Add("--meta-title");
		process.StartInfo.ArgumentList.Add(title);
		title += " - VLC media player";

		process.Start();

		HWND hwnd = default;
		var success = SpinWait.SpinUntil(() =>
		{
			hwnd = PInvoke.FindWindow(null, title);
			return hwnd != HWND.Null;
		}, TimeSpan.FromSeconds(5));

		if (!success)
		{
			throw new InvalidOperationException("Could not find the HWND spawned by the created process.");
		}

		return new Win32NativeWindow(hwnd);
	}

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		// no need to do anything here, airspace clipping logic will take care of it automatically
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		_ = PInvoke.SetLayeredWindowAttributes((HWND)window.Hwnd, new COLORREF(0), (byte)Math.Round(opacity * 255), LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetLayeredWindowAttributes)} failed: {Win32Helper.GetErrorMessage()}");
	}
}
