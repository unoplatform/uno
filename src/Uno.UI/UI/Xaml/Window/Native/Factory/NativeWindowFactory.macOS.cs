#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	private static INativeWindowWrapper? CreateWindowPlatform(Windows.UI.Xaml.Window window, XamlRoot xamlRoot) => null;
}
