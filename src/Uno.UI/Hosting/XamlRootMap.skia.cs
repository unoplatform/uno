#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Uno.UI.Hosting;

internal class XamlRootMap<THost> where THost : IXamlRootHost
{
	private readonly Dictionary<XamlRoot, THost> _map = new();
	private readonly Dictionary<THost, XamlRoot> _reverseMap = new();

	internal event EventHandler<XamlRoot>? Registered;

	internal event EventHandler<XamlRoot>? Unregistered;

	internal void Register(XamlRoot xamlRoot, THost host)
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

		xamlRoot.InvalidateRender += host.InvalidateRender;
		Registered?.Invoke(this, xamlRoot);
	}

	internal void Unregister(XamlRoot xamlRoot)
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
			_reverseMap.Remove(host);
			Unregistered?.Invoke(this, xamlRoot);
		}
	}

	internal THost? GetHostForRoot(XamlRoot xamlRoot) =>
		_map.TryGetValue(xamlRoot, out var host) ? host : default;

	internal XamlRoot? GetRootForHost(THost host) =>
		_reverseMap.TryGetValue(host, out var xamlRoot) ? xamlRoot : default;
}
