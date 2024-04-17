using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Uno.WinUI.Runtime.Skia.X11;

public class X11NativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
#pragma warning disable CS0414 // Field is assigned but its value is never used
	private static string SampleVideoLink = "https://uno-assets.platform.uno/tests/uno/big_buck_bunny_720p_5mb.mp4";
#pragma warning restore CS0414 // Field is assigned but its value is never used

	public bool IsNativeElement(object content)
	{
		if (content is not X11Window x11Window)
		{
			return false;
		}

		using var _1 = X11Helper.XLock(x11Window.Display);

		var _3 = XLib.XQueryTree(x11Window.Display, XLib.XDefaultRootWindow(x11Window.Display), out IntPtr root, out _, out var children, out _);
		XLib.XFree(children);

		// _NET_CLIENT_LIST only identifies top-level windows, not subwindows.
		var status = XLib.XGetWindowProperty(
			x11Window.Display,
			root,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_CLIENT_LIST),
			0,
			new IntPtr(0x7fffffff),
			false,
			X11Helper.AnyPropertyType,
			out _,
			out _,
			out var length,
			out IntPtr _,
			out IntPtr windowArray);

		if (status == X11Helper.Success)
		{
			unsafe
			{
				var span = new Span<IntPtr>(windowArray.ToPointer(), (int)length);
				foreach (var window in span)
				{
					if (window == x11Window.Window)
					{
						return true;
					}
				}
			}
		}

		return FindWindowById(x11Window.Display, x11Window.Window, root) != IntPtr.Zero;
	}
	public void AttachNativeElement(XamlRoot owner, object content)
	{
		Debug.Assert(!IsNativeElementAttached(owner, content));
		if (content is X11Window x11Window
			&& X11Manager.XamlRootMap.GetHostForRoot(owner) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(x11Window.Display);

			// this seems to be necessary or else the WM will keep detaching the subwindow
			XWindowAttributes attributes = default;
			var _2 = XLib.XGetWindowAttributes(x11Window.Display, x11Window.Window, ref attributes);
			attributes.override_direct = /* True */ 1;

			unsafe
			{
				IntPtr attr = Marshal.AllocHGlobal(Marshal.SizeOf(attributes));
				Marshal.StructureToPtr(attributes, attr, false);
				X11Helper.XChangeWindowAttributes(x11Window.Display, x11Window.Window, (IntPtr)XCreateWindowFlags.CWOverrideRedirect, (XSetWindowAttributes*)attr.ToPointer());
				Marshal.FreeHGlobal(attr);
			}

			var _3 = X11Helper.XReparentWindow(x11Window.Display, x11Window.Window, host.X11Window.Window, 0, 0);
			XLib.XSync(x11Window.Display, false);
		}
		else
		{
			throw new InvalidOperationException($"{nameof(AttachNativeElement)} called in an invalid state.");
		}
	}
	public void DetachNativeElement(XamlRoot owner, object content)
	{
		Debug.Assert(IsNativeElementAttached(owner, content));
		if (content is X11Window x11Window)
		{
			using var _1 = X11Helper.XLock(x11Window.Display);
			var _2 = XLib.XQueryTree(x11Window.Display, x11Window.Window, out IntPtr root, out _, out var children, out _);
			XLib.XFree(children);
			var _3 = X11Helper.XReparentWindow(x11Window.Display, x11Window.Window, root, 0, 0);
			XLib.XSync(x11Window.Display, false);
		}
		else
		{
			 throw new InvalidOperationException($"{nameof(DetachNativeElement)} called in an invalid state.");
		}
	}
	public void ArrangeNativeElement(XamlRoot owner, object content, Rect arrangeRect, Rect? clipRect)
	{
		Debug.Assert(IsNativeElementAttached(owner, content));
		if (content is X11Window x11Window
			&& (int)arrangeRect.Width > 0 && (int)arrangeRect.Height > 0)
		{
			using var _1 = X11Helper.XLock(x11Window.Display);
			var _2 = XLib.XResizeWindow(x11Window.Display, x11Window.Window, (int)arrangeRect.Width, (int)arrangeRect.Height);
			var _3 = X11Helper.XMoveWindow(x11Window.Display, x11Window.Window, (int)arrangeRect.X, (int)arrangeRect.Y);
			XLib.XSync(x11Window.Display, false);
		}
		else
		{
			throw new InvalidOperationException($"{nameof(ArrangeNativeElement)} called in an invalid state.");
		}
	}
	public Size MeasureNativeElement(XamlRoot owner, object content, Size childMeasuredSize, Size availableSize)
	{
		return new Size(200, 200);
	}

	/// <summary>
	/// replace the executable and the args with whatever you have locally. This is only used
	/// for internal debugging. However, make sure that you can set a unique title to the window,
	/// so that you can then look it up.
	/// </summary>
	public object? CreateSampleComponent(XamlRoot owner, string text) {
		if (X11Manager.XamlRootMap.GetHostForRoot(owner) is X11XamlRootHost host)
		{
			var display = host.X11Window.Display;
			if (!Exists("mpv"))
			{
				return null;
			}

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					// FileName = "mpv",
					FileName = "xterm",
					UseShellExecute = false
				}
			};

			// var title = $"Sample Video {Random.Shared.Next()} {text}"; // used to maintain unique titles
			// process.StartInfo.ArgumentList.Add("--keep-open=always");
			// process.StartInfo.ArgumentList.Add($"--title={title}");
			// process.StartInfo.ArgumentList.Add(SampleVideoLink);

			var title = $"Sample terminal {Random.Shared.Next()} {text}"; // used to maintain unique titles
			process.StartInfo.ArgumentList.Add("-xrm");
			process.StartInfo.ArgumentList.Add("XTerm.vt100.allowTitleOps: false");
			process.StartInfo.ArgumentList.Add("-T");
			process.StartInfo.ArgumentList.Add(title);

			process.Start();

			using var _1 = X11Helper.XLock(display);

			var _2 = XLib.XQueryTree(display, host.X11Window.Window, out IntPtr root, out _, out var children, out _);
			XLib.XFree(children);

			// Wait for the window to open.
			IntPtr window = IntPtr.Zero;
			SpinWait.SpinUntil(() =>
			{
				window = FindWindowByTitle(display, root, title);
				if (window == IntPtr.Zero)
				{
					return false;
				}

				XWindowAttributes attributes = default;
				var _ = XLib.XGetWindowAttributes(display, window, ref attributes);
				return attributes.map_state == MapState.IsViewable;
			});

			if (window == IntPtr.Zero)
			{
				process.Kill();
				return null;
			}

			return new X11Window(display, window);
		}

		// For debugging: replace the above with a hardcoded window id, obtainable using e.g. wmctrl
		// if (owner is XamlRoot xamlRoot
		// 	&& X11Manager.XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		// {
		// 	return new X11Window(host.X11Window.Display, 0x04a00002);
		// }

		return null;
	}

	public bool IsNativeElementAttached(XamlRoot owner, object nativeElement)
	{
		if (nativeElement is X11Window x11Window
			&& X11Manager.XamlRootMap.GetHostForRoot(owner) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(x11Window.Display);
			var _2 = XLib.XQueryTree(x11Window.Display, x11Window.Window, out _, out IntPtr parent, out var children, out _);
			XLib.XFree(children);
			return parent == host.X11Window.Window;
		}

		return false;
	}
	public void ChangeNativeElementVisibility(XamlRoot owner, object content, bool visible)
	{
		if (content is X11Window x11Window)
		{
			if (visible)
			{
				var _3 = XLib.XMapWindow(x11Window.Display, x11Window.Window);
			}
			else
			{
				var _3 = X11Helper.XUnmapWindow(x11Window.Display, x11Window.Window);
			}
		}
	}

	// This doesn't seem to work as most (all?) WMs won't change the opacity for subwindows, only top-level windows
	public void ChangeNativeElementOpacity(XamlRoot owner, object content, double opacity)
	{
		// if (IsNativeElementAttached(owner, content) && content is X11Window x11Window)
		// {
		// 	// The spec requires a value between 0 and max int, not 0 and 1
		// 	var actualOpacity = (IntPtr)(opacity * uint.MaxValue);
		//
		// 	// if (opacity == 1)
		// 	// {
		// 	// 	XLib.XDeleteProperty(
		// 	// 		x11Window.Display,
		// 	// 		x11Window.Window,
		// 	// 		X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_WINDOW_OPACITY));
		// 	// }
		// 	// else
		// 	{
		// 		var tmp = new IntPtr[]
		// 		{
		// 			actualOpacity
		// 		};
		// 		XLib.XChangeProperty(
		// 			x11Window.Display,
		// 			x11Window.Window,
		// 			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_WINDOW_OPACITY),
		// 			X11Helper.GetAtom(x11Window.Display, X11Helper.XA_CARDINAL),
		// 			32,
		// 			PropertyMode.Replace,
		// 			actualOpacity,
		// 			1);
		// 	}
		// }
	}

	private static bool Exists(string fileName)
	{
		if (File.Exists(fileName))
		{
			return true;
		}

		var values = Environment.GetEnvironmentVariable("PATH");
		if (values is null)
		{
			return false;
		}

		return values
			.Split(Path.PathSeparator)
			.Select(path => Path.Combine(path, fileName))
			.Any(File.Exists);
	}

	private unsafe static IntPtr FindWindowByTitle(IntPtr display, IntPtr current, string title)
	{
		var _1 = X11Helper.XFetchName(display, current, out var name);
		if (name == title)
		{
			return current;
		}

		var _2 = XLib.XQueryTree(display,
			current,
			out _,
			out _,
			out IntPtr children,
			out int nChildren);

		var span = new Span<IntPtr>(children.ToPointer(), nChildren);

		for (var i =  0; i < nChildren; ++i)
		{
			IntPtr window = FindWindowByTitle(display, span[i], title);

			if (window != IntPtr.Zero)
			{
				return window;
			}
		}

		return IntPtr.Zero;
	}

	private unsafe static IntPtr FindWindowById(IntPtr display, IntPtr current, IntPtr id)
	{
		if (current == id)
		{
			return current;
		}

		var _2 = XLib.XQueryTree(display,
			current,
			out _,
			out _,
			out IntPtr children,
			out int nChildren);

		var span = new Span<IntPtr>(children.ToPointer(), nChildren);

		for (var i =  0; i < nChildren; ++i)
		{
			IntPtr window = FindWindowById(display, span[i], id);

			if (window != IntPtr.Zero)
			{
				return window;
			}
		}

		return IntPtr.Zero;
	}
}
