﻿#nullable enable

using System;
using Microsoft.UI.Content;
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

		if (windowWrapper is null)
		{
			// It was not possible to create a new window, just return.
			return null;
		}

		if (windowWrapper is NativeWindowWrapperBase wrapperBase && !wrapperBase.AssociatedWithManagedWindow)
		{
			throw new InvalidOperationException("Window wrapper must be associated with a managed window and its XamlRoot.");
		}

		if (windowWrapper.ContentSiteView is null)
		{
			throw new InvalidOperationException(
				"ContentSiteView must be initialized along with the native window!" +
				"Use either the base ctor which takes XamlRoot or set ContentIsland within your factory manually.");
		}

		var contentIsland = new ContentIsland(windowWrapper.ContentSiteView);
		xamlRoot.VisualTree.ContentRoot.SetContentIsland(contentIsland);

		return windowWrapper;
	}
}
