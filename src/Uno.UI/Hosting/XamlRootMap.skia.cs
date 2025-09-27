#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

internal static class XamlRootMap
{
	private static readonly Dictionary<XamlRoot, IXamlRootHost> _map = new();
	private static readonly Dictionary<IXamlRootHost, XamlRoot> _reverseMap = new();

	internal static event EventHandler<XamlRoot>? Registered;

	internal static event EventHandler<XamlRoot>? Unregistered;

	internal static void Register(XamlRoot xamlRoot, IXamlRootHost host)
	{
		if (xamlRoot is null)
		{
			throw new ArgumentNullException(nameof(xamlRoot));
		}

		if (host is null)
		{
			throw new ArgumentNullException(nameof(host));
		}

		if (_map.ContainsKey(xamlRoot))
		{
			return;
		}

		_map[xamlRoot] = host;
		_reverseMap[host] = xamlRoot;
		xamlRoot.VisualTree.ContentRoot.SetHost(host); // Note: This might be a duplicated call but it's supported by ContentRoot and safe

		xamlRoot.RenderInvalidated += host.InvalidateRender;
		Registered?.Invoke(null, xamlRoot);
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
			xamlRoot.RenderInvalidated -= host.InvalidateRender;
			_map.Remove(xamlRoot);
			_reverseMap.Remove(host);
			Unregistered?.Invoke(null, xamlRoot);
		}
	}

	internal static IXamlRootHost? GetHostForRoot(XamlRoot xamlRoot) =>
		_map.TryGetValue(xamlRoot, out var host) ? host : default;

	internal static XamlRoot? GetRootForHost(IXamlRootHost host) =>
		_reverseMap.TryGetValue(host, out var xamlRoot) ? xamlRoot : default;
}
