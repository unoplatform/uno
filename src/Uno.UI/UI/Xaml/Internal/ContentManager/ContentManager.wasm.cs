#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	static partial void AttachToWindowPlatform(UIElement rootElement, Microsoft.UI.Xaml.Window window)
	{
		WindowManagerInterop.SetRootElement(rootElement.HtmlId);
		UpdateRootAttributes(rootElement);
		if (rootElement.XamlRoot is { } xamlRoot)
		{
			// This will requrest the initial tick to get the root element to load
			xamlRoot.InvalidateMeasure();
			xamlRoot.InvalidateArrange();
		}
	}

	private static void UpdateRootAttributes(UIElement rootElement)
	{
		if (rootElement is null)
		{
			throw new InvalidOperationException("Private window root is not yet set.");
		}

		if (FeatureConfiguration.Cursors.UseHandForInteraction)
		{
			rootElement.SetAttribute("data-use-hand-cursor-interaction", "true");
		}
		else
		{
			rootElement.RemoveAttribute("data-use-hand-cursor-interaction");
		}
	}
}
