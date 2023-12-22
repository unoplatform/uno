#nullable enable
#if !__SKIA__

using System;
using Uno.Foundation.Extensibility;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	public static bool SupportsMultipleWindows => false;

	private static INativeWindowWrapper? CreateWindowPlatform(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot) => NativeWindowWrapper.Instance;
}
#endif
