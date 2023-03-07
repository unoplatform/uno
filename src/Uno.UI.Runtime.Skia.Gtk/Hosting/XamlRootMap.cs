#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Runtime.Skia;
using Windows.UI.Xaml;

namespace Uno.UI.XamlHost.Skia.Gtk.Hosting;

internal static class XamlRootMap
{
	private static readonly Dictionary<XamlRoot, GtkHost> _map = new();

	internal static void Register(XamlRoot xamlRoot, GtkHost host)
	{
		if (xamlRoot is null)
		{
			throw new ArgumentNullException(nameof(xamlRoot));
		}

		if (host is null)
		{
			throw new ArgumentNullException(nameof(host));
		}

		_map[xamlRoot] = host;

		xamlRoot.InvalidateRender += host.RenderSurface.InvalidateRender;
	}

	internal static void Unregister(XamlRoot xamlRoot)
	{
		if (xamlRoot is null)
		{
			throw new ArgumentNullException(nameof(xamlRoot));
		}

		var host = GetHostForRoot(xamlRoot);
		if (host is not null)
		{
			xamlRoot.InvalidateRender -= host.RenderSurface.InvalidateRender;
			_map.Remove(xamlRoot);
		}
	}

	internal static GtkHost? GetHostForRoot(XamlRoot xamlRoot) =>
		_map.TryGetValue(xamlRoot, out var host) ? host : null;
}
