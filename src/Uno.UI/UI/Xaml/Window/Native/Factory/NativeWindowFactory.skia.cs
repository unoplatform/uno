#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	private static Lazy<INativeWindowFactoryExtension?> _nativeWindowFactory = new(() =>
	{
		if (!ApiExtensibility.CreateInstance<INativeWindowFactoryExtension>(typeof(DesktopWindow), out var factory))
		{
			return null;
		}

		return factory;
	});

	public static bool SupportsMultipleWindows => _nativeWindowFactory.Value?.SupportsMultipleWindows ?? false;

	private static INativeWindowWrapper? CreateWindowPlatform(Windows.UI.Xaml.Window window, XamlRoot xamlRoot) =>
		_nativeWindowFactory.Value?.CreateWindow(window, xamlRoot) ?? null;
}
