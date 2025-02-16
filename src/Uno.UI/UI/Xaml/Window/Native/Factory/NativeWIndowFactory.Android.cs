#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	public static bool SupportsClosingCancellation => false;

	public static bool SupportsMultipleWindows => false;

	private static INativeWindowWrapper? CreateWindowPlatform(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot) =>
		new NativeWindowWrapper(window, xamlRoot);
}
