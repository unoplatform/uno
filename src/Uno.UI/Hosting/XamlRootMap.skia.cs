#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Uno.UI.Hosting;

internal static class XamlRootMap<THost> where THost : IXamlRootHost
{
	private static readonly Dictionary<XamlRoot, THost> _map = new();

	internal static void Register(XamlRoot xamlRoot, THost host)
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
		xamlRoot.VisualTree.ContentRoot.SetHost(host); // Note: This might be a duplicated call but it's supported by ContentRoot and safe

		xamlRoot.InvalidateRender += host.InvalidateRender;
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
			xamlRoot.InvalidateRender -= host.InvalidateRender;
			_map.Remove(xamlRoot);
		}
	}

	internal static THost? GetHostForRoot(XamlRoot xamlRoot) =>
		_map.TryGetValue(xamlRoot, out var host) ? host : default;
}
