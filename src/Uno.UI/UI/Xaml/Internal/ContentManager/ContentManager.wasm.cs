#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	partial void SetupCoreWindowRootVisualPlatform(RootVisual rootVisual)
	{
		WindowManagerInterop.SetRootElement(_rootVisual.HtmlId);
	}

	private void UpdateRootAttributes()
	{
		if (_privateRootElement is null)
		{
			throw new InvalidOperationException("Private window root is not yet set.");
		}

		if (FeatureConfiguration.Cursors.UseHandForInteraction)
		{
			_privateRootElement.SetAttribute("data-use-hand-cursor-interaction", "true");
		}
		else
		{
			_privateRootElement.RemoveAttribute("data-use-hand-cursor-interaction");
		}
	}

	private void LoadRoot()
	{
		if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
		{
			UIElement.LoadingRootElement(_rootVisual);
		}


		if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
		{
			UIElement.RootElementLoaded(_rootVisual);
		}
	}
}
