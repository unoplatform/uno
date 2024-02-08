#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;

using SkiaSharp;

using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

using Window = Microsoft.UI.Xaml.Window;

using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowWrapper : NativeWindowWrapperBase
{
	private readonly MacOSWindowNative _window;

	public MacOSWindowWrapper(MacOSWindowNative window)
	{
		_window = window;

		// FIXME: we hit this too late, we already have received the first resize :(
		window.Host.SizeChanged += OnHostSizeChanged;
	}

	public override object NativeWindow => _window;

	public MacOSWindowNative Native => _window;

	private void OnHostSizeChanged(object? sender, Windows.Foundation.Size size)
	{
		Bounds = new Rect(default, size);
		VisibleBounds = Bounds;
	}
}
