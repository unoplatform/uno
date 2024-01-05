#nullable enable
using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Core;

internal partial class ContentRoot
{
	private object? _host;

	internal void SetHost(object host)
	{
		// WARNING: The host might be set more than once (but only using the same host!)

		if (_host is null)
		{
			_host = host;
			InputManager.Initialize(host);
		}
		else
		{
			Debug.Assert(_host == host);
		}
	}

	internal CoreDispatcher Dispatcher => CoreDispatcher.Main; //TODO:MZ: set dispatcher per content root (from SetHost?)

	internal Microsoft.UI.Xaml.Window? GetOwnerXamlWindow()
	{
		return Type switch
		{
			ContentRootType.CoreWindow => Microsoft.UI.Xaml.Window.CurrentSafe,
			ContentRootType.XamlIsland when XamlIslandRoot is not null => XamlIslandRoot.OwnerWindow,
			_ => null
		};
	}
}
