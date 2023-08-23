#nullable enable

using System;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWindowFactory
{
	public static INativeWindowWrapper? CreateWindow(Windows.UI.Xaml.Window window)
	{
		if (window is null)
		{
			throw new ArgumentNullException(nameof(window));
		}

		return CreateWindowPlatform(window);
	}

#if !__SKIA__
	private static INativeWindowWrapper? CreateWindowPlatform(Windows.UI.Xaml.Window window) => null;
#endif
}
