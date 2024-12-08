#nullable enable
#if !__SKIA__

using System;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	public static bool SupportsClosingCancellation => false;

	public static bool SupportsMultipleWindows => false;

	private static INativeWindowWrapper? CreateWindowPlatform(Windows.UI.Xaml.Window window, XamlRoot xamlRoot)
	{
		NativeWindowWrapper.Instance.SetWindow(window, xamlRoot);
		return NativeWindowWrapper.Instance;
	}
}
#endif
