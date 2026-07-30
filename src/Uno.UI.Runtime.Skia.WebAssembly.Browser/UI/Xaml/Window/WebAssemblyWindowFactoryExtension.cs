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

		// Only register the primary window. When ContentHostOverride is set, we're in ALC hosting mode
		// and the secondary ALC's window must not replace the primary app's InputManager/XamlRoot host.
		// We can't use window.IsAlcWindow here because the Window may be created through shared
		// Uno.Extensions assemblies (e.g. ApplicationBuilder), which are loaded in the default ALC
		// regardless of whether the calling app is a secondary ALC — making IsAlcWindow unreliable.
		// Additionally, as there can only be one window, we can simply check if the ContentHostOverride
		// is set, which is only set when secondary ALCs are used.
		if (Window.ContentHostOverride == null)
		{
			WebAssemblyWindowWrapper.Instance.SetWindow(window, xamlRoot);
			XamlRootMap.Register(xamlRoot, _host);
		}

		return WebAssemblyWindowWrapper.Instance;
	}
}
