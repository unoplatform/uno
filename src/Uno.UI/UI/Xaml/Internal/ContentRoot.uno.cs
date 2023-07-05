using System;
using System.Diagnostics;
using System.Linq;

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
}
