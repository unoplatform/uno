using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia;
namespace Uno.WinUI.Runtime.Skia.X11;

// https://www.x.org/releases/X11R7.6/doc/xextproto/shape.html
// Thanks to Jörg Seebohn for providing an example on how to use X SHAPE
// https://gist.github.com/je-so/903479/834dfd78705b16ec5f7bbd10925980ace4049e17
internal partial class X11NativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private const string SampleVideoLink = "https://uno-assets.platform.uno/tests/uno/big_buck_bunny_720p_5mb.mp4";

	/// replace the executable and the args with whatever you have locally. This is only used
	/// for internal debugging. However, make sure that you can set a unique title to the window,
	/// so that you can then look it up.
	public object? CreateSampleComponent(string text)
	{
		if (XamlRoot is { } xamlRoot && X11Manager.XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			if (!Exists("mpv") && !Exists("vlc") && !Exists("xterm"))
			{
				return null;
			}

			// xterm is by far the most reliable since it opens up quickly. X11 doesn't have a nice way of
			// waiting for a specific window to be present, so we make do with a Thread.Sleep
			var filename = Exists("xterm") ? "xterm" : (Exists("vlc") ? "vlc" : "mpv");

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Using {filename} as the X11 native application for {nameof(CreateSampleComponent)}.");
			}

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = filename,
					UseShellExecute = false
				}
			};


			string title;
			if (filename == "vlc")
			{
				title = $"Sample Video {Random.Shared.Next()} {text}"; // used to maintain unique titles
				process.StartInfo.ArgumentList.Add(SampleVideoLink);
				process.StartInfo.ArgumentList.Add("--meta-title");
				process.StartInfo.ArgumentList.Add(title);
				title += " - VLC media player";
			}
			else if (filename == "mpv")
			{
				title = $"Sample Video {Random.Shared.Next()} {text}"; // used to maintain unique titles
				process.StartInfo.ArgumentList.Add("--keep-open=always");
				process.StartInfo.ArgumentList.Add($"--title={title}");
				process.StartInfo.ArgumentList.Add(SampleVideoLink);
			}
			else if (filename == "alacritty")
			{
				title = $"Sample terminal {Random.Shared.Next()} {text}"; // used to maintain unique titles
				process.StartInfo.ArgumentList.Add("--title");
				process.StartInfo.ArgumentList.Add(title);
			}
			else if (filename == "xterm")
			{
				title = $"Sample terminal {Random.Shared.Next()} {text}"; // used to maintain unique titles
				process.StartInfo.ArgumentList.Add("-iconic");
				process.StartInfo.ArgumentList.Add("-xrm");
				process.StartInfo.ArgumentList.Add("XTerm.vt100.allowTitleOps: false");
				process.StartInfo.ArgumentList.Add("-T");
				process.StartInfo.ArgumentList.Add(title);
				process.StartInfo.ArgumentList.Add("-e");
				process.StartInfo.ArgumentList.Add("top");
			}
			else
			{
				return null;
			}

			process.Start();

			using var lockDiposable = X11Helper.XLock(_display);

			_ = XLib.XQueryTree(_display, host.RootX11Window.Window, out IntPtr root, out _, out var children, out _);
			_ = XLib.XFree(children);

			// Wait for the window to open.
			Thread.Sleep(500);
			IntPtr window = FindWindowByTitle(_display, root, title);

			if (window == IntPtr.Zero)
			{
				process.Kill();
				return null;
			}

			return new X11NativeWindow(window);
		}

		// For debugging: replace the above with a hardcoded window id, obtainable using e.g. wmctrl
		// if (owner is XamlRoot xamlRoot
		// 	&& X11Manager.XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		// {
		// 	return new X11Window(host.X11Window.Display, 0x04a00002);
		// }

		return null;
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
		_ = X11Helper.XFetchName(display, current, out var name);
		if (name == title)
		{
			return current;
		}

		_ = XLib.XQueryTree(display,
			current,
			out _,
			out _,
			out IntPtr children,
			out int nChildren);

		var span = new Span<IntPtr>(children.ToPointer(), nChildren);

		for (var i = 0; i < nChildren; ++i)
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

		_ = XLib.XQueryTree(display,
			current,
			out _,
			out _,
			out IntPtr children,
			out int nChildren);

		var span = new Span<IntPtr>(children.ToPointer(), nChildren);

		for (var i = 0; i < nChildren; ++i)
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
