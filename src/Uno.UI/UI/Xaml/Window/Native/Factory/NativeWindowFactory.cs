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

		var windowWrapper = CreateWindowPlatform(window, xamlRoot);

		if (xamlRoot.VisualTree.ContentRoot.CompositionContent is null)
		{
			throw new InvalidOperationException(
				"ContentRoot.CompositionContent must be initialized along with the native window!" +
				"Use either the base ctor which takes XamlRoot or set ContentIsland within your factory manually.");
		}

		return windowWrapper;
	}
}
