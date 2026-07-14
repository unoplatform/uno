#nullable enable

using System;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Headless.UI;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Headless.UI;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	private readonly IXamlRootHost _host;

	private Window? _initialWindow;

	internal NativeWindowFactoryExtension(IXamlRootHost host)
	{
		_host = host;
	}

	public bool SupportsClosingCancellation => false;

	public bool SupportsMultipleWindows => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		if (_initialWindow is not null && _initialWindow != window)
		{
			throw new InvalidOperationException("The headless host currently supports a single window only");
		}

		_initialWindow = window;
		HeadlessWindowWrapper.Instance.SetWindow(window, xamlRoot);
		XamlRootMap.Register(xamlRoot, _host);

		return HeadlessWindowWrapper.Instance;
	}
}
