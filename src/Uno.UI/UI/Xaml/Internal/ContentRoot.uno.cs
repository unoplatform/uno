#nullable enable
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.UI.Xaml;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Core;

partial class ContentRoot
{
	private object? _host;

	internal void SetHost(object host)
	{
		// WARNING: The host might be set more than once (but only using the same host!)

		if (_host is null)
		{
			_host = host;

			// Skip InputManager initialization for secondary ALC hosts.
			// Secondary ALC content is rendered inside the main app's ContentHostOverride,
			// so it uses the main app's input handling. Creating new input sources would
			// override the TypeScript keyboard handler and break input for the entire app.
			if (!IsHostFromSecondaryAlc(host))
			{
				InputManager.Initialize(host);
			}
		}
		else
		{
			Debug.Assert(_host == host);
		}
	}

	private static bool IsHostFromSecondaryAlc(object host)
	{
		// Check if the host is a Window from a secondary ALC
		if (host is Window window)
		{
			return window.IsAlcWindow;
		}

		return false;
	}

	internal CoreDispatcher Dispatcher => CoreDispatcher.Main; //TODO:MZ: set dispatcher per content root (from SetHost?)
}
