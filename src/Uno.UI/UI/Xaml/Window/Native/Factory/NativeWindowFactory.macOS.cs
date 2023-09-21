#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	//TODO:MZ: Allow multi-window support for macOS
	private static INativeWindowWrapper? CreateWindowPlatform(Windows.UI.Xaml.Window window, XamlRoot xamlRoot) =>
		NativeWindowWrapper.Instance;
}
