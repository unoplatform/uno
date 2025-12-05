#nullable enable

using System;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia;

internal class WebAssemblyWindowFactoryExtension : INativeWindowFactoryExtension
{
	private readonly IXamlRootHost _host;

	private Window? _initialWindow;

	internal WebAssemblyWindowFactoryExtension(IXamlRootHost host)
	{
		_host = host;
	}

	public bool SupportsMultipleWindows => false;

	public bool SupportsClosingCancellation => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		if (
			_initialWindow is not null
			&& _initialWindow != window

			// When a second ALC is defined with a Window, we allow it
			// even on single-window platforms
			&& Window.ContentHostOverride == null
		)
		{
			throw new InvalidOperationException("Wasm Skia currently supports single window only");
		}

		_initialWindow = window;
		WebAssemblyWindowWrapper.Instance.SetWindow(window, xamlRoot);
		XamlRootMap.Register(xamlRoot, _host);

		return WebAssemblyWindowWrapper.Instance;
	}
}
