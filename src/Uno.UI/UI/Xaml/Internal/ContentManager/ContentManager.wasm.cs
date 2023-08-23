#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	partial void SetupCoreWindowRootVisualPlatform(RootVisual rootVisual)
	{
		WindowManagerInterop.SetRootElement(rootVisual.HtmlId);
	}

	private void UpdateRootAttributes(UIElement rootElement)
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

	static partial void LoadRootElementPlatform(XamlRoot xamlRoot, UIElement rootElement)
	{
		if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
		{
			UIElement.LoadingRootElement(rootElement);
		}

		xamlRoot.InvalidateMeasure();
		xamlRoot.InvalidateArrange();

		if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
		{
			UIElement.RootElementLoaded(rootElement);
		}
	}
}
