#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Microsoft.UI.Xaml;

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

	public static bool SupportsClosingCancellation => _nativeWindowFactory.Value?.SupportsClosingCancellation ?? false;

	public static bool SupportsMultipleWindows => _nativeWindowFactory.Value?.SupportsMultipleWindows ?? false;

	private static INativeWindowWrapper? CreateWindowPlatform(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot)
	{
		if (_nativeWindowFactory.Value is not { } windowFactory)
		{
			throw new InvalidOperationException(
				"Window factory was not registered. Please ensure that you set up the application initialization " +
				"properly for this Skia target by following the migration docs.");
		}
		return windowFactory.CreateWindow(window, xamlRoot);
	}
}
