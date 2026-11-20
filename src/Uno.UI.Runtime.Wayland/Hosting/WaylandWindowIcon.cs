#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Wayland.Protocols;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland;

// Window icons through xdg-toplevel-icon-v1, from the package logo like the X11 host's _NET_WM_ICON.
internal static class WaylandWindowIcon
{
	private const int MaximumSize = 256;

	// UI thread: BitmapImage.GetScaledPath reads DisplayInformation.
	internal static string? ResolvePackageLogo()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is not { } uri)
		{
			return null;
		}
		var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
		var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);
		if (File.Exists(iconPath))
		{
			return iconPath;
		}
		if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
		{
			return scaledPath;
		}
		if (typeof(WaylandWindowIcon).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(WaylandWindowIcon).Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
		}
		return null;
	}

	// Any thread. Decodes through the neutral image codec into premultiplied BGRA; icon buffers must be square,
	// so the image is centered and scaled down to fit.
	internal static Icon? Decode(string path)
	{
		using var stream = File.OpenRead(path);
		if (!ImageEncoderDecoder.Current.TryDecode(stream, null, null, out var frames))
		{
			if (typeof(WaylandWindowIcon).Log().IsEnabled(LogLevel.Error))
			{
				typeof(WaylandWindowIcon).Log().Error($"Unable to decode icon file [{path}].");
			}
			return null;
		}
		using (frames)
		{
			var image = frames.Frames[0];
			var source = new byte[checked(image.PixelWidth * image.PixelHeight * 4)];
			image.CopyPixels(source);
			return Fit(source, image.PixelWidth, image.PixelHeight);
		}
	}

	internal static Icon Fit(ReadOnlySpan<byte> source, int width, int height)
	{
		var longest = Math.Max(width, height);
		var size = Math.Min(longest, MaximumSize);
		var scaledWidth = Math.Max(1, (int)Math.Round((double)width * size / longest));
		var scaledHeight = Math.Max(1, (int)Math.Round((double)height * size / longest));
		var offsetX = (size - scaledWidth) / 2;
		var offsetY = (size - scaledHeight) / 2;
		var pixels = new byte[size * size * 4];
		for (var y = 0; y < scaledHeight; y++)
		{
			// Area average over the source pixels each target pixel covers.
			var sourceY0 = y * height / scaledHeight;
			var sourceY1 = Math.Max(sourceY0 + 1, (y + 1) * height / scaledHeight);
			for (var x = 0; x < scaledWidth; x++)
			{
				var sourceX0 = x * width / scaledWidth;
				var sourceX1 = Math.Max(sourceX0 + 1, (x + 1) * width / scaledWidth);
				int b = 0, g = 0, r = 0, a = 0;
				for (var sy = sourceY0; sy < sourceY1; sy++)
				{
					for (var sx = sourceX0; sx < sourceX1; sx++)
					{
						var index = (sy * width + sx) * 4;
						b += source[index];
						g += source[index + 1];
						r += source[index + 2];
						a += source[index + 3];
					}
				}
				var count = (sourceY1 - sourceY0) * (sourceX1 - sourceX0);
				var target = ((y + offsetY) * size + x + offsetX) * 4;
				pixels[target] = (byte)(b / count);
				pixels[target + 1] = (byte)(g / count);
				pixels[target + 2] = (byte)(r / count);
				pixels[target + 3] = (byte)(a / count);
			}
		}
		return new Icon(size, pixels);
	}

	// Dispatch thread. The compositor copies the pixels on set_icon; the buffer may go once the icon is destroyed.
	internal static void Apply(WaylandConnection connection, XdgToplevel toplevel, Icon icon, int windowId)
	{
		if (connection.IconManager is not { } manager)
		{
			return;
		}
		using var buffer = new WaylandShmBuffer(connection.Shm, icon.Size, icon.Size, released: null);
		Marshal.Copy(icon.Pixels, 0, buffer.Memory, icon.Pixels.Length);
		var proxy = manager.CreateIcon();
		try
		{
			proxy.AddBuffer(buffer.Proxy, 1);
			manager.SetIcon(toplevel, proxy);
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace(FormattableString.Invariant($"window{windowId}: icon {icon.Size}x{icon.Size}"));
			}
		}
		finally
		{
			proxy.Dispose();
		}
	}

	// Square premultiplied BGRA pixels, the wl_shm ARGB8888 layout.
	internal sealed record Icon(int Size, byte[] Pixels);
}
