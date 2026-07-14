#nullable enable

using System.Collections.Generic;
using Uno.UI.Runtime.Skia;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Headless.UI;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Headless.UI;

internal sealed class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	private readonly HeadlessHostBuilder _builder;
	private readonly List<HeadlessWindowWrapper> _windows = new();
	private int _windowIndex;

	internal NativeWindowFactoryExtension(HeadlessHostBuilder builder)
	{
		_builder = builder;
	}

	public bool SupportsClosingCancellation => true;

	public bool SupportsMultipleWindows => true;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		var index = _windowIndex++;
		var options = _builder.ResolveWindowOptions(index, window);
		var wrapper = new HeadlessWindowWrapper(window, xamlRoot, options);
		_windows.Add(wrapper);
		return wrapper;
	}

	/// <summary>Disposes every window's renderer, joining their render threads. Called on host shutdown.</summary>
	internal void DisposeWindows()
	{
		foreach (var window in _windows)
		{
			window.DisposeRenderer();
		}
	}
}
