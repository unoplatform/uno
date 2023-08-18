#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void CustomSetContent(UIElement content)
	{
		//if (_rootVisual == null)
		//{
		//	_rootBorder = new Border();
		//	_rootScrollViewer = new ScrollViewer()
		//	{
		//		VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
		//		HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
		//		VerticalScrollMode = ScrollMode.Disabled,
		//		HorizontalScrollMode = ScrollMode.Disabled,
		//		Content = _rootBorder
		//	};
		//	//TODO Uno: We can set and RootScrollViewer properly in case of WASM
		//	CoreServices.Instance.PutVisualRoot(_rootScrollViewer);
		//	_rootVisual = CoreServices.Instance.MainRootVisual;

		//	if (_rootVisual == null)
		//	{
		//		throw new InvalidOperationException("The root visual could not be created.");
		//	}

			// Load the root element in DOM

			if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
			{
				UIElement.LoadingRootElement(_rootVisual);
			}

			WindowManagerInterop.SetRootElement(_rootVisual.HtmlId);

			if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
			{
				UIElement.RootElementLoaded(_rootVisual);
			}
		}

		_rootBorder.Child = _content = content;
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
}
