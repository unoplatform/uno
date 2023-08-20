#nullable enable

using System;
using Uno.Foundation.Extensibility;

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

	private static INativeWindowWrapper? CreateWindowPlatform(Windows.UI.Xaml.Window window)
	{
		if (_nativeWindowFactory.Value is not { } factory)
		{
			return null;
		}

		return factory.CreateWindow(window);
	}
}
