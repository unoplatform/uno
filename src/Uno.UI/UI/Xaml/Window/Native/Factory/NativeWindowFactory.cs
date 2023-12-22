#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWindowFactory
{
	public static INativeWindowWrapper? CreateWindow(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot)
	{
		if (window is null)
		{
			throw new ArgumentNullException(nameof(window));
		}

		if (xamlRoot is null)
		{
			throw new ArgumentNullException(nameof(xamlRoot));
		}

		return CreateWindowPlatform(window, xamlRoot);
	}
}
